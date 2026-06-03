$ErrorActionPreference = "Stop"

Write-Host "1. Limpando publicacoes anteriores..."
if (Test-Path "publish") { Remove-Item -Recurse -Force "publish" }

Write-Host "2. Compilando e publicando o projeto..."
dotnet publish -c Release -r win-x64 --self-contained false -o publish

$version = Read-Host "Versao do instalador (ex: 1.0.0)"

Write-Host "3. Gerando o instalador com Velopack..."
vpk pack -u PokeCollection -v $version -p publish -e PokeCollection.exe -i "icon_exe.ico"

Write-Host ""
Write-Host "====================================================="
Write-Host "Concluido! O instalador foi gerado com sucesso."
Write-Host "Abra a pasta 'Releases' para encontrar o 'PokeCollection-Setup.exe'."
Write-Host "====================================================="