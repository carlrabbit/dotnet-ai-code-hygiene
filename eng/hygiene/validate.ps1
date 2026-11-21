$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $root
dotnet build .\src\DotNetAiCodeHygiene.Cli\DotNetAiCodeHygiene.Cli.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Hygiene build failed ($LASTEXITCODE)." }
dotnet test .\tests\DotNetAiCodeHygiene.Core.Tests\DotNetAiCodeHygiene.Core.Tests.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Hygiene Core tests failed ($LASTEXITCODE)." }
dotnet test .\tests\DotNetAiCodeHygiene.Cli.Tests\DotNetAiCodeHygiene.Cli.Tests.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Hygiene CLI tests failed ($LASTEXITCODE)." }
dotnet pack .\src\DotNetAiCodeHygiene.Cli\DotNetAiCodeHygiene.Cli.csproj -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Hygiene pack failed ($LASTEXITCODE)." }
& (Join-Path $root 'eng/hygiene/validate-consumer.ps1')
if ($LASTEXITCODE -ne 0) { throw "Hygiene installed consumer failed ($LASTEXITCODE)." }
