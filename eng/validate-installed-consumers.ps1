$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$package = Get-Item .\artifacts\packages\DotNetAiCodeHygiene.Tool.0.11.0.nupkg
$packageSha256 = (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$tierRoot = Join-Path ([IO.Path]::GetTempPath()) ('hygiene-m0024-consumer-' + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $tierRoot 'feed'
$toolPath = Join-Path $tierRoot 'tools'
$consumer = Join-Path $tierRoot 'consumer'
$migration = Join-Path $tierRoot 'migration'
$cache = Join-Path $tierRoot 'nuget-cache'
$report = Join-Path $tierRoot 'audit'
New-Item -ItemType Directory -Force $feed, $toolPath, $consumer, $migration, $cache | Out-Null
Copy-Item -LiteralPath $package.FullName -Destination $feed
if ((Get-FileHash -LiteralPath (Join-Path $feed $package.Name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $packageSha256) { throw 'The isolated feed package differs from the package just packed.' }
$nugetConfig = Join-Path $tierRoot 'NuGet.config'
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear /><add key="CurrentRun" value="$feed" /><add key="NuGetOfficial" value="https://api.nuget.org/v3/index.json" /></packageSources><packageSourceMapping><packageSource key="CurrentRun"><package pattern="DotNetAiCodeHygiene.Tool" /></packageSource><packageSource key="NuGetOfficial"><package pattern="*" /></packageSource></packageSourceMapping></configuration>
"@ | Set-Content -LiteralPath $nugetConfig -Encoding utf8
$oldPath = $env:PATH
$oldPackages = $env:NUGET_PACKAGES
$oldSkipUpdate = $env:DOTNET_HYGIENE_SKIP_UPDATE
$env:PATH = $toolPath + [IO.Path]::PathSeparator + $env:PATH
$env:NUGET_PACKAGES = $cache
$env:DOTNET_HYGIENE_SKIP_UPDATE = '1'
try {
    dotnet tool install DotNetAiCodeHygiene.Tool --tool-path $toolPath --configfile $nugetConfig --version 0.11.0
    if ($LASTEXITCODE -ne 0) { throw "Isolated exact-package install failed ($LASTEXITCODE)." }
    $installed = Join-Path $toolPath 'dotnet-hygiene.exe'
    if (-not (Test-Path -LiteralPath $installed)) { throw 'The installed dotnet-hygiene apphost is missing.' }
    $installedVersion = dotnet hygiene --version
    if ($LASTEXITCODE -ne 0 -or ($installedVersion -join '').Trim() -ne '0.11.0') { throw 'The installed command did not report package version 0.11.0.' }

    Push-Location $consumer
    try {
        git init -q -b main
        git config user.email hygiene-m0024@example.invalid
        git config user.name 'M0024 Installed Consumer'
        Set-Content -NoNewline -Path .gitignore -Value "**/bin/`n**/obj/`n**/.hygiene/.state/`n"
        Set-Content -NoNewline -Path Consumer.csproj -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
        Set-Content -NoNewline -Path Sample.cs -Value 'using System; public class Sample{public object Create(){return new object();}}'
        dotnet restore .\Consumer.csproj --configfile $nugetConfig | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Consumer project restore failed.' }
        git add .gitignore Consumer.csproj Sample.cs
        git commit -m 'consumer baseline' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean consumer checkpoint.' }

        $bootstrap = dotnet hygiene bootstrap --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $bootstrap.status -ne 'approval-required' -or $bootstrap.lifecycle -ne 'bootstrap') { throw 'Installed bootstrap did not prepare an approval-required v3 profile baseline.' }
        $profile = Get-Content -LiteralPath .hygiene/profile.json -Raw | ConvertFrom-Json
        if ($profile.version -ne 3) { throw 'Installed bootstrap did not write profile v3.' }
        $editor = Get-Content -LiteralPath .editorconfig -Raw
        if ($editor -notmatch '(?m)^# hygiene profile:begin$' -or $editor -match '<!-- hygiene profile:' -or $editor -notmatch 'dotnet_diagnostic.IDE0090.severity = warning') { throw 'Installed bootstrap did not generate a valid v3 EditorConfig profile.' }
        if (-not (Test-Path -LiteralPath $bootstrap.reportPath)) { throw 'Installed bootstrap omitted its source-bound baseline report.' }
        git add -A
        git commit -m 'accept reviewed profile artifacts' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not establish the clean profile checkpoint.' }
        dotnet hygiene baseline accept --approver 'Isolated M0024 fixture reviewer' --report-sha256 $bootstrap.reportSha256 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'The exact installed bootstrap report could not be accepted in the isolated fixture.' }

        Set-Content -NoNewline -Path Sample.cs -Value 'using System; public class Sample{public object Create(){return new object();}}'
        git add Sample.cs; git commit -m 'manual format input' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean manual format input.' }
        $formatBefore = Get-Content -LiteralPath Sample.cs -Raw
        $formatCheck = dotnet hygiene format --check --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $formatCheck.targetCount -ne 1 -or $formatCheck.changedPaths -notcontains 'Sample.cs' -or (Get-Content -LiteralPath Sample.cs -Raw) -cne $formatBefore) { throw 'Installed format --check did not report exact changes without writing.' }
        dotnet hygiene format | Out-Null
        if ($LASTEXITCODE -ne 0 -or (Get-Content -LiteralPath Sample.cs -Raw) -ceq $formatBefore) { throw 'Installed SDK whitespace mutation failed or made no change.' }
        git add Sample.cs; git commit -m 'formatted sample' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean formatted source.' }

        Set-Content -NoNewline -Path Sample.cs -Value 'public sealed class Widget { } public class Sample{public Widget Create()=>new Widget();}'
        git add Sample.cs; git commit -m 'manual normalize input' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean manual normalize input.' }
        $normalizeBefore = Get-Content -LiteralPath Sample.cs -Raw
        $normalizeCheck = dotnet hygiene normalize --check --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $normalizeCheck.selectedRuleIds -notcontains 'sdk.IDE0090' -or $normalizeCheck.changedPaths -notcontains 'Sample.cs' -or (Get-Content -LiteralPath Sample.cs -Raw) -cne $normalizeBefore) { throw 'Installed normalize --check did not report the approved SDK diagnostic without writing.' }
        dotnet hygiene normalize | Out-Null
        if ($LASTEXITCODE -ne 0 -or (Get-Content -Raw Sample.cs) -notmatch 'Create\(\) => new\(\);') { throw 'Installed SDK style normalization did not apply IDE0090.' }
        git add Sample.cs; git commit -m 'normalized sample' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean normalized source.' }

        Set-Content -NoNewline -Path .editorconfig -Value "root = true`n[*.cs]`ncsharp_style_var_for_built_in_types = false:warning`ncsharp_style_var_when_type_is_apparent = false:warning`ncsharp_style_var_elsewhere = false:warning`n"
        Set-Content -NoNewline -Path Sample.cs -Value @'
public class Sample
{
    public void Run()
    {
        // hygiene:retain modernization.var-style -- The inferred framework type is required at this interop boundary.
        var retained = System.DateTime.Now;
        var reviewed = System.DateTime.UtcNow;
    }
}
'@
        git add .editorconfig Sample.cs; git commit -m 'source-owned exception fixture' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean source-exception fixture.' }
        $review = dotnet hygiene check --review-all --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0) { throw 'Installed review-all check failed on the exception fixture.' }
        $varReview = $review.reviewBatches | Where-Object ruleId -eq 'modernization.var-style'
        if (-not $varReview -or $varReview.items[0].summary -notmatch 'source-owned retention exception') { throw 'A valid source-owned exception did not exclude only its candidate from the normal review sample.' }
        if (Test-Path -LiteralPath '.hygiene/.state/modernization-decisions.json') { throw 'Installed tool created retired durable modernization verdict state.' }
        $dirtyBefore = Get-Content -LiteralPath Sample.cs -Raw
        Set-Content -NoNewline -Path Sample.cs -Value ($dirtyBefore + "`n// dirty input`n")
        $dirtySource = Get-Content -LiteralPath Sample.cs -Raw
        $dirtyMutation = dotnet hygiene format --output json 2>&1
        if ($LASTEXITCODE -ne 3 -or (Get-Content -LiteralPath Sample.cs -Raw) -cne $dirtySource) { throw 'A dirty mutating formatter did not stop without changing source.' }
        $readOnly = dotnet hygiene format --check --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or (Get-Content -LiteralPath Sample.cs -Raw) -cne $dirtySource) { throw 'Read-only formatter check mutated a dirty worktree.' }
        git checkout -- Sample.cs
        if ($LASTEXITCODE -ne 0) { throw 'Could not restore the isolated dirty consumer fixture.' }
    }
    finally { Pop-Location }

    Push-Location $migration
    try {
        git init -q -b main
        git config user.email hygiene-m0024@example.invalid
        git config user.name 'M0024 Migration Consumer'
        Set-Content -NoNewline -Path .gitignore -Value "**/bin/`n**/obj/`n**/.hygiene/.state/`n"
        New-Item -ItemType Directory -Force src, .hygiene | Out-Null
        Set-Content -NoNewline -Path src\Consumer.csproj -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
        Set-Content -NoNewline -Path src\Sample.cs -Value 'public class Sample { }'
        Set-Content -NoNewline -Path .hygiene\profile.json -Value "{`n  `"schemaVersion`": 1,`n  `"profile`": `"dotnet-11`",`n  `"version`": 2`n}"
        Set-Content -NoNewline -Path .hygiene\config.json -Value '{"schemaVersion":1,"disabledRules":["style.braces.required","style.usings.unused","readability.long-line.review"]}'
        Set-Content -NoNewline -Path .editorconfig -Value "root = true`n# consumer-owned text`n[*.md]`ntrim_trailing_whitespace = true`n`n<!-- hygiene profile:begin -->`n[*.cs]`ncsharp_prefer_braces = true`ndotnet_diagnostic.IDE0011.severity = error`n<!-- hygiene profile:end -->`n"
        dotnet restore .\src\Consumer.csproj --configfile $nugetConfig | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Profile migration consumer restore failed.' }
        git add .; git commit -m 'v2 migration input' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean v2 migration fixture.' }
        $migrationResult = dotnet hygiene update --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0) { throw 'Installed v2-to-v3 profile update failed.' }
        if ((Get-Content -Raw .hygiene\profile.json | ConvertFrom-Json).version -ne 3) { throw 'Installed profile migration did not write v3.' }
        if (@($migrationResult.migrationNotes | Where-Object { $_ -match 'style\.braces\.required|style\.usings\.unused' }).Count -ne 2 -or -not ($migrationResult.migrationNotes | Where-Object { $_ -match 'external baseline approval' })) { throw 'Installed profile migration failed to report removed mechanical toggles and approval requirement.' }
        $migratedConfig = Get-Content -Raw .hygiene\config.json
        if ($migratedConfig -notmatch 'readability.long-line.review' -or $migratedConfig -match 'style\.braces\.required|style\.usings\.unused') { throw 'Profile migration did not preserve semantic toggles or remove retired mechanical toggles.' }
        $migratedEditor = Get-Content -Raw .editorconfig
        if ($migratedEditor -match '<!-- hygiene profile:' -or $migratedEditor -notmatch '(?m)^# hygiene profile:begin' -or $migratedEditor -notmatch 'consumer-owned text') { throw 'Profile migration did not replace malformed legacy boundaries and preserve unrelated content.' }
    }
    finally { Pop-Location }

    $version = (& dotnet --version).Trim()
    $gitVersion = (& git --version).Trim()
    $provenancePath = Join-Path $PSScriptRoot '..\.execution\validation\consumer-provenance.json'
    New-Item -ItemType Directory -Force (Split-Path -Parent $provenancePath) | Out-Null
    [pscustomobject]@{ schemaVersion = 2; milestone = 'M0024'; package = $package.Name; packageSha256 = $packageSha256; installedCommand = 'dotnet-hygiene'; installedVersion = '0.11.0'; sdk = $version; git = $gitVersion; scenarios = @('bootstrap', 'manual-format-check-and-mutation', 'manual-normalize-check-and-mutation', 'source-owned-exception', 'dirty-checkpoint-rejection', 'v2-to-v3-migration') } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $provenancePath -Encoding utf8
    Write-Output "Installed-consumer M0024 evidence passed; package SHA-256 $packageSha256."
}
finally {
    $env:PATH = $oldPath
    if ($null -eq $oldPackages) { Remove-Item Env:\NUGET_PACKAGES -ErrorAction SilentlyContinue } else { $env:NUGET_PACKAGES = $oldPackages }
    if ($null -eq $oldSkipUpdate) { Remove-Item Env:\DOTNET_HYGIENE_SKIP_UPDATE -ErrorAction SilentlyContinue } else { $env:DOTNET_HYGIENE_SKIP_UPDATE = $oldSkipUpdate }
    $resolvedTier = [IO.Path]::GetFullPath($tierRoot)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolvedTemp.EndsWith([IO.Path]::DirectorySeparatorChar)) { $resolvedTemp += [IO.Path]::DirectorySeparatorChar }
    if ($resolvedTier.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -and (Split-Path -Leaf $resolvedTier) -like 'hygiene-m0024-consumer-*' -and (Test-Path -LiteralPath $resolvedTier)) {
        Remove-Item -LiteralPath $resolvedTier -Recurse -Force
    }
}
