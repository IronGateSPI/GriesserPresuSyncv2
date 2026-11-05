# Despliegue como Servicio de Windows

GriesserPresuSync está pensado para ejecutarse en segundo plano como un
Servicio de Windows. Este documento describe los pasos exactos para
publicarlo y registrarlo en un servidor.

## 1. Requisitos previos

| Máquina        | Requisito                                                        |
| -------------- | ---------------------------------------------------------------- |
| Build (PC dev) | .NET Core 3.1 SDK (o superior) y PowerShell                      |
| Servidor       | Windows Server 2016+ / Windows 10+. NO requiere .NET Core 3.1    |
|                | porque publicamos *self-contained* (el runtime se empaqueta).    |

## 2. Publicación (en la máquina de desarrollo)

```powershell
cd <ruta-al-repo>
.\scripts\publish.ps1
```

El script genera la carpeta `./publish/` con `GriesserPresuSync.exe`,
todos los `.dll` necesarios y el runtime de .NET Core 3.1 incluido.

Opcionalmente, si quieres un único `.exe`:

```powershell
.\scripts\publish.ps1 -SingleFile $true
```

## 3. Copiado al servidor

Copia el contenido de `./publish/` al servidor. Recomendado:

```
C:\Servicios\GriesserPresuSync\
```

(Si quieres otra ruta, recuerda pasarla con `-BinPath` al script de
instalación.)

## 4. Configuración

En el servidor, edita `C:\Servicios\GriesserPresuSync\appsettings.json`
para ajustar la cadena de conexión a SQL Server y los endpoints de la API:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SRVSQL19;Initial Catalog=BDGRIESSER;User=...;Password=..."
  },
  "GriesserSyncSettings": {
    "ApiUrl": "https://www.migriesser.com/es/budgets/api/",
    "SyncIntervalSeconds": 10
  },
  "MallorquinasSyncSettings": {
    "ApiUrl": "https://www.migriesser.com/es/mallorquinas_budgets/api",
    "SyncIntervalSeconds": 60
  },
  "ClientesCrmSyncSettings": {
    "ApiUrl": "https://www.crmgriesser.es/es",
    "EnableExport": true,
    "SyncIntervalSeconds": 30,
    "MaxIntentos": 5,
    "CodigoEmpresa": 1,
    "HttpTimeoutSeconds": 30,
    "EnableImport": true,
    "ImportHour": 3
  }
}
```

`SyncIntervalSeconds` es opcional. Si no se especifica, se usa 10s.

### 4.1 Módulo Clientes ↔ CRM Griesser

A partir de la versión 2026-05-22 el servicio incorpora dos workers
adicionales que mantienen sincronizados los partners de Sage con el
CRM `https://www.crmgriesser.es/`:

- **`WorkerClientesCrm`** — Sage → CRM, en tiempo cuasi real.
  Drena la tabla `dbo.IG_CRM_ClientesPendientes` (alimentada por
  triggers en `dbo.Clientes` y `dbo.ClientesImportesRiesgo`) y por
  cada cliente lanza un `PUT {ApiUrl}/{codigoCliente}/sync_sage_distributor`
  con los datos consolidados (nombre, NIF, dirección, facturación
  anual, descuento, CYC, CYC descubierto).
- **`WorkerClientesCrmImport`** — CRM → Sage, una vez al día.
  A la hora indicada en `ImportHour` (defecto 03:00) llama a
  `GET {ApiUrl}/all/sync_sage_distributor` y actualiza en
  `dbo.Clientes` los campos `zzTarget` (target de ventas) y
  `zzTipoCliente` (Premium / A / B / C / D).

Parámetros de la sección `ClientesCrmSyncSettings`:

| Parámetro            | Default                       | Descripción                                                                 |
| -------------------- | ----------------------------- | --------------------------------------------------------------------------- |
| `ApiUrl`             | `https://www.crmgriesser.es/es` | Base del CRM. No incluir el `/` final.                                    |
| `EnableExport`       | `true`                        | Activa/desactiva el worker Sage→CRM sin recompilar.                         |
| `SyncIntervalSeconds`| `30`                          | Intervalo del barrido de la cola.                                           |
| `MaxIntentos`        | `5`                           | Reintentos por fila antes de pasar a `Error`.                               |
| `CodigoEmpresa`      | `1`                           | Empresa de Sage a sincronizar.                                              |
| `HttpTimeoutSeconds` | `30`                          | Timeout HTTP por request al CRM.                                            |
| `FacturasHistoricoMeses` | `24`                      | Ventana del campo `facturas` del PUT. Ver nota de tamaño más abajo.          |
| `GuardarPayloadCompleto` | `false`                   | Guarda el JSON íntegro en `PayloadEnviado` también en los envíos correctos. Solo para depurar. |
| `EnableImport`       | `true`                        | Activa/desactiva el job nocturno.                                           |
| `ImportHour`         | `3`                           | Hora del día (0–23) en la que se ejecuta el job nocturno.                   |

### 4.1.1 Campos `facturas` y `pedidos` (2026-09)

