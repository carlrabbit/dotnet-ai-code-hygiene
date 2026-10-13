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
$package = Get-ChildItem .\artifacts\packages\DotNetAiCodeHygiene.Tool.0.9.0.nupkg
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
dotnet tool install DotNetAiCodeHygiene.Tool --tool-path $toolPath --configfile $nugetConfig --version 0.9.0
if ($LASTEXITCODE -ne 0) { throw "Tier-4 tool install failed with exit code $LASTEXITCODE." }
$hygiene = Join-Path $toolPath 'hygiene.exe'
if (-not (Test-Path -LiteralPath $hygiene)) { $hygiene = Join-Path $toolPath 'hygiene' }
if (-not (Test-Path -LiteralPath $hygiene)) { throw 'Tier-4 installed hygiene command was not created.' }
Push-Location $consumer
try {
    git init -q
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 fixture git init failed.' }
    git config user.email hygiene-tier4@example.invalid
    git config user.name 'Hygiene Tier 4'
    Set-Content -NoNewline -Path Consumer.csproj -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
    Set-Content -NoNewline -Path Sample.cs -Value 'public class Sample{private int value;public int Value(){return this.value;}}'
    & $hygiene --version
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene --version failed.' }
    $version = & $hygiene --version
    if ($LASTEXITCODE -ne 0 -or ($version -join '') -notmatch '0\.9\.0') { throw 'Installed hygiene version does not match 0.9.0.' }
    & $hygiene help --agent | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene help --agent failed.' }
    $bootstrap = & $hygiene bootstrap --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $bootstrap.findingCount -ne 0) { throw 'Installed bootstrap failed or left profile findings.' }
    & $hygiene rules disable style.braces.required | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed braces rule could not be disabled.' }
    $editorAfterDisable = Get-Content -Raw .editorconfig
    if ($editorAfterDisable -match '# hygiene rule: style\.braces\.required') { throw 'Disabling braces retained its hygiene-owned projection.' }
    & $hygiene rules enable style.braces.required | Out-Null
    if ($LASTEXITCODE -ne 0 -or (Get-Content -Raw .editorconfig) -notmatch '# hygiene rule: style\.braces\.required') { throw 'Enabling braces did not restore its projection.' }
    & $hygiene rules disable format.csharp.roslyn | Out-Null
    $disabledFormat = & $hygiene format --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $disabledFormat.changedCount -ne 0 -or $disabledFormat.selectedRuleIds.Count -ne 0) { throw 'Disabled format rule still participated in format.' }
    & $hygiene rules enable format.csharp.roslyn | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed format rule could not be enabled.' }
    $formatBefore = Get-Content -Raw Sample.cs
    $formatCheck = & $hygiene format --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $formatCheck.changedCount -lt 1 -or ($formatCheck.selectedRuleIds -join ',') -ne 'format.csharp.roslyn') { throw 'Installed hygiene format --check did not report pending changes and selected rule.' }
    if ((Get-Content -Raw Sample.cs) -cne $formatBefore) { throw 'Installed format --check modified source.' }
    & $hygiene format | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene format mutation failed.' }
    $formatted = Get-Content -Raw Sample.cs
    if ($formatted -ceq $formatBefore) { throw 'Installed format mutation did not change source.' }
    $formatClean = & $hygiene format --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $formatClean.changedCount -ne 0) { throw 'Installed format was not idempotent.' }
    $normalizeBefore = Get-Content -Raw Sample.cs
    & $hygiene rules disable style.qualification.this.unnecessary | Out-Null
    $thisDisabled = & $hygiene normalize --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or ($thisDisabled.selectedRuleIds -contains 'style.qualification.this.unnecessary')) { throw 'Disabled this qualification rule still participated in normalize.' }
    & $hygiene rules enable style.qualification.this.unnecessary | Out-Null
    $normalizeCheck = & $hygiene normalize --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $normalizeCheck.changedCount -lt 1) { throw 'Installed hygiene normalize --check did not report pending changes.' }
    if ((Get-Content -Raw Sample.cs) -cne $normalizeBefore) { throw 'Installed normalize --check modified source.' }
    & $hygiene normalize | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene normalize mutation failed.' }
    $normalized = Get-Content -Raw Sample.cs
    if ($normalized -ceq $normalizeBefore) { throw 'Installed normalize mutation did not change source.' }
    $normalizeClean = & $hygiene normalize --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $normalizeClean.changedCount -ne 0) { throw 'Installed normalize was not idempotent.' }
    $profilePath = Join-Path $consumer '.hygiene/profile.json'
    Set-Content -LiteralPath .editorconfig -Encoding utf8 -Value "root = true`n`n# consumer-owned text`n[*.md]`ntrim_trailing_whitespace = true`n`n<!-- hygiene profile:begin -->`n[*.cs]`ncsharp_prefer_braces = true`ndotnet_diagnostic.IDE0011.severity = error`ndotnet_style_require_accessibility_modifiers = always`ndotnet_diagnostic.IDE0040.severity = error`n<!-- hygiene profile:end -->"
    Set-Content -LiteralPath $profilePath -Encoding utf8 -Value "{`n  `"schemaVersion`": 1,`n  `"profile`": `"dotnet-11`",`n  `"version`": 1`n}"
    $update = & $hygiene update --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $update.findingCount -ne 0 -or $update.changedPaths.Count -eq 0) { throw 'Installed update did not migrate the v1 profile.' }
    if ((Get-Content -Raw $profilePath) -notmatch '"version": 2') { throw 'Installed profile update did not persist v2.' }
    $migratedEditor = Get-Content -Raw .editorconfig
    if ($migratedEditor -notmatch '# hygiene rule: style\.braces\.required' -or $migratedEditor -notmatch '# hygiene rule: style\.accessibility\.explicit') { throw 'Installed profile migration did not regenerate rule-owned EditorConfig contributions.' }
    if ($migratedEditor -notmatch '# consumer-owned text\r?\n\[\*\.md\]\r?\ntrim_trailing_whitespace = true') { throw 'Installed profile migration did not preserve user EditorConfig content.' }
    $updateAgain = & $hygiene update --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $updateAgain.findingCount -ne 0 -or $updateAgain.changedPaths.Count -ne 0) { throw 'Installed update was not idempotent after migration.' }
    $rules = & $hygiene rules --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or -not (($rules.rules | Where-Object id -eq 'profile.dotnet.analysis.required').configurable -eq $false)) { throw 'Installed mandatory profile rule is missing or configurable.' }
    if ($rules.rules.Count -ne 18 -or -not (($rules.rules | Where-Object id -eq 'format.csharp.roslyn').capabilities.format)) { throw 'Installed rule capabilities are incomplete.' }
    if (-not ($rules.rules | Where-Object id -eq 'docs.summary.quality.review')) { throw 'Installed rule catalog omitted summary quality review.' }
    if (-not ($rules.rules | Where-Object id -eq 'architecture.boringness.review')) { throw 'Installed rule catalog omitted BORINGness review.' }
    $newRuleIds = @('readability.type-responsibility.review', 'tests.quality.review', 'comments.intent.review')
    if (@($rules.rules | Where-Object { $_.id -in $newRuleIds }).Count -ne 3) { throw 'Installed rule catalog omitted an M0017 review rule.' }
    $visualRule = $rules.rules | Where-Object id -eq 'readability.control-flow.visual-block'
    if ($visualRule.version -ne 2 -or $visualRule.classification -ne 'review-candidate') { throw 'Installed visual-block rule did not expose its v2 candidate contract.' }
    $reviewSource = @('// Copyright 2026 Example. All rights reserved.', '// MIT License', '/// <summary>Sample API</summary>', 'public class Sample { public void Run() { int value = 1; if (value > 0) { System.Console.WriteLine(value); } } }') + @(0..31 | ForEach-Object { "/// <summary>A useful API description for sample $_.</summary>`npublic class Reviewed$_ { }" })
    Set-Content -NoNewline -Path Sample.cs -Value ($reviewSource -join "`n")
    & $hygiene format | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed formatter failed on the visual-block fixture.' }
    & $hygiene normalize | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed normalizer failed on the visual-block fixture.' }
    $visualCheck = & $hygiene check --review-all --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or -not ($visualCheck.findings | Where-Object { $_.ruleId -eq 'readability.control-flow.visual-block' -and $_.classification -eq 'review-candidate' })) { throw 'Installed visual-block candidate was not retained after format/normalize.' }
    git add Sample.cs
    git commit -m 'review fixture baseline' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 baseline commit failed.' }
    $baseCommit = (& git rev-parse HEAD).Trim()
    Add-Content -Path Sample.cs -Value @'

