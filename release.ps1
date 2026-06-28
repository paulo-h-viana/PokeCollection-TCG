#Requires -Version 7
<#
.SYNOPSIS
    Publica uma nova versao do PokeCollection no GitHub Releases (Velopack).
.DESCRIPTION
    Encapsula todo o pipeline de release num comando so, com os gotchas ja
    tratados (ver memoria release-process):
      - limpa a pasta publish antes (evita aninhamento publish/publish)
      - publica self-contained com -p:Version (FileVersion = tag)
      - baixa a release anterior como base para o delta
      - redireciona TEMP para o D: (C: vive sem espaco; vpk pack estoura senao)
      - empacota e sobe como vX.Y.Z marcando como Latest
      - opcionalmente da push da branch atual
.EXAMPLE
    ./release.ps1 -Version 1.5.3
.EXAMPLE
    ./release.ps1 -Version 1.5.3 -Push
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [switch]$Push
)

$ErrorActionPreference = 'Stop'
$root    = $PSScriptRoot
$repo    = 'https://github.com/paulo-h-viana/PokeCollection-TCG'
$publish = Join-Path $root 'publish'
$rel     = Join-Path $root 'Releases'
$vptmp   = Join-Path $root '.vptmp'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Action)
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "Falhou em '$Name' (exit $LASTEXITCODE)." }
}

$gh = (Get-Command gh -ErrorAction SilentlyContinue)?.Source
if (-not $gh) { $gh = 'C:\Program Files\GitHub CLI\gh.exe' }
if (-not (Test-Path $gh)) { throw "gh CLI nao encontrado em PATH nem em '$gh'." }

$tok = (& $gh auth token).Trim()
if ([string]::IsNullOrWhiteSpace($tok)) { throw 'Nao foi possivel obter o token do gh (gh auth login?).' }

$changelogPath = Join-Path $root 'changelog.json'
if (Test-Path $changelogPath) {
    $changelog = Get-Content $changelogPath -Raw | ConvertFrom-Json
    if (-not ($changelog.PSObject.Properties.Name -contains $Version)) {
        Write-Warning "changelog.json nao tem entrada para a versao $Version. O modal 'O que ha de novo' nao aparecera para esta versao. Adicione a entrada antes de publicar."
    }
}

Write-Host "Publicando PokeCollection v$Version" -ForegroundColor Green

Invoke-Step 'Limpando publish/' {
    Remove-Item $publish -Recurse -Force -ErrorAction SilentlyContinue
    $global:LASTEXITCODE = 0
}

Invoke-Step 'dotnet publish (self-contained)' {
    dotnet publish (Join-Path $root 'PokeCollection.csproj') `
        -c Release -r win-x64 --self-contained true -o $publish -p:Version=$Version --nologo
}

Invoke-Step 'Baixando release base (delta)' {
    New-Item -ItemType Directory -Force $rel | Out-Null
    vpk download github --repoUrl $repo --token $tok --outputDir $rel
}

Invoke-Step "Empacotando v$Version (TEMP no D:)" {
    New-Item -ItemType Directory -Force $vptmp | Out-Null
    $env:TEMP = $vptmp
    $env:TMP  = $vptmp
    vpk pack -u PokeCollection -v $Version -p $publish -e PokeCollection.exe `
        -i (Join-Path $root 'icon_exe.ico') -o $rel
}

Invoke-Step "Subindo v$Version para o GitHub" {
    vpk upload github --repoUrl $repo --token $tok --outputDir $rel `
        --tag "v$Version" --releaseName "v$Version" --publish true
}

Remove-Item $vptmp -Recurse -Force -ErrorAction SilentlyContinue

if ($Push) {
    Invoke-Step 'git push' { git push origin (git rev-parse --abbrev-ref HEAD) }
}

Write-Host "==> Verificando" -ForegroundColor Cyan
& $gh release list --repo paulo-h-viana/PokeCollection-TCG --limit 3

Write-Host "OK: v$Version publicada." -ForegroundColor Green
