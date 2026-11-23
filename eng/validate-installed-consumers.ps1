$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$package = Get-Item .\artifacts\packages\DotNetAiCodeHygiene.Tool.0.12.0.nupkg
$packageSha256 = (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$tierRoot = Join-Path ([IO.Path]::GetTempPath()) ('workbench-m0026-consumer-' + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $tierRoot 'feed'
$toolPath = Join-Path $tierRoot 'tools'
$autoToolPath = Join-Path $tierRoot 'autoformat-tools'
$consumer = Join-Path $tierRoot 'consumer'
$migration = Join-Path $tierRoot 'migration'
$relationshipConsumer = Join-Path $tierRoot 'relationship-consumer'
$cache = Join-Path $tierRoot 'nuget-cache'
$report = Join-Path $tierRoot 'audit'
New-Item -ItemType Directory -Force $feed, $toolPath, $autoToolPath, $consumer, $migration, $relationshipConsumer, $cache | Out-Null
Copy-Item -LiteralPath $package.FullName -Destination $feed
if ((Get-FileHash -LiteralPath (Join-Path $feed $package.Name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $packageSha256) { throw 'The isolated feed package differs from the package just packed.' }
$nugetConfig = Join-Path $tierRoot 'NuGet.config'
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear /><add key="CurrentRun" value="$feed" /><add key="NuGetOfficial" value="https://api.nuget.org/v3/index.json" /></packageSources><packageSourceMapping><packageSource key="CurrentRun"><package pattern="DotNetAiCodeHygiene.Tool" /><package pattern="DotNetAiWorkbench.Autoformat.Tool" /></packageSource><packageSource key="NuGetOfficial"><package pattern="*" /></packageSource></packageSourceMapping></configuration>
"@ | Set-Content -LiteralPath $nugetConfig -Encoding utf8
$oldPath = $env:PATH
$oldPackages = $env:NUGET_PACKAGES
$oldSkipUpdate = $env:DOTNET_HYGIENE_SKIP_UPDATE
$env:PATH = $autoToolPath + [IO.Path]::PathSeparator + $toolPath + [IO.Path]::PathSeparator + $env:PATH
$env:NUGET_PACKAGES = $cache
$env:DOTNET_HYGIENE_SKIP_UPDATE = '1'
try {
    dotnet tool install DotNetAiCodeHygiene.Tool --tool-path $toolPath --configfile $nugetConfig --version 0.12.0
    if ($LASTEXITCODE -ne 0) { throw "Isolated exact-package install failed ($LASTEXITCODE)." }
    $installed = Join-Path $toolPath 'dotnet-hygiene.exe'
    if (-not (Test-Path -LiteralPath $installed)) { throw 'The installed dotnet-hygiene apphost is missing.' }
    $installedShimSha256 = (Get-FileHash -LiteralPath $installed -Algorithm SHA256).Hash.ToLowerInvariant()
    $installedVersion = dotnet hygiene --version
    if ($LASTEXITCODE -ne 0 -or ($installedVersion -join '').Trim() -ne '0.12.0') { throw 'The installed command did not report package version 0.12.0.' }

    $autoPackage = Get-Item .\artifacts\packages\DotNetAiWorkbench.Autoformat.Tool.0.1.0.nupkg
    $autoPackageSha256 = (Get-FileHash -LiteralPath $autoPackage.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    Copy-Item -LiteralPath $autoPackage.FullName -Destination $feed
    if ((Get-FileHash -LiteralPath (Join-Path $feed $autoPackage.Name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $autoPackageSha256) { throw 'The isolated Autoformat feed package differs from the current package.' }
    dotnet tool install DotNetAiWorkbench.Autoformat.Tool --tool-path $autoToolPath --configfile $nugetConfig --version 0.1.0
    if ($LASTEXITCODE -ne 0) { throw "Isolated exact Autoformat install failed ($LASTEXITCODE)." }
    $autoShim = Join-Path $autoToolPath 'dotnet-autoformat.exe'
    if (-not (Test-Path -LiteralPath $autoShim)) { throw 'The independent dotnet-autoformat apphost is missing.' }
    $autoShimSha256 = (Get-FileHash -LiteralPath $autoShim -Algorithm SHA256).Hash.ToLowerInvariant()

    Push-Location $consumer
    try {
        git init -q -b main
        git config user.email hygiene-m0025@example.invalid
        git config user.name 'M0025 Installed Consumer'
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
        if ($editor -notmatch '(?m)^# hygiene profile:begin$' -or $editor -match '<!-- hygiene profile:' -or $editor -notmatch 'dotnet_diagnostic.IDE0090.severity = warning' -or $editor -notmatch 'csharp_style_implicit_object_creation_when_type_is_apparent = true' -or $editor -match 'dotnet_style_object_creation_when_type_is_apparent' -or $editor -notmatch 'csharp_preferred_modifier_order = public,private,protected,internal,file,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,required,volatile,async' -or $editor -match 'dotnet_diagnostic.IDE0071.severity') { throw 'Installed bootstrap did not generate the corrected v3 EditorConfig profile and modifier order.' }
        if (-not (Test-Path -LiteralPath $bootstrap.reportPath)) { throw 'Installed bootstrap omitted its source-bound baseline report.' }
        git add -A
        git commit -m 'accept reviewed profile artifacts' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not establish the clean profile checkpoint.' }
        dotnet hygiene baseline accept --approver 'Isolated M0025 fixture reviewer' --report-sha256 $bootstrap.reportSha256 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'The exact installed bootstrap report could not be accepted in the isolated fixture.' }

        Set-Content -NoNewline -Path Sample.cs -Value 'using System; public class Sample{public object Create(){return new object();}}'
        git add Sample.cs; git commit -m 'manual format input' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean manual format input.' }
        $formatBefore = Get-Content -LiteralPath Sample.cs -Raw
        $formatResult = dotnet hygiene format --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $formatResult.targetCount -ne 1 -or $formatResult.changedPaths -notcontains 'Sample.cs' -or (Get-Content -LiteralPath Sample.cs -Raw) -ceq $formatBefore) { throw 'Installed Hygiene format did not mutate and report the selected source.' }
        git add Sample.cs; git commit -m 'formatted sample' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean formatted source.' }

        Set-Content -NoNewline -Path Sample.cs -Value 'public class Sample{public object Create = new object();}'
        git add Sample.cs; git commit -m 'manual normalize input' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean manual normalize input.' }
        dotnet hygiene normalize | Out-Null
        if ($LASTEXITCODE -ne 0 -or (Get-Content -Raw Sample.cs) -notmatch 'Create = new\(\);') { throw 'Installed SDK style normalization did not apply IDE0090.' }
        git add Sample.cs; git commit -m 'normalized sample' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean normalized source.' }

        Set-Content -NoNewline -Path Sample.cs -Value @'
public class Sample
{
#pragma warning disable IDE0090 // This explicit construction is required by the serializer contract.
    public object Explicit = new object();
#pragma warning restore IDE0090
    public object Ordinary = new object();
    public int Count = default(int);
}
'@
        git add Sample.cs; git commit -m 'native suppression fixture' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean native suppression fixture.' }
        $nativeCheck = dotnet hygiene check --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or @($nativeCheck.findings | Where-Object { $_.ruleId -eq 'modernization.source-exception' }).Count -ne 0) { throw 'The valid installed IDE0090 suppression was reported stale or malformed.' }
        dotnet hygiene normalize | Out-Null
        $nativeNormalized = Get-Content -LiteralPath Sample.cs -Raw
        if ($LASTEXITCODE -ne 0 -or $nativeNormalized -notmatch 'Explicit = new object\(\)' -or $nativeNormalized -notmatch 'Ordinary = new\(\)' -or $nativeNormalized -notmatch 'Count = default;') { throw 'The IDE0090 suppression affected unrelated fixes or allowed its intended diagnostic through.' }
        git add Sample.cs; git commit -m 'normalized native suppression fixture' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit normalized native suppression fixture.' }
        Set-Content -NoNewline -Path Sample.cs -Value "#pragma warning disable IDE0090 // This explicit construction is required by the serializer contract.`npublic class Sample { public object Value = new(); }`n#pragma warning restore IDE0090`n"
        $ineffective = dotnet hygiene check --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or -not ($ineffective.findings | Where-Object { $_.ruleId -eq 'modernization.source-exception' -and $_.message -match 'Ineffective or stale' })) { throw 'The installed tool did not report an ineffective IDE0090 suppression.' }
        git checkout -- Sample.cs
        if ($LASTEXITCODE -ne 0) { throw 'Could not restore the clean installed suppression fixture.' }

        Set-Content -NoNewline -Path .editorconfig -Value "root = true`n[*.cs]`ncsharp_style_var_for_built_in_types = false:warning`ncsharp_style_var_when_type_is_apparent = false:warning`ncsharp_style_var_elsewhere = false:warning`n[Exact.cs]`ncsharp_style_var_for_built_in_types = true:warning`ncsharp_style_var_when_type_is_apparent = false:warning`n"
        Set-Content -NoNewline -Path Sample.cs -Value @'
public class Sample
{
    public void Run()
    {
        // hygiene:retain modernization.var-style -- The inferred framework type is required at this interop boundary.
        var retained = System.DateTime.Now; var reviewed = System.DateTime.UtcNow;
    }
}
'@
        Set-Content -NoNewline -Path Exact.cs -Value @'
#pragma warning disable IDE0007 // This explicit type is required at the interop boundary.
public class Exact { public void Run() { var apparent = new Widget(); int retained = 1; } }
#pragma warning restore IDE0007
public class Widget { }
'@
        Set-Content -NoNewline -Path Closing.cs -Value @'
public class Closing
{
    public void Run()
    {
        var earlier = 42;

        // hygiene:retain modernization.var-style -- Explicit typing is required by an interoperability constraint.
    }
}
'@
        git add .editorconfig Sample.cs Exact.cs Closing.cs; git commit -m 'source-owned exception fixture' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit clean source-exception fixture.' }
        $review = dotnet hygiene check --review-all --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0) { throw 'Installed review-all check failed on the exception fixture.' }
        $varReview = $review.reviewBatches | Where-Object ruleId -eq 'modernization.var-style'
        if (-not $varReview -or -not ($varReview.items | Where-Object { $_.path -eq 'Sample.cs' -and $_.summary -match 'source-owned retention exception' })) { throw 'A valid source-owned exception did not exclude only its candidate from the normal review sample.' }
        $reviewOccurrences = @($varReview.items | ForEach-Object { $_.occurrences })
        if (-not ($reviewOccurrences | Where-Object { $_.path -eq 'Sample.cs' -and $_.originalSyntax -match 'reviewed =' }) -or ($reviewOccurrences | Where-Object { $_.path -eq 'Sample.cs' -and $_.originalSyntax -match 'retained =' })) { throw 'Installed hygiene retention did not attach to only the first same-line syntax subject.' }
        if (-not ($reviewOccurrences | Where-Object { $_.path -eq 'Exact.cs' -and $_.diagnosticId -eq 'IDE0008' }) -or ($reviewOccurrences | Where-Object { $_.path -eq 'Exact.cs' -and $_.diagnosticId -eq 'IDE0007' })) { throw 'Installed native pragma did not suppress only its exact modernization diagnostic identity.' }
        if (-not ($reviewOccurrences | Where-Object { $_.path -eq 'Closing.cs' -and $_.originalSyntax -match 'earlier = 42' })) { throw 'Installed review omitted the earlier candidate when a retention comment preceded the method closing brace.' }
        if (-not ($review.findings | Where-Object { $_.ruleId -eq 'modernization.source-exception' -and $_.path -eq 'Closing.cs' -and $_.message -match 'Malformed' })) { throw 'Installed hygiene check did not report the closing-brace annotation as unattached.' }
        if (Test-Path -LiteralPath '.hygiene/.state/modernization-decisions.json') { throw 'Installed tool created retired durable modernization verdict state.' }
        $dirtyBefore = Get-Content -LiteralPath Sample.cs -Raw
        Set-Content -NoNewline -Path Sample.cs -Value ($dirtyBefore + "`npublic object Dirty(){return new object();}`n")
        $dirtySource = Get-Content -LiteralPath Sample.cs -Raw
        $beforeRemovedCheck = (Get-FileHash -LiteralPath Sample.cs -Algorithm SHA256).Hash
        $removedCheck = dotnet hygiene format --check --output json 2>&1 | Out-String
        if ($LASTEXITCODE -eq 0 -or (Get-FileHash -LiteralPath Sample.cs -Algorithm SHA256).Hash -ne $beforeRemovedCheck) { throw 'Removed Hygiene formatter --check was accepted or changed source.' }
        $dirtyMutation = dotnet hygiene format --output json 2>&1
        if ($LASTEXITCODE -ne 3 -or (Get-Content -LiteralPath Sample.cs -Raw) -cne $dirtySource) { throw 'A dirty mutating formatter did not stop without changing source.' }
        git checkout -- Sample.cs
        if ($LASTEXITCODE -ne 0) { throw 'Could not restore the isolated dirty consumer fixture.' }
    }
    finally { Pop-Location }

    Push-Location $migration
    try {
        git init -q -b main
        git config user.email hygiene-m0025@example.invalid
        git config user.name 'M0025 Migration Consumer'
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
        $editorBeforeAutoformatAdoption = Get-Content -LiteralPath .editorconfig -Raw
        $adoption = dotnet autoformat init --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $adoption.status -ne 'ready' -or -not $adoption.adoptedExistingHygienePolicy) { throw 'Autoformat did not adopt the existing hygiene-managed mechanical policy.' }
        $migratedEditor = Get-Content -Raw .editorconfig
        if ($migratedEditor -match '<!-- hygiene profile:' -or $migratedEditor -notmatch '(?m)^# hygiene profile:begin' -or $migratedEditor -notmatch 'consumer-owned text' -or $migratedEditor -notmatch '(?m)^# autoformat profile:begin' -or $migratedEditor -notmatch 'dotnet_diagnostic.IDE0011.severity = error') { throw 'Autoformat adoption did not transfer mechanical ownership while preserving hygiene review settings and unrelated content.' }
        $hygieneSection = $migratedEditor.Substring($migratedEditor.IndexOf('# hygiene profile:begin'), $migratedEditor.IndexOf('# hygiene profile:end') - $migratedEditor.IndexOf('# hygiene profile:begin'))
        if ($hygieneSection.Contains('dotnet_diagnostic.IDE0011.severity')) { throw 'The transitional hygiene block still owns an overlapping mechanical option.' }
    }
    finally { Pop-Location }

    Push-Location $relationshipConsumer
    try {
        git init -q -b main
        git config user.email hygiene-m0025@example.invalid
        git config user.name 'M0025 Relationship Consumer'
        Set-Content -NoNewline -Path .gitignore -Value "**/bin/`n**/obj/`n**/.hygiene/.state/`n"
        New-Item -ItemType Directory -Force Provider, Consumer | Out-Null
        Set-Content -NoNewline -Path Provider\Provider.csproj -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework><AssemblyName>ProviderLib</AssemblyName></PropertyGroup></Project>'
        Set-Content -NoNewline -Path Provider\Provider.cs -Value 'namespace ProviderLib; public sealed class Provider { public string GetValue() => "v1"; }'
        Set-Content -NoNewline -Path Consumer\Consumer.csproj -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><ItemGroup><ProjectReference Include="..\Provider\Provider.csproj" /></ItemGroup></Project>'
        Set-Content -NoNewline -Path Consumer\Service.cs -Value 'using ProviderLib; public sealed class ConsumerService { public string Read() => new Provider().GetValue(); }'
        dotnet restore .\Consumer\Consumer.csproj --configfile $nugetConfig | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'M0025 relationship consumer restore failed.' }
        git add -A; git commit -m 'relationship consumer baseline' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit M0025 relationship baseline.' }
        $bootstrap = dotnet hygiene bootstrap --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $bootstrap.status -ne 'approval-required') { throw 'Installed M0025 bootstrap did not create an approval-required baseline.' }
        git add -A; git commit -m 'reviewed M0025 profile artifacts' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit M0025 reviewed profile artifacts.' }
        dotnet hygiene baseline accept --approver 'Isolated M0025 fixture reviewer' --report-sha256 $bootstrap.reportSha256 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Installed M0025 baseline acceptance failed.' }

        Set-Content -NoNewline -Path Provider\Provider.cs -Value 'namespace ProviderLib; public sealed class Provider { public string GetDisplayValue() => "v2"; }'
        Set-Content -NoNewline -Path Consumer\Service.cs -Value 'using ProviderLib; public sealed class ConsumerService { public string Read() => new Provider().GetDisplayValue(); }'
        git add Provider\Provider.cs Consumer\Service.cs; git commit -m 'change provider contract and consumer' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit M0025 cross-project change.' }
        $base = (git rev-parse HEAD^).Trim()
        $review = dotnet hygiene check --base $base --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0) { throw 'Installed dotnet hygiene check failed on the M0025 relationship change.' }
        $boundary = $review.reviewBatches | Where-Object ruleId -eq 'architecture.boundary.ownership'
        $related = @($boundary.items | Where-Object { $_.relatedAnchors.Count -gt 0 })
        if (-not $boundary -or $boundary.sampleCount -lt 1 -or -not $related) { throw 'Installed consumer did not report selected cross-project boundary review work.' }
        $relatedItem = $related[0]
        if ($relatedItem.evidence.Count -lt 2 -or $relatedItem.contextStatus -ne 'available') { throw "Installed relationship review did not include bounded evidence from both projects (status=$($relatedItem.contextStatus), excerpts=$($relatedItem.evidence.Count), missing=$($relatedItem.missingEvidence -join '; '), relatedPaths=$((@($relatedItem.relatedAnchors | ForEach-Object path) -join ', ')))." }
        if (($review.reviewBatches | Where-Object { $_.ruleId -in @('architecture.abstraction.necessity','architecture.indirection.value','architecture.responsibility.cohesion','architecture.boundary.ownership','architecture.authority.uniqueness','architecture.lifecycle.binding','architecture.mechanism.explicitness','behavior.error.contract','behavior.validation.boundary','behavior.state.consistency','behavior.resource.ownership','tests.assertion.effectiveness','tests.change-contract.coverage','tests.isolation.coupling','docs.summary.quality.review','docs.summary.language.german.review','comments.intent.review') } | ForEach-Object sampleCount | Measure-Object -Sum).Sum -gt 10) { throw 'Installed consumer exceeded the shared ten-obligation portfolio budget.' }
        dotnet hygiene review accept $boundary.handle $relatedItem.id | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Installed consumer could not accept the selected relationship obligation.' }
        $afterAcceptance = dotnet hygiene check --base $base --output json | ConvertFrom-Json
        $afterBoundary = $afterAcceptance.reviewBatches | Where-Object ruleId -eq 'architecture.boundary.ownership'
        if ($LASTEXITCODE -ne 0 -or @($afterBoundary.items | Where-Object id -eq $relatedItem.id).Count -ne 0) { throw 'Same-gate accepted relationship work was not suppressed without a replacement obligation.' }

        $protectedPaths = @('Provider/Provider.cs','Consumer/Service.cs','.git/index','.hygiene/profile.json','.hygiene/.state/latest-run.json','.hygiene/.state/gate-reviews.json')
        $protectedBefore = @{}
        foreach ($path in $protectedPaths) { if (Test-Path -LiteralPath $path) { $protectedBefore[$path] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash } }
        $audit = Join-Path $tierRoot 'm0025-audit'
        $auditStart = dotnet hygiene --yolo-all --attest-explicit-task --report-dir $audit --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $auditStart.status -ne 'collecting') { throw 'Installed exhaustive audit did not start after explicit task attestation.' }
        $auditRun = $auditStart.auditDirectory
        $auditWork = dotnet hygiene audit next --run $auditRun --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $null -eq $auditWork) { throw 'Installed exhaustive audit did not produce bounded next work.' }
        foreach ($path in $protectedPaths) { if ($protectedBefore.ContainsKey($path) -and (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $protectedBefore[$path]) { throw "Exhaustive audit modified protected file '$path'." } }
    }
    finally { Pop-Location }

    $legacyConsumerPath = $env:PATH
    $env:PATH = $autoToolPath + [IO.Path]::PathSeparator + $oldPath
    if ((dotnet autoformat --version | Out-String).Trim() -ne '0.1.0') { throw 'The installed Autoformat command did not report version 0.1.0.' }
    $autoHelp = dotnet autoformat --help | Out-String
    if ($LASTEXITCODE -ne 0 -or $autoHelp -notmatch 'format' -or $autoHelp -notmatch 'normalize' -or $autoHelp -notmatch 'init') { throw 'Installed Autoformat help did not describe its supported command surface.' }
    $autoConsumer = Join-Path $tierRoot 'autoformat-consumer'
    New-Item -ItemType Directory -Force $autoConsumer | Out-Null
    Push-Location $autoConsumer
    try {
        git init -q -b main; git config user.email autoformat-m0026@example.invalid; git config user.name 'M0026 Autoformat Consumer'
        Set-Content -NoNewline -Path .gitignore "**/bin/`n**/obj/`n"
        New-Item -ItemType Directory -Force Second | Out-Null
        Set-Content -NoNewline -Path Consumer.csproj '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><ItemGroup><Compile Remove="Second/**/*.cs" /></ItemGroup></Project>'
        Set-Content -NoNewline -Path Second\Second.csproj '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
        Set-Content -NoNewline -Path Second\Second.cs 'public class Second{public object Value=new object();}'
        Set-Content -NoNewline -Path Sample.cs 'using System; public class Sample{public object Value=new object();}'
        Set-Content -NoNewline -Path FormatOnly.cs 'public class FormatOnly{public object Value=new object();}'
        Set-Content -NoNewline -Path Consumer.slnx '<Solution><Project Path="Consumer.csproj" /><Project Path="Second/Second.csproj" /></Solution>'
        dotnet restore .\Consumer.slnx --configfile $nugetConfig | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Autoformat consumer restore failed.' }
        git add -A; git commit -m 'consumer source baseline' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not commit Autoformat consumer source baseline.' }
        $beforePolicyRun = Get-Content Sample.cs -Raw
        $missingPolicy = dotnet autoformat normalize --output json 2>&1 | Out-String
        if ($LASTEXITCODE -ne 2 -or $missingPolicy -notmatch 'dotnet autoformat init' -or (Get-Content Sample.cs -Raw) -cne $beforePolicyRun) { throw 'Autoformat source run did not fail safely with an actionable init instruction.' }
        $init = dotnet autoformat init --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $init.status -ne 'initialized') { throw 'Independent Autoformat init failed.' }
        $policy = Get-Content .editorconfig -Raw
        if ($policy -notmatch '# autoformat profile:begin' -or (Test-Path .hygiene)) { throw 'Autoformat init did not create only the independent mechanical policy.' }
        $policyHash = (Get-FileHash -LiteralPath .editorconfig -Algorithm SHA256).Hash
        $repeatInit = dotnet autoformat init --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $repeatInit.status -ne 'ready' -or (Get-FileHash -LiteralPath .editorconfig -Algorithm SHA256).Hash -ne $policyHash) { throw 'Autoformat init was not idempotent.' }
        git add .editorconfig; git commit -m 'initialize Autoformat policy' | Out-Null

        $formatResult = dotnet autoformat format FormatOnly.cs --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $formatResult.selectedPaths.Count -ne 1 -or $formatResult.selectedPaths[0] -ne 'FormatOnly.cs' -or $formatResult.changedPaths -notcontains 'FormatOnly.cs') { throw 'Installed whitespace-only format did not mutate and report the explicit file.' }
        if ($LASTEXITCODE -ne 0 -or (Get-Content FormatOnly.cs -Raw) -notmatch 'Value = new object\(\)') { throw 'Whitespace-only format unexpectedly applied SDK style fixes or failed to format.' }
        git add FormatOnly.cs; git commit -m 'format whitespace fixture' | Out-Null

        Set-Content -NoNewline -Path Sample.cs 'using System; public class Sample{public object Value=new object();}'
        git add Sample.cs; git commit -m 'normalize fixture' | Out-Null
        $pathResult = dotnet autoformat normalize Sample.cs --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $pathResult.selectedPaths.Count -ne 1 -or $pathResult.selectedPaths[0] -ne 'Sample.cs' -or $pathResult.changedPaths -notcontains 'Sample.cs') { throw 'Explicit path selection did not mutate only its target.' }
        if ((Get-Content Second\Second.cs -Raw) -notmatch 'Value=new object\(\)') { throw 'Explicit normalization changed an unselected project.' }
        git add Sample.cs; git commit -m 'explicit normalized fixture' | Out-Null
        dotnet autoformat | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Installed Autoformat default normalize mode failed.' }
        $normalized = Get-Content Sample.cs -Raw
        if ($normalized -notmatch 'Value = new\(\);') { throw 'Installed Autoformat explicit normalization did not apply the approved SDK normalization.' }
        if ((Get-Content Second\Second.cs -Raw) -notmatch 'Value = new\(\);') { throw 'Unscoped Autoformat did not normalize the second SDK project.' }
        git add Sample.cs FormatOnly.cs Second\Second.cs; git commit -m 'normalized fixture' | Out-Null
        dotnet autoformat normalize | Out-Null
        if ($LASTEXITCODE -ne 0 -or (git status --porcelain).Length -ne 0) { throw 'Installed Autoformat normalization was not idempotent.' }

        Set-Content -NoNewline -Path Staged.cs 'public class Staged { public int Value = default(int); }'
        git add Staged.cs
        Set-Content -NoNewline -Path Sample.cs 'using System; public class Sample{public object Value=new object();}'
        Set-Content -NoNewline -Path Untracked.cs 'public class Untracked { public int Value = default(int); }'
        $dirtyFiles = @('Sample.cs','Staged.cs','Untracked.cs')
        $beforeChanged = @{}; foreach ($file in $dirtyFiles) { $beforeChanged[$file] = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash }
        $changedRun = dotnet autoformat --changed --output json 2>&1 | Out-String
        if ($LASTEXITCODE -eq 0) { throw 'Removed Autoformat --changed was accepted.' }
        foreach ($file in $dirtyFiles) { if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $beforeChanged[$file]) { throw "Rejected Autoformat --changed modified '$file'." } }
        $dirtyMutation = dotnet autoformat normalize --output json 2>&1 | Out-String
        if ($LASTEXITCODE -ne 2 -or $dirtyMutation -notmatch 'clean Git worktree') { throw 'Autoformat mutation did not reject the dirty checkpoint.' }
        foreach ($file in $dirtyFiles) { if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $beforeChanged[$file]) { throw "Rejected mutation changed '$file'." } }
        git checkout -- Sample.cs; git reset -- Staged.cs | Out-Null; Remove-Item -LiteralPath Staged.cs,Untracked.cs

        $nested = Join-Path $autoConsumer 'nested'; New-Item -ItemType Directory -Force $nested | Out-Null
        Set-Content -NoNewline -Path (Join-Path $nested '.editorconfig') "[*.cs]`ncsharp_style_implicit_object_creation_when_type_is_apparent = false`n"
        $nestedBefore = Get-Content Sample.cs -Raw
        git add nested/.editorconfig; git commit -m 'nested policy override' | Out-Null
        $nestedConflict = dotnet autoformat format --output json 2>&1 | Out-String
        if ($LASTEXITCODE -ne 2 -or $nestedConflict -notmatch 'Conflicting mechanical setting' -or (Get-Content Sample.cs -Raw) -cne $nestedBefore) { throw 'Nested mechanical policy conflict was not reported safely.' }
        Remove-Item -LiteralPath $nested -Recurse -Force

        Add-Content .editorconfig '# autoformat profile:begin'
        $malformedBefore = Get-Content .editorconfig -Raw
        $malformed = dotnet autoformat init --output json 2>&1 | Out-String
        if ($LASTEXITCODE -ne 2 -or $malformed -notmatch 'Malformed or incomplete|Duplicate managed policy markers' -or (Get-Content .editorconfig -Raw) -cne $malformedBefore) { throw 'Malformed policy markers were not rejected without overwriting configuration.' }
        git checkout -- .editorconfig
    }
    finally { Pop-Location; $env:PATH = $legacyConsumerPath }
    $version = (& dotnet --version).Trim()
    $gitVersion = (& git --version).Trim()
    $provenancePath = Join-Path $PSScriptRoot '..\.execution\validation\consumer-provenance.json'
    New-Item -ItemType Directory -Force (Split-Path -Parent $provenancePath) | Out-Null
    [pscustomobject]@{ schemaVersion = 4; milestone = 'M0026'; package = $package.Name; packageSha256 = $packageSha256; installedShimSha256 = $installedShimSha256; autoformatPackage = $autoPackage.Name; autoformatPackageSha256 = $autoPackageSha256; autoformatShimSha256 = $autoShimSha256; installedCommand = 'dotnet-autoformat'; installedVersion = '0.1.0'; legacyCommand = 'dotnet-hygiene'; legacyVersion = '0.12.0'; sdk = $version; git = $gitVersion; scenarios = @('autoformat-independent-installed-consumer-init-mutation-idempotence','autoformat-help-explicit-format-default-and-path-selection','autoformat-removed-flags-and-dirty-rejection','autoformat-fresh-init-idempotence-nested-conflict-and-malformed-marker-safety','autoformat-adoption-of-hygiene-managed-policy','bootstrap-correct-ide0090-and-modern-modifier-profile', 'manual-format-mutation', 'manual-normalize-mutation', 'native-suppression-check-mutation-and-stale-detection', 'source-owned-exception-exact-subject-and-diagnostic-identity', 'unattached-closing-brace-exception', 'semantic-check-changed-read-only', 'dirty-checkpoint-rejection', 'v2-to-v3-migration', 'cross-project-related-evidence', 'shared-semantic-budget', 'same-gate-acceptance-without-refill', 'attested-exhaustive-read-only-audit') } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $provenancePath -Encoding utf8
    Write-Output "Installed-consumer M0026 dual-tool evidence passed; package SHA-256 $packageSha256."
}
finally {
    $env:PATH = $oldPath
    if ($null -eq $oldPackages) { Remove-Item Env:\NUGET_PACKAGES -ErrorAction SilentlyContinue } else { $env:NUGET_PACKAGES = $oldPackages }
    if ($null -eq $oldSkipUpdate) { Remove-Item Env:\DOTNET_HYGIENE_SKIP_UPDATE -ErrorAction SilentlyContinue } else { $env:DOTNET_HYGIENE_SKIP_UPDATE = $oldSkipUpdate }
    $resolvedTier = [IO.Path]::GetFullPath($tierRoot)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolvedTemp.EndsWith([IO.Path]::DirectorySeparatorChar)) { $resolvedTemp += [IO.Path]::DirectorySeparatorChar }
    if ($resolvedTier.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -and (Split-Path -Leaf $resolvedTier) -like 'workbench-m0026-consumer-*' -and (Test-Path -LiteralPath $resolvedTier)) {
        Remove-Item -LiteralPath $resolvedTier -Recurse -Force
    }
}
