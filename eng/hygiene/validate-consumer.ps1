$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$id = 'DotNetAiCodeHygiene.Tool'; $version = '0.12.0'; $command = 'dotnet-hygiene'
$package = Join-Path $root "artifacts/packages/$id.$version.nupkg"
if (-not (Test-Path $package)) { throw "Current Hygiene package is missing: $package" }
$temp = Join-Path ([IO.Path]::GetTempPath()) ("m0026-hygiene-" + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $temp feed; $tools = Join-Path $temp tools; $cache = Join-Path $temp cache
New-Item -ItemType Directory -Force $feed,$tools,$cache | Out-Null
Copy-Item $package $feed
$config = Join-Path $temp NuGet.config
$nugetConfigContent = "<configuration><packageSources><clear/><add key=`"local`" value=`"$feed`"/><add key=`"nuget`" value=`"https://api.nuget.org/v3/index.json`"/></packageSources></configuration>"
Set-Content -LiteralPath $config -Value $nugetConfigContent
$old = $env:NUGET_PACKAGES; $env:NUGET_PACKAGES = $cache
try {
    dotnet tool install $id --tool-path $tools --configfile $config --version $version
    if ($LASTEXITCODE -ne 0) { throw 'Exact current Hygiene package install failed.' }
    $shim = Join-Path $tools "$command.exe"
    if (-not (Test-Path $shim)) { throw 'Installed Hygiene shim missing.' }
    $shimSha256 = (Get-FileHash -LiteralPath $shim -Algorithm SHA256).Hash.ToLowerInvariant()
        $reported = & $shim --version
        if ($LASTEXITCODE -ne 0 -or ($reported -join '').Trim() -ne $version) { throw 'Installed Hygiene version mismatch.' }
        $help = & $shim --help
        if ($LASTEXITCODE -ne 0 -or ($help -join "`n") -notmatch 'bootstrap') { throw 'Installed Hygiene command surface unavailable.' }
        $consumer = Join-Path $temp consumer; New-Item -ItemType Directory $consumer | Out-Null
        Push-Location $consumer
        try {
            git init -q -b main; if ($LASTEXITCODE -ne 0) { throw 'git init failed.' }
            git config user.email m0026@example.invalid; git config user.name 'M0026 Hygiene Consumer'
            Set-Content .gitignore "**/bin/`n**/obj/`n**/.hygiene/.state/`n"
            Set-Content Consumer.csproj '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
            Set-Content Sample.cs 'public class Sample { }'
            dotnet restore .\Consumer.csproj
            if ($LASTEXITCODE -ne 0) { throw 'Hygiene consumer restore failed.' }
            git add .; git commit -m baseline | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Hygiene consumer baseline commit failed.' }
            $bootstrap = & $shim bootstrap --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $bootstrap.status -ne 'approval-required') { throw 'Installed Hygiene bootstrap did not produce its explicit approval step.' }
            git add -A; git commit -m 'initialize hygiene profile' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Hygiene profile commit failed.' }
            & $shim baseline accept --approver 'M0026 consumer reviewer' --report-sha256 $bootstrap.reportSha256
            if ($LASTEXITCODE -ne 0) { throw 'Hygiene baseline acceptance failed.' }
            & $shim update --output json | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Installed Hygiene update failed.' }
            & $shim check --output json | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Installed Hygiene check failed.' }
        $review = & $shim check --review-all --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or @($review.reviewBatches).Count -eq 0 -or @($review.reviewBatches | Where-Object sampleCount -gt 0).Count -eq 0) { throw 'Installed Hygiene semantic review discovery did not return bounded review work.' }
            Set-Content -Path Sample.cs -Value 'using System; public class Sample{public object Create(){return new object();}}' -NoNewline
            git add Sample.cs; git commit -m 'manual format input' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Could not checkpoint Hygiene format input.' }
            $formatCheck = & $shim format --check --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $formatCheck.changedPaths -notcontains 'Sample.cs') { throw 'Installed Hygiene format check failed.' }
            & $shim format
            if ($LASTEXITCODE -ne 0) { throw 'Installed Hygiene delegated format failed.' }
            git add Sample.cs; git commit -m 'format via hygiene adapter' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Could not checkpoint Hygiene formatting.' }
            Set-Content -Path Sample.cs -Value 'public class Sample{public object Create = new object();}' -NoNewline
            git add Sample.cs; git commit -m 'manual normalize input' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Could not checkpoint Hygiene normalize input.' }
            & $shim normalize
            if ($LASTEXITCODE -ne 0) { throw 'Installed Hygiene delegated normalize failed.' }
        } finally { Pop-Location }
    $evidence = Join-Path $root '.execution/validation/hygiene-consumer-provenance.json'
    New-Item -ItemType Directory -Force -Path (Split-Path $evidence) | Out-Null
    $packageSha256 = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
    [pscustomobject]@{ schemaVersion=1; milestone='M0026'; product='hygiene'; package=$id; version=$version; packageSha256=$packageSha256; installedShimSha256=$shimSha256; consumerScenarios=@('exact-current-install','version','help','fresh-bootstrap','explicit-baseline-acceptance','update','check','semantic-review-discovery','delegated-format-check-and-write','delegated-normalize-write') } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $evidence -Encoding utf8
    Write-Output "PASS installed exact-current Hygiene package $id $version ($packageSha256); shim $shimSha256"
} finally { $env:NUGET_PACKAGES = $old }
