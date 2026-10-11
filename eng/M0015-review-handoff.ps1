param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('prepare', 'verify', 'test-validator')]
    [string]$Action,
    [string]$SemanticModelRepositoryPath,
    [string]$StageRoot,
    [string]$PrivateMapPath,
    [string]$HandoffRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$packageRoot = Join-Path $repoRoot 'tools/M0015/review-handoff'
$frozen = Get-Content (Join-Path $repoRoot 'tools/M0015/FrozenGateEvidence.json') -Raw | ConvertFrom-Json
$sourceKeys = @{
    'hygiene-pr15' = [pscustomobject]@{ key='repository-a'; path=$repoRoot; head='4d05393b4501a0b4d701f6ba6e1d051e9bd35ec2'; name='repository-a-source.zip'; repo='carlrabbit/dotnet-ai-code-hygiene' }
    'semantic-model-source' = [pscustomobject]@{ key='repository-b'; path=$SemanticModelRepositoryPath; head='88b5ad5092e38e00257d4e327d6606afa2bf4952'; name='repository-b-source.zip'; repo='carlrabbit/dotnet-semantic-type-model' }
}

function Invoke-Git([string]$Root, [string[]]$Arguments, [string]$Description) {
    $output = & git -C $Root @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Description failed: $($output -join ' ')" }
    return $output
}

function Get-Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant() }
function Write-JsonFile([string]$Path, [object]$Value, [int]$Depth = 16) {
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth $Depth) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

