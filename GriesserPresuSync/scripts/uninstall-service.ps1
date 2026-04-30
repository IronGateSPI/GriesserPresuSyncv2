<#
.SYNOPSIS
    Desinstala el servicio Griesser Presu Sync.

.DESCRIPTION
    Para el servicio si está corriendo y lo elimina del SCM.
    Debe ejecutarse como Administrador.
#>
[CmdletBinding()]
param(
    [string] $ServiceName = 'GriesserPresuSync'
)

$ErrorActionPreference = 'Stop'

$current = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($current)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Este script debe ejecutarse como Administrador."
}

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $svc) {
    Write-Host "El servicio '$ServiceName' no existe; nada que hacer." -ForegroundColor Yellow
    return
}

if ($svc.Status -ne 'Stopped') {
    Write-Host "Parando '$ServiceName' ..." -ForegroundColor Cyan
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    for ($i=0; $i -lt 30 -and (Get-Service -Name $ServiceName).Status -ne 'Stopped'; $i++) {
        Start-Sleep -Milliseconds 500
    }
}

Write-Host "Eliminando '$ServiceName' ..." -ForegroundColor Cyan
& sc.exe delete $ServiceName | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "sc.exe delete falló (código $LASTEXITCODE)"
}

Write-Host "Servicio '$ServiceName' desinstalado." -ForegroundColor Green
