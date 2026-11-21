$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$id = 'DotNetAiWorkbench.Autoformat.Tool'; $version = '0.1.0'; $command = 'dotnet-autoformat'
$package = Join-Path $root "artifacts/packages/$id.$version.nupkg"
if (-not (Test-Path $package)) { throw "Current Autoformat package is missing: $package" }
$temp = Join-Path ([IO.Path]::GetTempPath()) ("m0026-autoformat-" + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $temp feed; $tools = Join-Path $temp tools; $cache = Join-Path $temp cache
New-Item -ItemType Directory -Force $feed,$tools,$cache | Out-Null
Copy-Item $package $feed
$config = Join-Path $temp NuGet.config
$nugetConfigContent = "<configuration><packageSources><clear/><add key=`"local`" value=`"$feed`"/><add key=`"nuget`" value=`"https://api.nuget.org/v3/index.json`"/></packageSources></configuration>"
Set-Content -LiteralPath $config -Value $nugetConfigContent
$old = $env:NUGET_PACKAGES; $env:NUGET_PACKAGES = $cache
try {
    dotnet tool install $id --tool-path $tools --configfile $config --version $version
    if ($LASTEXITCODE -ne 0) { throw 'Exact current Autoformat package install failed.' }
    $shim = Join-Path $tools "$command.exe"
    if (-not (Test-Path $shim)) { throw 'Installed Autoformat shim missing.' }
    $shimSha256 = (Get-FileHash -LiteralPath $shim -Algorithm SHA256).Hash.ToLowerInvariant()
        $reported = & $shim --version
        if ($LASTEXITCODE -ne 0 -or ($reported -join '').Trim() -ne $version) { throw 'Installed Autoformat version mismatch.' }
        $autoHelp = & $shim --help
        if ($LASTEXITCODE -ne 0 -or ($autoHelp -join "`n") -notmatch 'format' -or ($autoHelp -join "`n") -notmatch 'normalize') { throw 'Installed Autoformat help is incomplete.' }
        $consumer = Join-Path $temp consumer; New-Item -ItemType Directory $consumer | Out-Null
        Push-Location $consumer
        try {
            git init -q -b main; if ($LASTEXITCODE -ne 0) { throw 'git init failed.' }
            git config user.email m0026@example.invalid; git config user.name 'M0026 Autoformat Consumer'
            Set-Content .gitignore "**/bin/`n**/obj/`n"
            Set-Content Consumer.csproj '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
            Set-Content Sample.cs 'using System; public class Sample{public object Create(){return new object();}}'
            Set-Content FormatOnly.cs 'public class FormatOnly{public int Value(){return 1;}}'
            dotnet restore .\Consumer.csproj
            if ($LASTEXITCODE -ne 0) { throw 'Autoformat consumer restore failed.' }
            $init = & $shim init --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $init.status -ne 'initialized') { throw 'Installed Autoformat init failed.' }
            $policyHash = (Get-FileHash .editorconfig -Algorithm SHA256).Hash
            $repeatInit = & $shim init --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $repeatInit.status -ne 'ready' -or (Get-FileHash .editorconfig -Algorithm SHA256).Hash -ne $policyHash) { throw 'Installed Autoformat init was not byte-stable on repeat.' }
            git add -A; git commit -m baseline | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Autoformat consumer baseline commit failed.' }
            $formatCheck = & $shim format FormatOnly.cs --check --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $formatCheck.changedPaths -notcontains 'FormatOnly.cs') { throw 'Installed Autoformat whitespace check failed.' }
            & $shim format FormatOnly.cs
            if ($LASTEXITCODE -ne 0) { throw 'Installed Autoformat whitespace mutation failed.' }
            git add FormatOnly.cs; git commit -m 'format source' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Could not checkpoint formatted source.' }
            $normalizeCheck = & $shim normalize --check --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $normalizeCheck.changedPaths -notcontains 'Sample.cs') { throw 'Installed Autoformat style and whitespace check failed.' }
            $explicitCheck = & $shim normalize Sample.cs --check --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $explicitCheck.selectedPaths.Count -ne 1 -or $explicitCheck.selectedPaths[0] -ne 'Sample.cs') { throw 'Installed Autoformat explicit path did not narrow selection.' }
            & $shim normalize
            if ($LASTEXITCODE -ne 0) { throw 'Installed Autoformat normalize mutation failed.' }
            git add Sample.cs; git commit -m 'normalize source' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Could not checkpoint normalized source.' }
            & $shim normalize
            if ($LASTEXITCODE -ne 0 -or (git status --porcelain).Length -ne 0) { throw 'Installed Autoformat normalize was not idempotent.' }
            Set-Content Staged.cs 'public class Staged { }'; git add Staged.cs
            Set-Content Unstaged.cs 'public class Unstaged { }'
            Set-Content Untracked.cs 'public class Untracked { }'
            $beforeChanged = @{}; foreach ($file in @('Staged.cs','Unstaged.cs','Untracked.cs')) { $beforeChanged[$file] = (Get-FileHash $file -Algorithm SHA256).Hash }
            $changed = & $shim --changed --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or -not $changed.checkOnly -or @($changed.selectedPaths | Where-Object { $_ -in @('Staged.cs','Unstaged.cs','Untracked.cs') }).Count -ne 3) { throw 'Installed Autoformat --changed did not select staged, unstaged and untracked C# files read-only.' }
            foreach ($file in $beforeChanged.Keys) { if ((Get-FileHash $file -Algorithm SHA256).Hash -ne $beforeChanged[$file]) { throw "Autoformat --changed modified '$file'." } }
            $dirtyMutation = & $shim format 2>&1 | Out-String
            if ($LASTEXITCODE -ne 2 -or $dirtyMutation -notmatch 'clean Git worktree') { throw 'Installed Autoformat mutation did not reject the dirty checkpoint.' }
        } finally { Pop-Location }
        $adoptionRoot = Join-Path $temp adoption-consumer; New-Item -ItemType Directory $adoptionRoot | Out-Null
        Push-Location $adoptionRoot
        try {
            git init -q -b main; if ($LASTEXITCODE -ne 0) { throw 'Autoformat adoption fixture git init failed.' }
            Set-Content Consumer.csproj '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
            Set-Content Sample.cs 'public class Sample { }'
            Set-Content .editorconfig "# retained user note`nroot = true`n# hygiene profile:begin`n[*.cs]`ncsharp_prefer_braces = true`ndotnet_diagnostic.IDE0011.severity = error`n# hygiene profile:end`n"
            dotnet restore .\Consumer.csproj
            if ($LASTEXITCODE -ne 0) { throw 'Autoformat adoption fixture restore failed.' }
            $adopted = & $shim init --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or -not $adopted.adoptedExistingHygienePolicy -or $adopted.changedPaths -notcontains '.editorconfig') { throw 'Installed Autoformat did not transfer the hygiene-managed policy.' }
            $configText = Get-Content .editorconfig -Raw
            $hygieneBlock = $configText.Substring($configText.IndexOf('# hygiene profile:begin'), $configText.IndexOf('# hygiene profile:end') - $configText.IndexOf('# hygiene profile:begin'))
            if ($configText -notmatch '# retained user note' -or $configText -notmatch '# autoformat profile:begin' -or $hygieneBlock.Contains('dotnet_diagnostic.IDE0011.severity') -or (Test-Path .hygiene)) { throw 'Autoformat adoption overwrote user text or retained overlapping hygiene policy ownership.' }
        } finally { Pop-Location }
    $evidence = Join-Path $root '.execution/validation/autoformat-consumer-provenance.json'
    New-Item -ItemType Directory -Force -Path (Split-Path $evidence) | Out-Null
    $packageSha256 = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
    [pscustomobject]@{ schemaVersion=1; milestone='M0026'; product='autoformat'; package=$id; version=$version; packageSha256=$packageSha256; installedShimSha256=$shimSha256; consumerScenarios=@('exact-current-install','version','help','fresh-init','explicit-path','format-check-and-write','normalize-check-and-write','normalize-idempotence','changed-staged-unstaged-untracked-read-only','dirty-mutation-rejection','hygiene-policy-adoption') } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $evidence -Encoding utf8
    Write-Output "PASS installed exact-current Autoformat package $id $version ($packageSha256); shim $shimSha256"
} finally { $env:NUGET_PACKAGES = $old }