El PUT incorpora dos campos nuevos junto al ya existente `facturacion_desglosada`:

- **`facturas`** — facturas individuales con su detalle de producto, en estructura
  anidada (una factura agrupa N productos, porque en Sage consolida líneas de
  varios albaranes). Origen: `ResumenCliente` + `LineasAlbaranCliente` +
  `CabeceraAlbaranCliente.zColor`.
- **`pedidos`** — pedidos de venta pendientes de servir, con sus líneas.
  Origen: `CabeceraPedidoCliente` + `LineasPedidoCliente`, filtrando
  `Estado = 0` (pendiente; `Estado = 2` es servido).

Ambos aplican el mismo filtro de familias que el desglose: `1Ma, 1P, 1PE, 1PG,
1T, 1To, 1We, AU, S`.

**Sobre el tamaño del payload.** `facturas` está acotado a 24 meses por defecto
(`FacturasHistoricoMeses`) de forma deliberada. Medido sobre BDGRIESSER, el
cliente con más volumen acumula 258 facturas y 1.912 líneas factura-artículo en
5 años, lo que generaría unos 215 KB url-encoded solo en ese campo. Con 24 meses
baja a un entorno cómodo. El histórico largo agregado sigue viajando en
`facturacion_desglosada`, que se mantiene hasta que el CRM migre su programación.

Subir este valor a 60 es posible sin recompilar, pero conviene confirmar antes
con Griesser el límite de tamaño de body que acepta su servidor.

### 4.2 Despliegue de la base de datos (módulo Clientes ↔ CRM)

Antes de arrancar el servicio por primera vez con este módulo, ejecuta
los scripts SQL en este orden, sobre la base `BDGRIESSER`:

```text
scripts\sql\2026_05_22_create_IG_CRM_ClientesPendientes.sql
scripts\sql\2026_05_22_create_triggers_clientes_crm.sql
```

Ambos son **idempotentes y transaccionales**: se pueden re-ejecutar las
veces que haga falta. El primero crea la tabla cola y sus índices; el
segundo crea (o re-crea) los triggers `tr_Clientes_CRM_AfterIUD` y
`tr_ClientesImportesRiesgo_CRM_AfterIUD`.

> No se modifica la tabla `dbo.Clientes`. Los campos `zzTarget` y
> `zzTipoCliente` ya existen.

## 5. Instalación del servicio (en el servidor, como Administrador)

```powershell
cd C:\Servicios\GriesserPresuSync
.\scripts\install-service.ps1
```

El script crea el servicio con:

- Nombre interno: `GriesserPresuSync`
- Cuenta: `LocalSystem`
- Tipo de inicio: **Automático**
- Acciones de fallo: **reinicio automático** tras 1 min, hasta 3 veces;
  el contador se resetea cada 24h.
- Descripción visible en `services.msc`.

Después arranca el servicio y comprueba el estado.

## 6. Verificación

1. `services.msc` → buscar **Griesser Presu Sync** → estado *Running*.
2. **Visor de eventos** → *Registros de Windows* → *Application* →
   filtrar por origen `GriesserPresuSync` para ver los logs del worker.
3. SQL: tras unos segundos, las tablas
   `IG_GriesserSyncPresupuestos` y `IG_GriesserSyncMallorquinas`
   deberían empezar a recibir filas.
4. SQL (módulo CRM): modifica un cliente partner en `dbo.Clientes`;
   en pocos segundos debe aparecer una fila en
   `dbo.IG_CRM_ClientesPendientes` y luego pasar a `EstadoEnvio = 'Enviado'`.
   En el Visor de Eventos verás líneas tipo
   `Cliente 000746 enviado al CRM (filas: 1)`.

## 7. Operación habitual

| Acción              | Comando (como Admin)                                                |
| ------------------- | ------------------------------------------------------------------- |
| Parar servicio      | `Stop-Service GriesserPresuSync`                                    |
| Arrancar servicio   | `Start-Service GriesserPresuSync`                                   |
| Reiniciar           | `Restart-Service GriesserPresuSync`                                 |
| Desinstalar         | `.\scripts\uninstall-service.ps1`                                   |
| Actualizar binarios | Stop-Service → reemplazar archivos en `BinPath` → Start-Service     |

## 8. Resolución de problemas

- **El servicio arranca y se para inmediatamente**: revisa el Visor de
  Eventos. Es típico: cadena de conexión mal formada, SQL inaccesible,
  endpoint de API caído, o `appsettings.json` no copiado.
- **Permisos**: si SQL Server requiere autenticación integrada, el
  servicio debe correr bajo una cuenta de dominio con acceso a la base.
  En ese caso, en `services.msc` → propiedades del servicio → pestaña
  *Iniciar sesión* → cuenta de dominio.
- **No veo logs en Event Viewer**: la primera vez Windows puede tardar
  un par de segundos en crear el *source*. Si sigue sin aparecer, el
  servicio puede no estar arrancando — mira *System* en Event Viewer
  para errores del SCM.
