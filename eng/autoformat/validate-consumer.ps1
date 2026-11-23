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
            $formatResult = & $shim format FormatOnly.cs --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $formatResult.selectedPaths.Count -ne 1 -or $formatResult.changedPaths -notcontains 'FormatOnly.cs') { throw 'Installed Autoformat whitespace mutation did not report the explicit changed file.' }
            git add FormatOnly.cs; git commit -m 'format source' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Could not checkpoint formatted source.' }
            $normalizeResult = & $shim normalize Sample.cs --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $normalizeResult.selectedPaths.Count -ne 1 -or $normalizeResult.changedPaths -notcontains 'Sample.cs') { throw 'Installed Autoformat explicit normalization did not mutate the selected path.' }
            git add Sample.cs; git commit -m 'normalize source' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'Could not checkpoint normalized source.' }
            $beforeRepeat = Get-FileHash Sample.cs -Algorithm SHA256
            $repeat = & $shim normalize Sample.cs --output json | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $repeat.changedCount -ne 0 -or (Get-FileHash Sample.cs -Algorithm SHA256).Hash -ne $beforeRepeat.Hash) { throw 'Installed Autoformat normalization was not stable on a clean repeat.' }
            $beforeRemovedFlag = Get-FileHash Sample.cs -Algorithm SHA256
            $removedCheck = & $shim normalize --check --output json 2>&1 | Out-String
            if ($LASTEXITCODE -eq 0 -or (Get-FileHash Sample.cs -Algorithm SHA256).Hash -ne $beforeRemovedFlag.Hash) { throw 'Removed Autoformat --check was accepted or changed source.' }
            Set-Content Staged.cs 'public class Staged { }'; git add Staged.cs
            Set-Content Unstaged.cs 'public class Unstaged { }'
            Set-Content Untracked.cs 'public class Untracked { }'
            $beforeRemovedChanged = @{}; foreach ($file in @('Staged.cs','Unstaged.cs','Untracked.cs')) { $beforeRemovedChanged[$file] = (Get-FileHash $file -Algorithm SHA256).Hash }
            $removedChanged = & $shim --changed --output json 2>&1 | Out-String
            if ($LASTEXITCODE -eq 0) { throw 'Removed Autoformat --changed was accepted.' }
            foreach ($file in $beforeRemovedChanged.Keys) { if ((Get-FileHash $file -Algorithm SHA256).Hash -ne $beforeRemovedChanged[$file]) { throw "Rejected Autoformat --changed modified '$file'." } }
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
    [pscustomobject]@{ schemaVersion=1; milestone='M0026'; product='autoformat'; package=$id; version=$version; packageSha256=$packageSha256; installedShimSha256=$shimSha256; consumerScenarios=@('exact-current-install','version','help','fresh-init','explicit-path','format-write','normalize-write','normalize-idempotence','removed-switch-rejection','dirty-mutation-rejection','hygiene-policy-adoption') } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $evidence -Encoding utf8
    Write-Output "PASS installed exact-current Autoformat package $id $version ($packageSha256); shim $shimSha256"
} finally { $env:NUGET_PACKAGES = $old }
