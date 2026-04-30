<#
.SYNOPSIS
    Instala GriesserPresuSync como Servicio de Windows (LocalSystem,
    arranque automático, con reinicio automático ante fallo).

.DESCRIPTION
    Debe ejecutarse en el servidor de destino, **como Administrador**.
    Se asume que el binario self-contained ya está copiado en -BinPath.

    Crea el servicio con sc.exe (más fiable que New-Service en máquinas con
    PowerShell antiguo) y configura:
      - start= auto         (arranque automático con Windows)
      - obj= LocalSystem    (cuenta de sistema)
      - failure actions     (reinicio automático tras 1 minuto, hasta 3 veces;
                             reset del contador a las 24h)
      - description amigable

.PARAMETER ServiceName
    Nombre interno del servicio. Por defecto 'GriesserPresuSync'.

.PARAMETER DisplayName
    Texto que verá el usuario en services.msc.

.PARAMETER BinPath
    Carpeta donde está el .exe ya publicado. Por defecto C:\Servicios\GriesserPresuSync.

.EXAMPLE
    .\install-service.ps1
.EXAMPLE
    .\install-service.ps1 -BinPath D:\Apps\GriesserPresuSync
#>
[CmdletBinding()]
param(
    [string] $ServiceName = 'GriesserPresuSync',
    [string] $DisplayName = 'Griesser Presu Sync',
    [string] $Description = 'Sincroniza presupuestos y mallorquinas desde la API de migriesser.com a las tablas intermedias del ERP.',
    [string] $BinPath     = 'C:\Servicios\GriesserPresuSync'
)

$ErrorActionPreference = 'Stop'

# --- 0. Comprobar que vamos como Admin ---
$current = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($current)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Este script debe ejecutarse como Administrador."
}

# --- 1. Comprobar el .exe ---
$Exe = Join-Path $BinPath 'GriesserPresuSync.exe'
if (-not (Test-Path $Exe)) {
    Write-Error "No se encuentra $Exe. Copia primero el paquete publicado a $BinPath."
}

# --- 2. Si ya existe, paramos y borramos para reinstalar limpio ---
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Servicio '$ServiceName' ya existe, parándolo y eliminándolo para reinstalar..." -ForegroundColor Yellow
    if ($existing.Status -ne 'Stopped') {
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        # Esperar a que pare
        for ($i=0; $i -lt 30 -and (Get-Service -Name $ServiceName).Status -ne 'Stopped'; $i++) {
            Start-Sleep -Milliseconds 500
        }
    }
    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
}

# --- 3. Crear el servicio ---
# OJO: sc.exe espera un espacio DESPUÉS del '=' en sus parámetros.
$binPathQuoted = "`"$Exe`""
Write-Host "Creando servicio '$ServiceName' apuntando a $Exe ..." -ForegroundColor Cyan
$createOut = & sc.exe create $ServiceName binPath= $binPathQuoted start= auto obj= LocalSystem DisplayName= "`"$DisplayName`""
if ($LASTEXITCODE -ne 0) {
    Write-Error "sc.exe create falló: $createOut"
}

# --- 4. Descripción ---
& sc.exe description $ServiceName "`"$Description`"" | Out-Null

# --- 5. Failure actions: reset 24h, restart cada 60s hasta 3 veces ---
& sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

# --- 6. Arrancar ---
Write-Host "Arrancando '$ServiceName' ..." -ForegroundColor Cyan
Start-Service -Name $ServiceName

Start-Sleep -Seconds 2
$svc = Get-Service -Name $ServiceName
Write-Host ""
Write-Host "Servicio instalado y arrancado." -ForegroundColor Green
Write-Host "  Nombre:   $($svc.Name)"
Write-Host "  Display:  $($svc.DisplayName)"
Write-Host "  Estado:   $($svc.Status)"
Write-Host ""
Write-Host "Logs en: Visor de Eventos -> Registros de Windows -> Application -> Source 'GriesserPresuSync'" -ForegroundColor Yellow