namespace Xunit { [System.AttributeUsage(System.AttributeTargets.Method)] public sealed class FactAttribute : System.Attribute { } }
/// <summary>An additional API description for the PR gate.</summary>
public class ReviewedAdditional
{
    // Keep this guard aligned with the request contract.
    public void Run()
    {
        var key = 1; // Keep the source statement and trailing review separate.
        // Avoid the guarded operation when the key is empty.
        if (key > 0) { System.Console.WriteLine(key); }
    }
    [Xunit.Fact]
    public void ChangedBehaviorHasRegressionTest() { System.Console.WriteLine("observable"); }
}
'@
    $bareCheck = & $hygiene check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or @($bareCheck.reviewBatches | Where-Object sampleCount -ne 0).Count -ne 0) { throw 'Bare installed check must schedule zero semantic review.' }
    $check = & $hygiene check --base $baseCommit --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene check failed.' }
    if (-not ($check.findings | Where-Object ruleId -eq 'docs.summary.required')) { throw 'Installed check did not report a missing API summary.' }
    if (-not ($check.findings | Where-Object ruleId -eq 'docs.text.sentence')) { throw 'Installed check did not report missing summary punctuation.' }
    $quality = $check.reviewBatches | Where-Object ruleId -eq 'docs.summary.quality.review'
    $german = $check.reviewBatches | Where-Object ruleId -eq 'docs.summary.language.german.review'
    if (-not $quality -or $quality.ruleVersion -ne 6 -or $quality.sampleCount -lt 1 -or $quality.sampleCount -gt 5 -or -not $german -or $german.ruleVersion -ne 4) { throw 'Installed PR-base check did not emit bounded independent v6 quality and v4 German review batches.' }
    if (-not ($check.reviewBatches | Where-Object ruleId -eq 'architecture.boringness.review')) { throw 'Installed check omitted BORINGness review.' }
    if (@($check.reviewBatches | Where-Object { $_.ruleId -in $newRuleIds }).Count -ne 3) { throw 'Installed PR-base check omitted an M0017 review batch.' }
    $typeReview = $check.reviewBatches | Where-Object ruleId -eq 'readability.type-responsibility.review'
    $testReview = $check.reviewBatches | Where-Object ruleId -eq 'tests.quality.review'
    $commentReview = $check.reviewBatches | Where-Object ruleId -eq 'comments.intent.review'
    if ($typeReview.sampleCount -lt 1 -or $testReview.sampleCount -lt 1 -or $commentReview.sampleCount -lt 1) { throw 'Installed M0017 rules did not select changed type, test, and comment subjects.' }
    if (($testReview.questions.Count -ne 3) -or $testReview.items[0].summary -ne '') { throw 'Installed test-quality review did not retain its fixed questions and empty non-applicable summary.' }
    $newAccepted = & $hygiene review accept $testReview.handle $testReview.items[0].id --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $newAccepted.acceptedCount -ne 1 -or $newAccepted.ruleId -ne 'tests.quality.review') { throw 'Installed test-quality review acceptance failed.' }
    $commentExpanded = & $hygiene review expand $commentReview.handle --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $commentExpanded.reviewBatch.ruleId -ne 'comments.intent.review' -or $commentExpanded.reviewBatch.items.Count -lt 4) { throw 'Installed comment-intent expansion failed or omitted comment groups.' }
    $commentHandoff = & $hygiene review handoff $commentReview.handle
    if ($LASTEXITCODE -ne 0 -or $commentHandoff -notmatch 'Review handoff written: ') { throw 'Installed comment-intent handoff failed.' }
    $commentRequestPath = $commentHandoff.Substring('Review handoff written: '.Length).Trim()
    $commentRequest = Get-Content -LiteralPath $commentRequestPath -Raw | ConvertFrom-Json
    $commentSource = $commentRequest.sources | Where-Object path -eq 'Sample.cs'
    if (-not $commentSource -or $commentSource.content -notmatch 'Copyright 2026' -or $commentSource.content -notmatch 'MIT License' -or $commentSource.content -notmatch 'trailing review separate' -or $commentSource.content -notmatch 'Avoid the guarded operation') { throw 'Installed comment-intent expansion did not preserve current source context or ordinary legal comments.' }
    $typeHandoff = & $hygiene review handoff $typeReview.handle
    if ($LASTEXITCODE -ne 0 -or $typeHandoff -notmatch 'Review handoff written: ') { throw 'Installed type-responsibility handoff failed.' }
    $typeRequestPath = $typeHandoff.Substring('Review handoff written: '.Length).Trim()
    $typeRequest = Get-Content -LiteralPath $typeRequestPath -Raw | ConvertFrom-Json
    if ($typeRequest.reviewerClass -ne 'planner' -or $typeRequest.rule.id -ne 'readability.type-responsibility.review') { throw 'Installed type-responsibility handoff contract is invalid.' }
    $internalFields = 'samplingTicket|stateEpoch|repositorySeed|hazard|threshold|evaluationFingerprint|aggregateEvidence'
    if (($check | ConvertTo-Json -Depth 32) -match $internalFields) { throw 'Installed check exposed internal sampling metadata.' }
    $firstQualityItem = $quality.items[0].id
    $accepted = & $hygiene review accept $quality.handle $firstQualityItem --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $accepted.acceptedCount -ne 1 -or $accepted.ruleId -ne 'docs.summary.quality.review') { throw 'Installed explicit review accept failed.' }
    $checkAgain = & $hygiene check --base $baseCommit --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Installed follow-up hygiene check failed.' }
    $qualityAgain = $checkAgain.reviewBatches | Where-Object ruleId -eq 'docs.summary.quality.review'
    $germanAgain = $checkAgain.reviewBatches | Where-Object ruleId -eq 'docs.summary.language.german.review'
    $boringness = $checkAgain.reviewBatches | Where-Object ruleId -eq 'architecture.boringness.review'
    $acceptedAll = & $hygiene review accept $qualityAgain.handle --all --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $acceptedAll.acceptedCount -ne $qualityAgain.sampleCount) { throw 'Installed review accept --all did not consume exactly the current normal sample.' }
    if (-not ($german.questions[0].text -match 'natural, comprehensible German')) { throw 'Installed German review rubric is incorrect.' }
    $expanded = & $hygiene review expand $qualityAgain.handle --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $expanded.reviewBatch.mode -ne 'expanded' -or $expanded.reviewBatch.ruleId -ne 'docs.summary.quality.review') { throw 'Installed summary review expansion failed or selected the wrong batch.' }
    $boringExpanded = & $hygiene review expand $boringness.handle --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $boringExpanded.reviewBatch.items.Count -lt 32) { throw 'Installed BORINGness expansion did not retain the complete type population.' }
    $handoff = & $hygiene review handoff $germanAgain.handle
    if ($LASTEXITCODE -ne 0 -or $handoff -notmatch 'Review handoff written: ') { throw 'Installed German review handoff failed.' }
    $plannerHandoff = & $hygiene review handoff $boringness.handle
    if ($LASTEXITCODE -ne 0 -or $plannerHandoff -notmatch 'Review handoff written: ') { throw 'Installed BORINGness planner handoff failed.' }
    $plannerPath = $plannerHandoff.Substring('Review handoff written: '.Length).Trim()
    $plannerRequest = Get-Content -LiteralPath $plannerPath -Raw | ConvertFrom-Json
    if ($plannerRequest.reviewerClass -ne 'planner' -or $plannerRequest.populationCount -lt 32 -or $plannerRequest.PSObject.Properties.Name -match 'result|ticket|hazard') { throw 'Installed BORINGness planner handoff contract is invalid.' }
    & $hygiene rules disable docs.summary.language.german.review | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed German rule could not be disabled.' }
    $qualityOnly = & $hygiene check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $qualityOnly.reviewBatches.Count -ne 5 -or $qualityOnly.reviewBatches.ruleId -contains 'docs.summary.language.german.review' -or -not ($qualityOnly.reviewBatches | Where-Object ruleId -eq 'docs.summary.quality.review') -or -not ($qualityOnly.reviewBatches | Where-Object ruleId -eq 'architecture.boringness.review') -or -not ($qualityOnly.reviewBatches | Where-Object ruleId -eq 'comments.intent.review')) { throw 'Disabling the installed German rule changed or removed other semantic-review rules.' }
}
finally {
    Pop-Location
    $env:NUGET_PACKAGES = $previousNugetPackages
    Remove-Item -LiteralPath $tierRoot -Recurse -Force
}

$selfCheck = & dotnet .\src\DotNetAiCodeHygiene.Cli\bin\Release\net11.0\DotNetAiCodeHygiene.Cli.dll check --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Repository self-host hygiene check failed.' }
$qualityBatch = $selfCheck.reviewBatches | Where-Object ruleId -eq 'docs.summary.quality.review'
$germanBatch = $selfCheck.reviewBatches | Where-Object ruleId -eq 'docs.summary.language.german.review'
if (-not $qualityBatch -or $germanBatch -or -not ($selfCheck.reviewBatches | Where-Object ruleId -eq 'architecture.boringness.review')) { throw 'Repository self-host check did not retain the German disable, quality review, and BORINGness rule behavior.' }
git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Repository diff whitespace check failed.' }
Write-Output 'Tier-4 exact installed consumer validation passed; repository M0006 self-host behavior passed.'
