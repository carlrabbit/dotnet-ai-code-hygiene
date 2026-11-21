$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $root
dotnet build .\src\DotNetAiWorkbench.Autoformat.Cli\DotNetAiWorkbench.Autoformat.Cli.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Autoformat build failed ($LASTEXITCODE)." }
dotnet test .\tests\DotNetAiWorkbench.Autoformat.Tests\DotNetAiWorkbench.Autoformat.Tests.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Autoformat tests failed ($LASTEXITCODE)." }
dotnet pack .\src\DotNetAiWorkbench.Autoformat.Cli\DotNetAiWorkbench.Autoformat.Cli.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Autoformat pack failed ($LASTEXITCODE)." }
& (Join-Path $root 'eng/autoformat/validate-consumer.ps1')
if ($LASTEXITCODE -ne 0) { throw "Autoformat installed consumer failed ($LASTEXITCODE)." }
