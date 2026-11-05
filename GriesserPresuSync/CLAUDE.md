# GriesserPresuSync — contexto del proyecto

Servicio de Windows (.NET Core 3.1, self-contained) que sincroniza Sage 200 con
dos sistemas externos de Griesser. Interlocutor: Albert, perfil de gestión de
proyecto con conocimiento técnico. Explica el diseño antes que el código y
prioriza cero sorpresas en producción.

## Workers

| Worker | Dirección | Cadencia |
| --- | --- | --- |
| `Worker` | MiGriesser → Sage (presupuestos) | configurable |
| `WorkerMallorquinas` | MiGriesser → Sage (mallorquinas) | configurable |
| `WorkerClientesCrm` | Sage → CRM Griesser (partners) | 30 s |
| `WorkerClientesCrmImport` | CRM → Sage (target, tipo cliente) | diaria, 03:00 |

El grueso del trabajo reciente está en `WorkerClientesCrm`.

## Entornos

Hay tres ubicaciones distintas y conviene no confundirlas:

| Dónde | Qué es |
| --- | --- |
| Carpeta local de trabajo | Copia donde se edita el código. Es la fuente de verdad del desarrollo. |
| `I:\VisualStudio\GriesserPresuSyncV2\` | **Servidor del cliente**, por RDP. Donde se compila con Visual Studio. |
| `C:\Servicios\GriesserPresuSync\` | Servidor del cliente. Donde corre el servicio en producción. |

El flujo es: editar en local → copiar al `I:` del servidor → compilar y publicar
allí → copiar el `publish` a `C:\Servicios`.

Cuidado al sincronizar hacia el `I:`: ya ha pasado que llegasen unos ficheros y
otros no, dando errores de compilación engañosos (`CS1061` por una propiedad que
sí existía en local). Los ficheros nuevos son los que más se escapan, porque no
aparecen marcados como modificados.

## Base de datos

`BDGRIESSER` en SQL Server, instancia `Igserver1\SAGE`. La cadena de conexión de
producción está en `appsettings.json` — no se duplica aquí a propósito, para no
repartir la credencial por más sitios de los necesarios.

**Vale mucho la pena tener un MCP de SQL configurado.** Durante el desarrollo se
usó uno contra una copia local restaurada del cliente, y validar cada query
contra datos reales antes de escribir el código detectó cosas que de otro modo
habrían llegado a producción:

- `num_factura` colisionaba entre ejercicios porque Sage reinicia la numeración.
- La cola acumulaba 122 MB de payloads duplicados.
- Los tipos de `IG_PedidoOnline` no eran uniformes entre tablas.
- `referencia_cliente` sale vacío en el 100% de las filas.

Si se configura uno nuevo, que sea con un login **de solo lectura**
(`db_datareader` sobre `BDGRIESSER`, nada más). Un detalle que despista: si las
consultas devuelven vacío en lugar de dar error de permisos, es que al login le
falta el rol — las vistas de catálogo filtran silenciosamente por permisos.

Las queries del worker de clientes NO usan EF: van por ADO.NET directo contra la
conexión que da EF, porque las tablas de Sage no están mapeadas y mapearlas sería
pesado y frágil.

## Decisiones no evidentes en el código

**No usar `FormUrlEncodedContent`.** Internamente llama a `Uri.EscapeDataString`,
que tiene un tope duro de 65.520 caracteres por valor y lanza
`Invalid URI: The Uri string is too long`. El payload de un partner grande ronda
los 300 KB, así que reventaba siempre. El body se construye a mano con
`WebUtility.UrlEncode` sobre un `StringContent`. No revertir esto.

**Decimales compactos.** Las columnas de Sage son `DECIMAL(28,10)`, así que un
valor de 6 unidades llegaba al JSON como `6.0000000000`. `DecimalCompactoConverter`
los redondea a 2 y quita ceros. Recorta en torno a un 30% del payload.

**`num_factura` lleva el ejercicio.** Formato `2026/SV/12345` o `2026/12345`.
Sage reinicia la numeración cada año (verificado: todas las series arrancan en 1
en cada ejercicio), así que serie+número solos colisionan dentro de la ventana
de 24 meses.

**Reclamo de filas con estado `Procesando`.** El worker marca las filas de
`IG_CRM_ClientesPendientes` antes de consultar Sage. Esto permite que el trigger
deduplique sin ventana temporal y sin perder cambios: mientras se procesa, la cola
no tiene pendientes, así que un cambio que llegue encola uno nuevo. Al empezar
cada ciclo, las filas que sigan en `Procesando` son huérfanas de una ejecución
cortada y vuelven a `Pendiente`.

**ORDEN DE DESPLIEGUE: primero el servicio, después el trigger.** El trigger
deduplicado (`2026_09_28_fix_trigger_riesgo_dedupe.sql`) depende del reclamo de
filas. Con el binario antiguo se perderían cambios.

**El payload no se guarda entero en cada fila.** El trigger encolaba hasta 26
filas por un mismo recálculo de riesgo y se escribía el JSON completo en todas:
122 MB acumulados solo con el payload antiguo, que era 15 veces menor. Ahora en
los envíos correctos se guarda un resumen con los bloques grandes sustituidos por
su tamaño; el completo solo cuando falla. Para depurar está el flag
`GuardarPayloadCompleto` en `appsettings.json`.

**El PUT envía siempre el estado completo, no incrementos.** De ahí que la
deduplicación sea segura: un encolado omitido solo retrasa, nunca pierde.

**`pedido_online`.** Sage usa `-1` = sí / `0` = no; el CRM espera `1` / `0`.
Los tipos NO son uniformes y es a propósito: las tablas de Sage
(`ResumenCliente`, `CabeceraPedidoCliente`, `CabeceraOfertaCliente`,
`CabeceraAlbaranCliente`) lo tienen como `SMALLINT NOT NULL`, y las de staging
(`IG_GriesserSyncPresupuestos`, `IG_GriesserSyncMallor`) como `INT NULL`.
En `facturacion_desglosada` el campo se resuelve con `MIN()` en SQL — sobre la
codificación -1/0 equivale a un OR — para no meterlo en el `GROUP BY` y no partir
agrupaciones.

**Sin filtros de familia.** Se quitaron de las tres queries por decisión del
cliente: con el filtro faltaban 82 de 340 facturas del partner más grande, que
no llegaban al CRM en absoluto.

## Campos del PUT al CRM

Orden exacto esperado por el CRM, no alterarlo: `sync_sage_distributor`, `nif`,
`via_tipo`, `address`, `zip_code`, `geo_municipality`, `geo_province`,
`facturacion_anual`, `facturacion_desglosada`, `facturas`, `pedidos`,
`descubierto`, `credito_y_caucion`, `credito_y_caucion_descubierto`, `descuento`,
`descuento_weinor`, `descuento_mallorquinas`, `credito_interno`, `credito_latente`.

`facturacion_desglosada` es el formato antiguo agregado y se mantiene solo hasta
que Griesser migre su programación a `facturas`. Al retirarlo se ahorran ~86 KB
por envío del peor cliente.

## Pendiente de verificar

- **`referencia_cliente` sale vacío.** Mapeado a `SuPedido`, que está a cero en
  las 10.771 líneas facturadas del periodo y en las 1.372 cabeceras de pedido.
  La captura de producción sí mostraba contenido en "Ref. ped. cliente".
  Confirmar el campo antes de darlo por bueno.
- **Límite de body del servidor del CRM.** Sin confirmar. El peor cliente envía
  unos 300 KB; está holgado frente a los defaults habituales pero nadie lo ha
  verificado con Griesser.
- **`pedido_online` sin ejercitar.** El campo está a 0 en las 19.702 facturas,
  19.978 pedidos y 49.058 líneas de staging. La conversión -1 → 1 no se ha
  probado con datos reales.
- **Pedidos borrador en `Estado = 0`.** Algunos tienen base imponible 0, sin
  color y sin persianas. Decidir si deben viajar al CRM.
- **Rendimiento.** La query de facturas tarda ~1 s por cliente. Si se acumulan
  pendientes, ahí está el cuello de botella; se arreglaría con un índice sobre
  `LineasAlbaranCliente` por las claves de factura.

## Convenciones

Validar entradas antes de tocar base de datos. Capturar y registrar errores, no
silenciarlos. Un fallo en facturas, pedidos o desglose no debe abortar el envío
de datos maestros: se manda el campo vacío y el error queda en el log.
Transacciones para lo que afecte a más de una entidad, condicionadas a que el
provider no sea InMemory (los tests lo usan).

El despliegue está en `DEPLOYMENT.md`: publicar con `scripts\publish.ps1`, copiar
a `C:\Servicios\GriesserPresuSync\`, parar y arrancar el servicio. Cuidado con
sobrescribir el `appsettings.json` del servidor, que lleva la cadena de conexión
de producción.
