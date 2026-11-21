$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $root
& (Join-Path $root 'eng/autoformat/validate.ps1')
if ($LASTEXITCODE -ne 0) { throw "Autoformat development validation failed ($LASTEXITCODE)." }
dotnet pack .\src\DotNetAiWorkbench.Autoformat.Cli\DotNetAiWorkbench.Autoformat.Cli.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Autoformat pack failed ($LASTEXITCODE)." }
& (Join-Path $root 'eng/autoformat/validate-consumer.ps1')
if ($LASTEXITCODE -ne 0) { throw "Autoformat installed consumer failed ($LASTEXITCODE)." }
