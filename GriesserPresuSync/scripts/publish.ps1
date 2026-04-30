<#
.SYNOPSIS
    Publica GriesserPresuSync como Windows Service self-contained (win-x64).

.DESCRIPTION
    Genera un paquete autocontenido en ./publish/ que incluye el runtime de
    .NET Core 3.1, de forma que el servidor de destino NO necesita tener
    .NET Core instalado (recordar que 3.1 está EOL desde diciembre de 2022).

    Uso típico:
        cd <repo>
        .\scripts\publish.ps1
    Y luego copiar el contenido de ./publish/ al servidor (p. ej.
    C:\Servicios\GriesserPresuSync\) y lanzar install-service.ps1 allí.

.PARAMETER Configuration
    Release (por defecto) o Debug.

.PARAMETER OutputPath
    Carpeta de salida. Por defecto: <repo>/publish

.PARAMETER SingleFile
    Si se pasa $true, genera un único .exe (auto-extraído al primer arranque).
    Por defecto $false (carpeta con todos los .dll, más fácil de inspeccionar).
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $OutputPath = '',
    [bool]   $SingleFile = $false
)

$ErrorActionPreference = 'Stop'

# Localizar la raíz del repositorio (un nivel por encima de /scripts).
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$Csproj   = Join-Path $RepoRoot 'GriesserPresuSync.csproj'

if (-not (Test-Path $Csproj)) {
    Write-Error "No se encuentra $Csproj. ¿Está bien la estructura del repo?"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot 'publish'
}

Write-Host "Limpiando $OutputPath ..." -ForegroundColor Cyan
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}

$publishArgs = @(
    'publish', $Csproj,
    '-c', $Configuration,
    '-r', 'win-x64',
    '--self-contained', 'true',
    '-o', $OutputPath,
    "/p:PublishSingleFile=$($SingleFile.ToString().ToLower())",
    '/p:DebugType=None',
    '/p:DebugSymbols=false'
)

Write-Host "Ejecutando: dotnet $($publishArgs -join ' ')" -ForegroundColor Cyan
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish falló con código $LASTEXITCODE"
}

# Comprobar que el .exe esté ahí.
$Exe = Join-Path $OutputPath 'GriesserPresuSync.exe'
if (-not (Test-Path $Exe)) {
    Write-Error "No se ha generado $Exe. Algo ha ido mal en el publish."
}

Write-Host ""
Write-Host "OK. Paquete listo en:" -ForegroundColor Green
Write-Host "  $OutputPath" -ForegroundColor Green
Write-Host ""
Write-Host "Siguientes pasos:" -ForegroundColor Yellow
Write-Host "  1. Copiar el contenido de '$OutputPath' al servidor (p. ej. C:\Servicios\GriesserPresuSync\)"
Write-Host "  2. Ajustar appsettings.json (cadena de conexión a SQL) en esa carpeta"
Write-Host "  3. Ejecutar (como Administrador) scripts\install-service.ps1 en el servidor"
