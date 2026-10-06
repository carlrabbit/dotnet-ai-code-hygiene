$ErrorActionPreference = 'Stop'

Set-Location (Join-Path $PSScriptRoot '..')

dotnet restore .\DotNetAiCodeHygiene.slnx
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

dotnet build .\DotNetAiCodeHygiene.slnx --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE." }

dotnet test .\DotNetAiCodeHygiene.slnx --configuration Release --no-build
if ($LASTEXITCODE -ne 0) { throw "dotnet test failed with exit code $LASTEXITCODE." }

dotnet pack .\src\DotNetAiCodeHygiene.Cli\DotNetAiCodeHygiene.Cli.csproj --configuration Release --no-build
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed with exit code $LASTEXITCODE." }

# Tier 4: install the exact package just packed into an isolated consumer tool path.
$package = Get-ChildItem .\artifacts\packages\DotNetAiCodeHygiene.Tool.0.5.0.nupkg
$tierRoot = Join-Path ([IO.Path]::GetTempPath()) ('hygiene-tier4-' + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $tierRoot 'feed'; $toolPath = Join-Path $tierRoot 'tools'; $consumer = Join-Path $tierRoot 'consumer'; $nugetCache = Join-Path $tierRoot 'nuget-cache'
$nugetConfig = Join-Path $tierRoot 'NuGet.config'
New-Item -ItemType Directory -Force $feed, $toolPath, $consumer, $nugetCache | Out-Null
Copy-Item -LiteralPath $package.FullName -Destination $feed
if ((Get-FileHash -LiteralPath (Join-Path $feed $package.Name) -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash) { throw 'Tier-4 local feed package does not match the package just packed.' }
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="CurrentRun" value="$feed" />
    <add key="NuGetOfficial" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="CurrentRun">
      <package pattern="DotNetAiCodeHygiene.Tool" />
    </packageSource>
    <packageSource key="NuGetOfficial">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfig -Encoding utf8
$previousNugetPackages = $env:NUGET_PACKAGES
$env:NUGET_PACKAGES = $nugetCache
dotnet tool install DotNetAiCodeHygiene.Tool --tool-path $toolPath --configfile $nugetConfig --version 0.5.0
if ($LASTEXITCODE -ne 0) { throw "Tier-4 tool install failed with exit code $LASTEXITCODE." }
$hygiene = Join-Path $toolPath 'hygiene.exe'
if (-not (Test-Path -LiteralPath $hygiene)) { $hygiene = Join-Path $toolPath 'hygiene' }
if (-not (Test-Path -LiteralPath $hygiene)) { throw 'Tier-4 installed hygiene command was not created.' }
Push-Location $consumer
try {
    git init -q
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 fixture git init failed.' }
    Set-Content -NoNewline -Path Consumer.csproj -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
    Set-Content -NoNewline -Path Sample.cs -Value 'public class Sample{private int value;public int Value(){return this.value;}}'
    & $hygiene --version
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene --version failed.' }
    $version = & $hygiene --version
    if ($LASTEXITCODE -ne 0 -or ($version -join '') -notmatch '0\.5\.0') { throw 'Installed hygiene version does not match 0.5.0.' }
    & $hygiene help --agent | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene help --agent failed.' }
    $formatBefore = Get-Content -Raw Sample.cs
    $formatCheck = & $hygiene format --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $formatCheck.changedCount -lt 1) { throw 'Installed hygiene format --check did not report pending changes.' }
    if ((Get-Content -Raw Sample.cs) -cne $formatBefore) { throw 'Installed format --check modified source.' }
    & $hygiene format | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene format mutation failed.' }
    $formatted = Get-Content -Raw Sample.cs
    if ($formatted -ceq $formatBefore) { throw 'Installed format mutation did not change source.' }
    $formatClean = & $hygiene format --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $formatClean.changedCount -ne 0) { throw 'Installed format was not idempotent.' }
    $normalizeBefore = Get-Content -Raw Sample.cs
    $normalizeCheck = & $hygiene normalize --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $normalizeCheck.changedCount -lt 1) { throw 'Installed hygiene normalize --check did not report pending changes.' }
    if ((Get-Content -Raw Sample.cs) -cne $normalizeBefore) { throw 'Installed normalize --check modified source.' }
    & $hygiene normalize | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene normalize mutation failed.' }
    $normalized = Get-Content -Raw Sample.cs
    if ($normalized -ceq $normalizeBefore) { throw 'Installed normalize mutation did not change source.' }
    $normalizeClean = & $hygiene normalize --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $normalizeClean.changedCount -ne 0) { throw 'Installed normalize was not idempotent.' }
    $bootstrap = & $hygiene bootstrap --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $bootstrap.findingCount -ne 0) { throw 'Installed bootstrap failed or left profile findings.' }
    $update = & $hygiene update --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $update.findingCount -ne 0 -or $update.changedPaths.Count -ne 0) { throw 'Installed update was not clean and idempotent.' }
    $rules = & $hygiene rules --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or -not (($rules.rules | Where-Object id -eq 'profile.dotnet.analysis.required').configurable -eq $false)) { throw 'Installed mandatory profile rule is missing or configurable.' }
    Set-Content -NoNewline -Path Sample.cs -Value "/// <summary>Sample API</summary>`npublic class Sample { public void Run() { } }`n"
    $check = & $hygiene check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene check failed.' }
    if (-not ($check.findings | Where-Object ruleId -eq 'docs.summary.required')) { throw 'Installed check did not report a missing API summary.' }
    if (-not ($check.findings | Where-Object ruleId -eq 'docs.text.sentence')) { throw 'Installed check did not report missing summary punctuation.' }
    $quality = $check.reviewBatches | Where-Object ruleId -eq 'docs.summary.quality.review'
    $german = $check.reviewBatches | Where-Object ruleId -eq 'docs.summary.language.german.review'
    if (-not $quality -or $quality.ruleVersion -ne 3 -or -not $german -or $german.ruleVersion -ne 1) { throw 'Installed check did not emit the independent v3 quality and v1 German review batches.' }
    if (-not ($german.questions[0].text -match 'natural, comprehensible German')) { throw 'Installed German review rubric is incorrect.' }
    & $hygiene rules disable docs.summary.language.german.review | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed German rule could not be disabled.' }
    $qualityOnly = & $hygiene check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $qualityOnly.reviewBatches.Count -ne 1 -or $qualityOnly.reviewBatches[0].ruleId -ne 'docs.summary.quality.review') { throw 'Disabling the installed German rule changed or removed language-neutral quality review.' }
}
finally {
    Pop-Location
    $env:NUGET_PACKAGES = $previousNugetPackages
    Remove-Item -LiteralPath $tierRoot -Recurse -Force
}