function Test-Handoff([string]$Root) {
    $sumPath = Join-Path $Root 'SHA256SUMS.txt'
    if (-not (Test-Path -LiteralPath $sumPath -PathType Leaf)) { throw "Handoff checksum manifest is missing: $sumPath" }
    $entries = @{}
    foreach ($line in [IO.File]::ReadAllLines($sumPath)) {
        if ($line -notmatch '^([A-Fa-f0-9]{64})  (.+)$') { throw "Malformed handoff checksum line: $line" }
        $relative = $Matches[2].Replace('\', '/')
        if ($entries.ContainsKey($relative)) { throw "Duplicate checksum path: $relative" }
        $entries[$relative] = $Matches[1].ToUpperInvariant()
    }
    $actual = @(Get-ChildItem -LiteralPath $Root -File -Recurse | Where-Object { $_.FullName -ne $sumPath } | ForEach-Object { [IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\', '/') } | Sort-Object)
    $expected = @($entries.Keys | Sort-Object)
    if (($actual -join '|') -ne ($expected -join '|')) { throw 'Handoff file inventory differs from SHA256SUMS.txt (missing or unexpected files).' }
    foreach ($relative in $expected) {
        $path = Join-Path $Root ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
        if ((Get-Hash $path) -ne $entries[$relative]) { throw "Handoff checksum mismatch: $relative" }
    }
    $manifest = Get-Content (Join-Path $Root 'FROZEN-MANIFEST.json') -Raw | ConvertFrom-Json
    $candidates = Get-Content (Join-Path $Root 'BLINDED-CANDIDATES.json') -Raw | ConvertFrom-Json
    if ($candidates.items.Count -ne $manifest.assessmentCount) { throw 'Assessment count does not match the blinded candidate list.' }
    foreach ($source in $manifest.sources) {
        $archive = Join-Path $Root $source.archivePath
        if ((Get-Hash $archive) -ne $source.archiveSha256) { throw "Source archive checksum does not match frozen manifest: $($source.archivePath)" }
    }
    return [pscustomobject]@{ fileCount=$actual.Count; assessmentCount=$manifest.assessmentCount; sha256Manifest=(Get-Hash $sumPath) }
}

if ($Action -eq 'verify') {
    if ([string]::IsNullOrWhiteSpace($HandoffRoot)) { throw 'Pass -HandoffRoot for the staged or delivered handoff directory.' }
    $result = Test-Handoff (Resolve-Path $HandoffRoot).Path
    "Handoff verification passed: $($result.fileCount) files, $($result.assessmentCount) assessments, SHA256SUMS=$($result.sha256Manifest)"
    exit 0
}

if ($Action -eq 'test-validator') {
    $validator = Join-Path $packageRoot 'validate-submission.ps1'
    $results = Join-Path $repoRoot 'tools/M0015/fixtures/review-handoff/validator-test-results.json'
    & $validator -Action test -TestResultsPath $results
    exit 0
}

if ([string]::IsNullOrWhiteSpace($SemanticModelRepositoryPath)) { throw 'Pass -SemanticModelRepositoryPath for the frozen public Semantic Type Model clone.' }
if ([string]::IsNullOrWhiteSpace($StageRoot)) { $StageRoot = Join-Path $env:TEMP 'M0015-review-handoff-stage-a' }
if ([string]::IsNullOrWhiteSpace($PrivateMapPath)) { $PrivateMapPath = 'C:\src\m0015-evidence\stage-a-private\token-policy-map.json' }
$SemanticModelRepositoryPath = (Resolve-Path $SemanticModelRepositoryPath).Path
$StageRoot = [IO.Path]::GetFullPath($StageRoot)
$archiveRoot = Join-Path $StageRoot 'source-archives'
$stagedHandoff = Join-Path $StageRoot 'handoff'
New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null
New-Item -ItemType Directory -Path $stagedHandoff -Force | Out-Null
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

$sourceSpecs = @(
    [pscustomobject]@{ gateId='hygiene-pr15'; key='repository-a'; path=$repoRoot; head='4d05393b4501a0b4d701f6ba6e1d051e9bd35ec2'; name='repository-a-source.zip'; repo='carlrabbit/dotnet-ai-code-hygiene'; base='48a87618d02278a178cad02f44a924e207ec88d3'; expectedHeadCount=96 },
    [pscustomobject]@{ gateId='semantic-model-source'; key='repository-b'; path=$SemanticModelRepositoryPath; head='88b5ad5092e38e00257d4e327d6606afa2bf4952'; name='repository-b-source.zip'; repo='carlrabbit/dotnet-semantic-type-model'; base='9ba2bf4dc8268b490236d9f287f358d7a840353a'; expectedHeadCount=396 }
)
$sourceRows = [System.Collections.Generic.List[object]]::new()
foreach ($source in $sourceSpecs) {
    $resolved = (Invoke-Git $source.path @('rev-parse', $source.head) "Resolve frozen $($source.key) revision").Trim()
    if ($resolved -ne $source.head) { throw "Frozen head mismatch for $($source.key)." }
    $gitlinks = Invoke-Git $source.path @('ls-tree','-r',$source.head) "Inspect frozen $($source.key) tree for submodules"
    if (@($gitlinks | Where-Object { $_ -match '^160000 ' }).Count -gt 0) { throw "Frozen source archive requires submodule materialization; refusing an incomplete archive for $($source.key)." }
    $trackedPaths = @(Invoke-Git $source.path @('ls-tree','-r','--name-only',$source.head) "Inventory frozen $($source.key) tracked files" | ForEach-Object { $_.Replace('\','/') } | Sort-Object)
    $archivePath = Join-Path $archiveRoot $source.name
    if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
    $null = Invoke-Git $source.path @('archive','--format=zip',"--output=$archivePath",$source.head) "Archive frozen $($source.key) source"
    if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) { throw "Source archive was not created: $archivePath" }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try { $archivePaths = @($zip.Entries | Where-Object { -not $_.FullName.EndsWith('/') } | ForEach-Object { $_.FullName.Replace('\\','/') } | Sort-Object) } finally { $zip.Dispose() }
    if (($archivePaths -join '|') -ne ($trackedPaths -join '|')) { throw "Archive inventory differs from the full tracked tree for $($source.key)." }
    $sourceRows.Add([ordered]@{ sourceKey=$source.key; repository=$source.repo; base=$source.base; head=$source.head; archivePath="source-archives/$($source.name)"; archiveSha256=(Get-Hash $archivePath); trackedFileCount=$trackedPaths.Count; headCandidateCount=$source.expectedHeadCount })
}

$independentSeeds = [ordered]@{
    'hygiene-pr15'='M0015-stageA-independent-control-hygiene-20261011'
    'semantic-model-source'='M0015-stageA-independent-control-semantic-model-20261011'
}
$controlDrawCount = 5
$itemsById = @{}
$privateRows = [System.Collections.Generic.List[object]]::new()
$sourceCounts = @{}
foreach ($source in $sourceSpecs) {
    $resultPath = Join-Path $repoRoot "tools/M0015/results/$($source.gateId)-comparison.json"
    $headPath = Join-Path $repoRoot "tools/M0015/results/$($source.gateId)-head-candidates.json"
    $result = Get-Content $resultPath -Raw | ConvertFrom-Json
    $head = Get-Content $headPath -Raw | ConvertFrom-Json
    if ($head.candidateCount -ne $source.expectedHeadCount -or $head.candidates.Count -ne $source.expectedHeadCount) { throw "Frozen head population changed for $($source.gateId)." }
    $sourceIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $policyMembership = @{}
    foreach ($policy in @('uniform','change-first','history-aware')) {
        foreach ($candidate in @($result.policies.$policy.selected)) {
            $id = [string]$candidate.id
            $null = $sourceIds.Add($id)
            if (-not $policyMembership.ContainsKey($id)) { $policyMembership[$id] = [System.Collections.Generic.List[string]]::new() }
            $policyMembership[$id].Add($policy)
            if (-not $itemsById.ContainsKey("$($source.gateId)|$id")) { $itemsById["$($source.gateId)|$id"] = [pscustomobject]@{ source=$source; candidate=$candidate; groups=[System.Collections.Generic.List[string]]::new() } }
            $itemsById["$($source.gateId)|$id"].groups.Add($policy)
        }
    }
    $controlSeed = $independentSeeds[$source.gateId]
    $controlDraws = @($head.candidates | ForEach-Object {
        $candidate = $_
        $input = "$controlSeed`0$($source.gateId)`0$($candidate.id)"
        $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($input)))
        [pscustomobject]@{ candidate=$candidate; rank=$hash }
    } | Sort-Object rank, @{Expression={$_.candidate.id}} | Select-Object -First $controlDrawCount)
    foreach ($draw in $controlDraws) {
        $id = [string]$draw.candidate.id
        $key = "$($source.gateId)|$id"
        if (-not $itemsById.ContainsKey($key)) { $itemsById[$key] = [pscustomobject]@{ source=$source; candidate=$draw.candidate; groups=[System.Collections.Generic.List[string]]::new() } }
        $itemsById[$key].groups.Add('independent-control')
    }
    $sourceCounts[$source.gateId] = [ordered]@{ policySelectionUnionCount=$sourceIds.Count; independentControlDrawCount=$controlDraws.Count; uniqueAssessmentCount=@($itemsById.Keys | Where-Object { $_.StartsWith("$($source.gateId)|", [StringComparison]::Ordinal) }).Count }
}

$random = [Security.Cryptography.RandomNumberGenerator]::Create()
$blindRows = [System.Collections.Generic.List[object]]::new()
$orderedCandidateKeys = @($itemsById.Keys | Sort-Object)
foreach ($key in $orderedCandidateKeys) {
    $row = $itemsById[$key]
    $bytes = [byte[]]::new(12); $random.GetBytes($bytes)
    $token = 'M15A-' + [Convert]::ToHexString($bytes)
    $blindRows.Add([ordered]@{ token=$token; sourceKey=$row.source.key; path=$row.candidate.path; symbol=$row.candidate.symbol; disambiguator=[int]$row.candidate.disambiguator })
    $privateRows.Add([ordered]@{ token=$token; gateId=$row.source.gateId; candidateId=$row.candidate.id; path=$row.candidate.path; symbol=$row.candidate.symbol; groups=@($row.groups | Sort-Object -Unique) })
}
$random.Dispose()

$blinded = [ordered]@{ schemaVersion=1; kind='m0015-policy-blinded-boringness-assessment-list'; rule='architecture.boringness.review@2'; items=@($blindRows) }
$blindedPath = Join-Path $packageRoot 'BLINDED-CANDIDATES.json'
Write-JsonFile $blindedPath $blinded
$archiveSourceRows = @($sourceRows)
$manifest = [ordered]@{
    schemaVersion=1
    kind='m0015-stage-a-frozen-review-manifest'
    stage='A-user-run-semantic-review-handoff'
    frozenAtUtc=[DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    rule='architecture.boringness.review@2'
    purpose='Independent project-owner assessment; selection policy and control membership are blinded.'
    assessmentCount=$blindRows.Count
    blindedCandidateListSha256=(Get-Hash $blindedPath)
    controlProtocol=[ordered]@{ method='five independent SHA-256 seeded draws without replacement within each source, using separate seeds; draw from the complete eligible frozen head population independently of policy selections'; seeds=$independentSeeds; drawsPerSource=$controlDrawCount; membershipNotEncodedInBlindedList=$true }
    sources=$archiveSourceRows
    sourcePopulationSummary=$sourceCounts
    judgmentContract=[ordered]@{ questionAnswers=@('yes','no','uncertain'); outcomes=@('accepted','negative','uncertain'); escalation='Q4 yes or at least two yes answers among Q1,Q2,Q3,Q5'; noJudgmentsIncluded=$true }
}
$manifestPath = Join-Path $packageRoot 'FROZEN-MANIFEST.json'
Write-JsonFile $manifestPath $manifest
$manifestHash = Get-Hash $manifestPath

$instructions = @'
# M0015 Stage A — Independent BORINGness Assessment

## Purpose and blinding

Assess each token independently against the fixed BORINGness questions using only the two frozen source archives. The candidate list intentionally contains no policy labels or control markers. Do not inspect the repository's selector comparison results, prior mapping files, or internal implementation notes before locking the judgments. Do not infer why a candidate was sampled. No automated assessment is part of this handoff.

The submission population is frozen at  __ASSESSMENT_COUNT__ unique candidate declarations. `sourceKey` in the blinded list identifies which source archive to inspect; each listed path and fully qualified symbol is anchored to the exact head revision in `FROZEN-MANIFEST.json`.

## Source archives

- `source-archives/repository-a-source.zip` — `carlrabbit/dotnet-ai-code-hygiene` at head `4d05393b4501a0b4d701f6ba6e1d051e9bd35ec2`.
- `source-archives/repository-b-source.zip` — `carlrabbit/dotnet-semantic-type-model` at head `88b5ad5092e38e00257d4e327d6606afa2bf4952`.

Extract each archive separately. For every list item, locate its `path`, then read the declaration and enough surrounding callers, registrations, adapters, and owning types to assess the current architecture. Do not assess from the symbol name alone. The `disambiguator` identifies repeated declarations with the same symbol in a file.

## Fixed Q1–Q5 rubric

Answer each question `yes`, `no`, or `uncertain`. “Yes” means the signal is present.

- **Q1 — Speculative abstraction:** Is there an interface, provider, factory, strategy, generic mechanism, extension point, or similar abstraction with only one meaningful production use/implementation and no current requirement for variability?
- **Q2 — Indirection:** To understand what this code actually does, must you follow several forwarding/delegation layers before reaching the behavior?
- **Q3 — Change locality:** Would a small behavioral change require coordinated edits across multiple plumbing types, registrations, adapters, or mappings rather than mostly changing the code that owns the behavior?
- **Q4 — Hidden machinery:** Does the implementation rely on reflection, dynamic discovery, convention-based registration, a custom DSL/framework, or other implicit machinery where ordinary explicit C# could satisfy the current requirement?
- **Q5 — Unclear ownership:** After reading the relevant types, is it difficult to identify the single place that owns the behavior or state being reviewed?

Legitimate boundaries are not failures merely because they currently have one implementation. External-service, persistence, platform, interoperability, or other real boundaries may justify abstraction. Evaluate the current requirement, not hypothetical future extensibility.

Outcome rule: `negative` when Q4 is yes or at least two of Q1/Q2/Q3/Q5 are yes; `accepted` when all five answers are determined and that escalation threshold is not met; `uncertain` when the source evidence is insufficient for a reliable outcome. One non-Q4 signal alone is review evidence, not planner escalation. Record a brief rationale and source-relative evidence pointers. Do not change source code or produce implementation suggestions as if they were findings.

## Complete the judgment template

Copy `JUDGMENT-TEMPLATE.json` to `M0015-judgments.json`. Keep the frozen `schemaVersion`, `kind`, `manifestSha256`, and every token. Fill reviewer identity/role, UTC completion timestamp, confirm independence, and complete each token's Q1–Q5 answers, outcome, rationale, and source-relative evidence. Evidence may be an empty array only when the outcome is `uncertain`; explain what is unavailable in the rationale. Do not add policy or control labels.

Run the included validator from this folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\validate-submission.ps1 -Action validate -PackageRoot . -SubmissionPath .\M0015-judgments.json
```

The validator checks structure, frozen token coverage, rubric/outcome consistency, source-relative evidence references, and hashes. It does not judge the semantic correctness of answers or reveal selection membership.

## Return path

Return only the completed `M0015-judgments.json` to `M0015-review/submissions/` (the project owner’s submission folder). Keep this handoff package unchanged. Do not add judgments to the repository until the owner requests Stage C. Missing or uncertain judgments should remain explicit; do not convert absence into acceptance.
'@
$instructions = $instructions.Replace('__ASSESSMENT_COUNT__', [string]$blindRows.Count)
[IO.File]::WriteAllText((Join-Path $packageRoot 'ASSESSOR-INSTRUCTIONS.md'), $instructions + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))

$templateItems = foreach ($item in $blindRows) { [ordered]@{ token=$item.token; q1=$null; q2=$null; q3=$null; q4=$null; q5=$null; outcome=$null; rationale=$null; evidence=@() } }
$template = [ordered]@{ schemaVersion=1; kind='m0015-boringness-assessment-submission'; manifestSha256=$manifestHash; reviewer=[ordered]@{ reviewerId=$null; role=$null; independenceConfirmed=$null; completedAtUtc=$null }; items=@($templateItems) }
Write-JsonFile (Join-Path $packageRoot 'JUDGMENT-TEMPLATE.json') $template

$schema = [ordered]@{
    '$schema'='https://json-schema.org/draft/2020-12/schema';
    type='object'; additionalProperties=$false; required=@('schemaVersion','kind','manifestSha256','reviewer','items');
    properties=[ordered]@{
        schemaVersion=[ordered]@{const=1}; kind=[ordered]@{const='m0015-boringness-assessment-submission'}; manifestSha256=[ordered]@{type='string';pattern='^[A-F0-9]{64}$'};
        reviewer=[ordered]@{type='object';required=@('reviewerId','role','independenceConfirmed','completedAtUtc');properties=[ordered]@{reviewerId=[ordered]@{type='string';minLength=1};role=[ordered]@{type='string';minLength=1};independenceConfirmed=[ordered]@{type='boolean'};completedAtUtc=[ordered]@{type='string';format='date-time'}}};
        items=[ordered]@{type='array';minItems=$blindRows.Count;maxItems=$blindRows.Count;items=[ordered]@{type='object';required=@('token','q1','q2','q3','q4','q5','outcome','rationale','evidence');properties=[ordered]@{token=[ordered]@{type='string'};q1=[ordered]@{enum=@('yes','no','uncertain')};q2=[ordered]@{enum=@('yes','no','uncertain')};q3=[ordered]@{enum=@('yes','no','uncertain')};q4=[ordered]@{enum=@('yes','no','uncertain')};q5=[ordered]@{enum=@('yes','no','uncertain')};outcome=[ordered]@{enum=@('accepted','negative','uncertain')};rationale=[ordered]@{type='string';minLength=1};evidence=[ordered]@{type='array';items=[ordered]@{type='object';required=@('path','note');properties=[ordered]@{path=[ordered]@{type='string';minLength=1};note=[ordered]@{type='string';minLength=1}}}}}}}
    }
}
Write-JsonFile (Join-Path $packageRoot 'JUDGMENT-SCHEMA.json') $schema

$readme = @'
# M0015 Review Handoff

This package supports the project owner's independent, policy-blinded assessment of frozen BORINGness candidates. It contains no judgments and no token-to-policy mapping.

Start with `ASSESSOR-INSTRUCTIONS.md`. Verify `SHA256SUMS.txt` before use. Source archives are in `source-archives/`. Fill `JUDGMENT-TEMPLATE.json`, validate the completed copy, and return it to `M0015-review/submissions/M0015-judgments.json`.

`validate-submission.ps1` validates only the submission structure and frozen token coverage; it does not perform semantic review or unblind policy membership.
'@
[IO.File]::WriteAllText((Join-Path $packageRoot 'README.md'), $readme + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))

$privateParent = Split-Path -Parent ([IO.Path]::GetFullPath($PrivateMapPath))
New-Item -ItemType Directory -Path $privateParent -Force | Out-Null
$private = [ordered]@{ schemaVersion=1; kind='private-m0015-token-policy-map'; frozenManifestSha256=$manifestHash; warning='Do not copy into the blinded handoff or submit to the assessor before judgments are locked.'; entries=@($privateRows) }
Write-JsonFile ([IO.Path]::GetFullPath($PrivateMapPath)) $private

if (Test-Path -LiteralPath $stagedHandoff) { Remove-Item -LiteralPath $stagedHandoff -Recurse -Force }
New-Item -ItemType Directory -Path $stagedHandoff | Out-Null
Get-ChildItem -LiteralPath $packageRoot -File | Where-Object Name -ne 'SHA256SUMS.txt' | Copy-Item -Destination $stagedHandoff -Force
New-Item -ItemType Directory -Path (Join-Path $stagedHandoff 'source-archives') | Out-Null
Get-ChildItem -LiteralPath $archiveRoot -File | Copy-Item -Destination (Join-Path $stagedHandoff 'source-archives') -Force
$sumLines = foreach ($file in (Get-ChildItem -LiteralPath $stagedHandoff -File -Recurse | Sort-Object FullName)) {
    $relative = [IO.Path]::GetRelativePath($stagedHandoff, $file.FullName).Replace('\','/')
    "$(Get-Hash $file.FullName)  $relative"
}
[IO.File]::WriteAllLines((Join-Path $stagedHandoff 'SHA256SUMS.txt'), [string[]]$sumLines, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $stagedHandoff 'SHA256SUMS.txt') -Destination (Join-Path $packageRoot 'SHA256SUMS.txt') -Force

$null = Test-Handoff $stagedHandoff
"Prepared blinded Stage A population: $($blindRows.Count) unique candidates; source A=$($sourceCounts['hygiene-pr15'].uniqueAssessmentCount), source B=$($sourceCounts['semantic-model-source'].uniqueAssessmentCount). Source archives are complete tracked-tree ZIPs at both frozen heads. Private token mapping saved outside the repository and handoff. Stage directory: $stagedHandoff"
