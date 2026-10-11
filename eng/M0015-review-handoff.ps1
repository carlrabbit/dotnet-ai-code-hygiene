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

function Get-Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant() }
function Write-JsonFile([string]$Path, [object]$Value, [int]$Depth = 24) {
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth $Depth) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}
function Invoke-Git([string]$Root, [string[]]$Arguments, [string]$Description) {
    $output = & git -C $Root @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Description failed: $($output -join ' ')" }
    return $output
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
    $actual = @(Get-ChildItem -LiteralPath $Root -File -Recurse | Where-Object FullName -ne $sumPath | ForEach-Object { [IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\', '/') } | Sort-Object)
    $expected = @($entries.Keys | Sort-Object)
    if (($actual -join '|') -ne ($expected -join '|')) { throw 'Handoff inventory differs from SHA256SUMS.txt (missing or unexpected files).' }
    foreach ($relative in $expected) {
        $path = Join-Path $Root ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
        if ((Get-Hash $path) -ne $entries[$relative]) { throw "Handoff checksum mismatch: $relative" }
    }

    $manifestPath = Join-Path $Root 'assessment-manifest.json'
    $candidatePath = Join-Path $Root 'blinded-candidates.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -Depth 30
    $candidateList = Get-Content -LiteralPath $candidatePath -Raw | ConvertFrom-Json -Depth 20
    if ((Get-Hash $candidatePath) -ne $manifest.blindedCandidateListSha256) { throw 'Candidate list checksum does not match the frozen manifest.' }
    if ((Get-Hash (Join-Path $Root 'protocol.json')) -ne $manifest.protocolSha256) { throw 'Frozen protocol checksum does not match the assessment manifest.' }
    if ($candidateList.items.Count -ne $manifest.assessmentCount) { throw 'Assessment count does not match the blinded candidate list.' }
    $itemTokens = @($candidateList.items | ForEach-Object token | Sort-Object -Unique)
    $manifestTokens = @($manifest.subjects | ForEach-Object token | Sort-Object -Unique)
    if (($itemTokens -join '|') -ne ($manifestTokens -join '|')) { throw 'Manifest subject tokens do not exactly match the blinded candidate list.' }
    foreach ($source in $manifest.sources) {
        $archivePath = Join-Path $Root $source.archivePath
        if ((Get-Hash $archivePath) -ne $source.archiveSha256) { throw "Frozen archive checksum mismatch: $($source.archivePath)" }
        $zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
        try {
            $paths = @($zip.Entries | Where-Object { -not $_.FullName.EndsWith('/') } | ForEach-Object FullName)
            foreach ($entry in $zip.Entries) { if (-not $entry.FullName.EndsWith('/')) { $stream = $entry.Open(); try { $stream.CopyTo([IO.Stream]::Null) } finally { $stream.Dispose() } } }
            foreach ($subject in @($manifest.subjects | Where-Object sourceKey -eq $source.sourceKey)) {
                if ($subject.archivedPath -notin $paths) { throw "Subject source path is not reachable in archive: $($subject.archivedPath)" }
            }
        } finally { $zip.Dispose() }
    }
    return [pscustomobject]@{ status='passed'; checksummedFileCount=$expected.Count; assessmentCount=$manifest.assessmentCount; sha256Manifest=(Get-Hash $sumPath); archiveCount=$manifest.sources.Count }
}

if ($Action -eq 'verify') {
    if ([string]::IsNullOrWhiteSpace($HandoffRoot)) { throw 'Pass -HandoffRoot for the staged or delivered handoff directory.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $result = Test-Handoff (Resolve-Path $HandoffRoot).Path
    "Handoff verification passed: $($result.checksummedFileCount) payload files, $($result.assessmentCount) assessments, $($result.archiveCount) complete archives, SHA256SUMS=$($result.sha256Manifest)"
    exit 0
}

if ($Action -eq 'test-validator') {
    $validator = Join-Path $packageRoot 'validate-judgments.ps1'
    $results = Join-Path $repoRoot 'tools/M0015/fixtures/review-handoff/validator-test-results.json'
    & $validator -Action test -PackageRoot $packageRoot -TestResultsPath $results
    exit 0
}

if ([string]::IsNullOrWhiteSpace($SemanticModelRepositoryPath)) { throw 'Pass -SemanticModelRepositoryPath for the frozen public Semantic Type Model clone.' }
if ([string]::IsNullOrWhiteSpace($StageRoot)) { $StageRoot = Join-Path $env:TEMP 'M0015-review-handoff-stage-a' }
if ([string]::IsNullOrWhiteSpace($PrivateMapPath)) { $PrivateMapPath = 'C:\src\m0015-evidence\stage-a-private\token-policy-map.json' }
$SemanticModelRepositoryPath = (Resolve-Path $SemanticModelRepositoryPath).Path
$StageRoot = [IO.Path]::GetFullPath($StageRoot)
$archiveRoot = Join-Path $StageRoot 'repositories'
$stagedHandoff = Join-Path $StageRoot 'handoff'
New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
foreach ($legacyName in @('ASSESSOR-INSTRUCTIONS.md','BLINDED-CANDIDATES.json','FROZEN-MANIFEST.json','JUDGMENT-SCHEMA.json','JUDGMENT-TEMPLATE.json','validate-submission.ps1')) {
    $legacyPath = Join-Path $packageRoot $legacyName
    if (Test-Path -LiteralPath $legacyPath -PathType Leaf) { Remove-Item -LiteralPath $legacyPath -Force }
}

$sourceSpecs = @(
    [pscustomobject]@{ gateId='hygiene-pr15'; key='repository-a'; path=$repoRoot; head='4d05393b4501a0b4d701f6ba6e1d051e9bd35ec2'; name='dotnet-ai-code-hygiene-4d05393b4501.zip'; repo='carlrabbit/dotnet-ai-code-hygiene'; url='https://github.com/carlrabbit/dotnet-ai-code-hygiene'; base='48a87618d02278a178cad02f44a924e207ec88d3'; expectedHeadCount=96 },
    [pscustomobject]@{ gateId='semantic-model-source'; key='repository-b'; path=$SemanticModelRepositoryPath; head='88b5ad5092e38e00257d4e327d6606afa2bf4952'; name='dotnet-semantic-type-model-88b5ad5092e3.zip'; repo='carlrabbit/dotnet-semantic-type-model'; url='https://github.com/carlrabbit/dotnet-semantic-type-model'; base='9ba2bf4dc8268b490236d9f287f358d7a840353a'; expectedHeadCount=396 }
)

Add-Type -AssemblyName System.IO.Compression.FileSystem
$sourceRows = [System.Collections.Generic.List[object]]::new()
foreach ($source in $sourceSpecs) {
    $resolved = (Invoke-Git $source.path @('rev-parse', $source.head) "Resolve frozen $($source.key) revision").Trim()
    if ($resolved -ne $source.head) { throw "Frozen head mismatch for $($source.key)." }
    $trackedPaths = @(Invoke-Git $source.path @('ls-tree','-r','--name-only',$source.head) "Inventory frozen $($source.key) tree" | ForEach-Object { $_.Replace('\','/') } | Sort-Object)
    $sensitive = @($trackedPaths | Where-Object { $_ -match '(?i)(^|/)(\.env$|credentials?\.json$|secrets?\.json$|id_rsa$|id_ed25519$|.*\.(pfx|p12|key))' })
    if ($sensitive.Count -gt 0) { throw "Sensitive-looking tracked paths need review before archiving $($source.key): $($sensitive -join ', ')" }
    $gitlinks = Invoke-Git $source.path @('ls-tree','-r',$source.head) "Inspect frozen $($source.key) tree for submodules"
    if (@($gitlinks | Where-Object { $_ -match '^160000 ' }).Count -gt 0) { throw "Frozen archive requires submodule materialization; refusing incomplete archive for $($source.key)." }
    $archivePath = Join-Path $archiveRoot $source.name
    if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
    $null = Invoke-Git $source.path @('archive','--format=zip',"--output=$archivePath",$source.head) "Archive frozen $($source.key) source"
    $zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $archivePaths = @($zip.Entries | Where-Object { -not $_.FullName.EndsWith('/') } | ForEach-Object { $_.FullName.Replace('\','/') } | Sort-Object)
        if (($archivePaths -join '|') -ne ($trackedPaths -join '|')) { throw "Archive inventory differs from full tracked tree for $($source.key)." }
        foreach ($entry in $zip.Entries) { if (-not $entry.FullName.EndsWith('/')) { $stream = $entry.Open(); try { $stream.CopyTo([IO.Stream]::Null) } finally { $stream.Dispose() } } }
    } finally { $zip.Dispose() }
    $sourceRows.Add([ordered]@{ sourceKey=$source.key; repository=$source.repo; url=$source.url; base=$source.base; head=$source.head; archivePath="repositories/$($source.name)"; archiveSha256=(Get-Hash $archivePath); trackedFileCount=$trackedPaths.Count; eligibleHeadCandidateCount=$source.expectedHeadCount; sensitivePathInventory='no tracked sensitive-looking paths found' })
}

$controlSeeds = [ordered]@{
    'hygiene-pr15'='M0015-stageA-control-hygiene-20261011-v1'
    'semantic-model-source'='M0015-stageA-control-semantic-model-20261011-v1'
}
$itemsByKey = @{}
$privateRows = [System.Collections.Generic.List[object]]::new()
$sourceCounts = [ordered]@{}
foreach ($source in $sourceSpecs) {
    $result = Get-Content (Join-Path $repoRoot "tools/M0015/results/$($source.gateId)-comparison.json") -Raw | ConvertFrom-Json -Depth 30
    $head = Get-Content (Join-Path $repoRoot "tools/M0015/results/$($source.gateId)-head-candidates.json") -Raw | ConvertFrom-Json -Depth 30
    if ($head.candidateCount -ne $source.expectedHeadCount -or $head.candidates.Count -ne $source.expectedHeadCount) { throw "Frozen head population changed for $($source.gateId)." }
    $headById = @{}; foreach ($candidate in $head.candidates) { $headById[[string]$candidate.id] = $candidate }
    $unionIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $membership = @{}
    foreach ($policy in @('uniform','change-first','history-aware')) {
        $selected = @($result.policies.$policy.selected)
        if ($selected.Count -ne 5) { throw "Expected exactly five candidates for $policy at $($source.gateId)." }
        foreach ($candidate in $selected) {
            $id = [string]$candidate.id; $null = $unionIds.Add($id)
            if (-not $membership.ContainsKey($id)) { $membership[$id] = [System.Collections.Generic.List[string]]::new() }
            $membership[$id].Add($policy)
            $headCandidate = $headById[$id]
            if ($null -eq $headCandidate) { throw "Selected identity is missing from frozen head population: $id" }
            $entryKey = "$($source.gateId)|$id"
            if (-not $itemsByKey.ContainsKey($entryKey)) { $itemsByKey[$entryKey] = [pscustomobject]@{source=$source;candidate=$headCandidate;groups=[System.Collections.Generic.List[string]]::new();control=$false} }
            $itemsByKey[$entryKey].groups.Add($policy)
        }
    }

    $seed = $controlSeeds[$source.gateId]
    $control = @($head.candidates | Where-Object { -not $unionIds.Contains([string]$_.id) } | ForEach-Object {
        $candidate = $_; $payload = "$seed`0$($source.gateId)`0$($candidate.id)"
        [pscustomobject]@{candidate=$candidate;rank=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($payload)))}
    } | Sort-Object rank, @{Expression={$_.candidate.id}} | Select-Object -First 1)
    if ($control.Count -ne 1) { throw "Could not select one independent eligible control outside policy union for $($source.gateId)." }
    $controlKey = "$($source.gateId)|$($control[0].candidate.id)"
    $itemsByKey[$controlKey] = [pscustomobject]@{source=$source;candidate=$control[0].candidate;groups=[System.Collections.Generic.List[string]]::new();control=$true}
    $sourceCounts[$source.gateId] = [ordered]@{eligiblePopulation=$head.candidateCount;selectionUnionCount=$unionIds.Count;perPolicySelectionCount=5;independentControlCount=1;uniqueAssessmentCount=@($itemsByKey.Keys | Where-Object { $_.StartsWith("$($source.gateId)|",[StringComparison]::Ordinal) }).Count}
}

$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
$candidateRows = [System.Collections.Generic.List[object]]::new()
$subjectRows = [System.Collections.Generic.List[object]]::new()
foreach ($key in @($itemsByKey.Keys | Sort-Object)) {
    $record = $itemsByKey[$key]; $candidate = $record.candidate
    $bytes = [byte[]]::new(16); $rng.GetBytes($bytes); $token = 'M15A-' + [Convert]::ToHexString($bytes)
    $row = [ordered]@{token=$token;sourceKey=$record.source.key;repository=$record.source.url;revision=$record.source.head;archivedPath=$candidate.path;project=$candidate.project;symbol=$candidate.symbol;disambiguator=[int]$candidate.disambiguator}
    $candidateRows.Add($row)
    $subjectRows.Add([ordered]@{token=$token;sourceKey=$record.source.key;repository=$record.source.url;revision=$record.source.head;project=$candidate.project;archivedPath=$candidate.path;symbol=$candidate.symbol;disambiguator=[int]$candidate.disambiguator;fingerprint=$candidate.fingerprint})
    $privateRows.Add([ordered]@{token=$token;gateId=$record.source.gateId;candidateId=$candidate.id;repository=$record.source.repo;revision=$record.source.head;project=$candidate.project;path=$candidate.path;symbol=$candidate.symbol;disambiguator=[int]$candidate.disambiguator;fingerprint=$candidate.fingerprint;policyMembership=@($record.groups | Sort-Object -Unique);independentControl=$record.control})
}
$rng.Dispose()
if ($candidateRows.Count -gt 20) { throw "Assessment population exceeds the milestone maximum of 20: $($candidateRows.Count)." }

$privateParent = Split-Path -Parent ([IO.Path]::GetFullPath($PrivateMapPath)); New-Item -ItemType Directory -Path $privateParent -Force | Out-Null
$privateMap = [ordered]@{schemaVersion=1;kind='private-m0015-token-candidate-policy-map';protocolVersion='m0015-stage-a-amended-20261011-v1';entries=@($privateRows)}
$privateMapPathFull = [IO.Path]::GetFullPath($PrivateMapPath); Write-JsonFile $privateMapPathFull $privateMap
$internalMapPath = Join-Path $repoRoot 'tools/M0015/review-handoff-private/token-policy-map.json'
New-Item -ItemType Directory -Path (Split-Path -Parent $internalMapPath) -Force | Out-Null
Write-JsonFile $internalMapPath $privateMap

$candidateList = [ordered]@{schemaVersion=1;kind='m0015-blinded-assessment-frame';protocolVersion='m0015-stage-a-amended-20261011-v1';items=@($candidateRows)}
$candidateListPath = Join-Path $packageRoot 'blinded-candidates.json'; Write-JsonFile $candidateListPath $candidateList
$qRubric = @(
    [ordered]@{id='Q1';name='Speculative abstraction';text='Is there an interface, provider, factory, strategy, generic mechanism, extension point, or similar abstraction with only one meaningful production use/implementation and no current requirement for variability?'},
    [ordered]@{id='Q2';name='Indirection';text='To understand what this code actually does, must you follow several forwarding/delegation layers before reaching the behavior?'},
    [ordered]@{id='Q3';name='Change locality';text='Would a small behavioral change require coordinated edits across multiple plumbing types, registrations, adapters, or mappings rather than mostly changing the code that owns the behavior?'},
    [ordered]@{id='Q4';name='Hidden machinery';text='Does the implementation rely on reflection, dynamic discovery, convention-based registration, a custom DSL/framework, or other implicit machinery where ordinary explicit C# could satisfy the current requirement?'},
    [ordered]@{id='Q5';name='Unclear ownership';text='After reading the relevant types, is it difficult to identify the single place that owns the behavior or state being reviewed?'}
)
$analysisPlan = @('Validate submission manifest and source revisions before unblinding.','Do not inspect PR #16, selection artifacts, or policy mappings before judgment submission is locked.','Compare only observed judgments for the same assessed identities at equal review budget; account for shared candidates, partial coverage, uncertain outcomes, and small N.','Report scheduling selection separately from semantic findings. Controls follow the frozen protocol; no population-wide extrapolation is permitted.','These judgments are post-gate outcomes and cannot calibrate the same gate; temporal transfer requires later-gate evidence.')
$protocol = [ordered]@{schemaVersion=1;protocolVersion='m0015-stage-a-amended-20261011-v1';rule='architecture.boringness.review@2';selectionFrame=[ordered]@{policySets=@('uniform','change-first','history-aware');selectedCountPerPolicy=5;membership='identity union across all three frozen policy sets per source-changing gate';maximumAssessmentCount=20};controlProtocol=[ordered]@{method='rank eligible head candidates outside the policy-selection union by SHA-256(seed NUL gateId NUL stableCandidateId), choose smallest rank and break ties by stableCandidateId';seeds=$controlSeeds;controlsPerGate=1};rubric=[ordered]@{questions=$qRubric;answers=@('yes','no','uncertain');escalation='Q4=yes or at least two yes answers among Q1,Q2,Q3,Q5';abstention='Any uncertain answer requires an uncertain decision with explanation.'};analysisPlan=$analysisPlan;dataSchemaVersion=1}
$protocolPath = Join-Path $packageRoot 'protocol.json'; Write-JsonFile $protocolPath $protocol
$manifest = [ordered]@{
    schemaVersion=1;kind='m0015-stage-a-assessment-manifest';protocolVersion='m0015-stage-a-amended-20261011-v1';protocolSha256=(Get-Hash $protocolPath);frozenAtUtc=[DateTimeOffset]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ');rule='architecture.boringness.review@2';ruleVersion=2;assessmentCount=$candidateRows.Count
    sources=@($sourceRows);subjects=@($subjectRows);sourcePopulationSummary=$sourceCounts;blindedCandidateListSha256=(Get-Hash $candidateListPath);tokenMappingSha256=(Get-Hash $internalMapPath)
    controlProtocol=[ordered]@{method='For each gate, rank eligible head candidates outside the union of the three five-candidate policy selections by SHA-256(seed NUL gateId NUL stableCandidateId); choose the smallest rank, with stableCandidateId as tie-breaker.';seeds=$controlSeeds;controlsPerGate=1;outsidePolicySelectionUnion=$true}
    policySelectionFrame=[ordered]@{policies=@('uniform','change-first','history-aware');selectionsPerPolicy=5;coverage='union of all selected candidate identities for the frozen gate; policy membership is withheld in the user-facing package';historicalGateResults='exploratory; the assessment frame is newly frozen for this protocol'}
    rubric=[ordered]@{questions=$qRubric;answers=@('yes','no','uncertain');escalation='negative when Q4=yes or at least two yes answers among Q1,Q2,Q3,Q5; accepted when all answers are determined and the threshold is not met; uncertain when insufficient evidence prevents a reliable decision';abstention='Use uncertain if any question cannot be assessed from the frozen archive; leave partial items absent rather than inferring an answer.'}
    analysisPlan=$analysisPlan
    dataSchemaVersion=1;submissionSchema='judgments-schema.json';noJudgmentsIncluded=$true
}
$manifestPath = Join-Path $packageRoot 'assessment-manifest.json'; Write-JsonFile $manifestPath $manifest
$manifestHash = Get-Hash $manifestPath

$templateItems = foreach ($row in $candidateRows) { [ordered]@{token=$row.token;sourceRevision=$row.revision;fingerprint=($subjectRows | Where-Object token -eq $row.token).fingerprint;q1=$null;q2=$null;q3=$null;q4=$null;q5=$null;decision=$null;evidence=@();uncertainty=$null} }
$template = [ordered]@{schemaVersion=1;kind='m0015-semantic-review-judgments';protocolVersion=$manifest.protocolVersion;manifestSha256=$manifestHash;reviewer=[ordered]@{reviewerId=$null;role=$null;model=$null;sessionId=$null;completedAtUtc=$null};items=@($templateItems)}
Write-JsonFile (Join-Path $packageRoot 'judgments-template.json') $template

$schemaText = @'
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["schemaVersion", "kind", "protocolVersion", "manifestSha256", "reviewer", "items"],
  "properties": {
    "schemaVersion": {"const": 1},
    "kind": {"const": "m0015-semantic-review-judgments"},
    "protocolVersion": {"type": "string", "minLength": 1},
    "manifestSha256": {"type": "string", "pattern": "^[A-F0-9]{64}$"},
    "reviewer": {"type": "object", "required": ["reviewerId", "role", "model", "sessionId", "completedAtUtc"], "properties": {"reviewerId": {"type": "string", "minLength": 1}, "role": {"type": "string", "minLength": 1}, "model": {"type": "string", "minLength": 1}, "sessionId": {"type": "string", "minLength": 1}, "completedAtUtc": {"type": "string", "format": "date-time"}}},
    "items": {"type": "array", "items": {"type": "object", "required": ["token", "sourceRevision", "fingerprint", "q1", "q2", "q3", "q4", "q5", "decision", "evidence"], "properties": {"token": {"type": "string", "minLength": 1}, "sourceRevision": {"type": "string", "pattern": "^[A-Fa-f0-9]{40}$"}, "fingerprint": {"type": "string", "pattern": "^[A-Fa-f0-9]{64}$"}, "q1": {"enum": ["yes", "no", "uncertain"]}, "q2": {"enum": ["yes", "no", "uncertain"]}, "q3": {"enum": ["yes", "no", "uncertain"]}, "q4": {"enum": ["yes", "no", "uncertain"]}, "q5": {"enum": ["yes", "no", "uncertain"]}, "decision": {"enum": ["accepted", "negative", "uncertain"]}, "evidence": {"type": "array", "items": {"type": "object", "required": ["path", "note"], "properties": {"path": {"type": "string", "minLength": 1}, "line": {"type": ["integer", "null"], "minimum": 1}, "note": {"type": "string", "minLength": 1}}}}, "uncertainty": {"type": ["string", "null"]}}}}
  }
}
'@
[IO.File]::WriteAllText((Join-Path $packageRoot 'judgments-schema.json'), $schemaText + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))

$prompt = @'
# M0015 independent BORINGness assessment

You are the project owner conducting a manual, policy-blinded assessment. Assess only the candidate declarations in `blinded-candidates.json` against the exact repository head snapshots. Do not inspect PR #16, comparison outputs, selection artifacts, or any token-to-policy mapping before locking and returning your judgments. Do not infer why an item was included. This handoff contains no model-generated or prefilled semantic judgments.

## Frozen sources and candidates

Each list item identifies a source archive by `sourceKey`, repository URL, exact head revision, archived path, project, fully qualified symbol, and declaration disambiguator. Extract the matching ZIP from `repositories/` and inspect the declaration plus enough callers, registrations, adapters, and owning types to assess the actual architecture. Do not assess from a symbol name alone. The matching declaration fingerprint and revision are frozen in `assessment-manifest.json` and the template.

## Q1–Q5 rubric

Answer `yes`, `no`, or `uncertain`; “yes” means the signal is present.

- **Q1 — Speculative abstraction:** Is there an interface, provider, factory, strategy, generic mechanism, extension point, or similar abstraction with only one meaningful production use/implementation and no current requirement for variability?
- **Q2 — Indirection:** To understand what this code actually does, must you follow several forwarding/delegation layers before reaching the behavior?
- **Q3 — Change locality:** Would a small behavioral change require coordinated edits across multiple plumbing types, registrations, adapters, or mappings rather than mostly changing the code that owns the behavior?
- **Q4 — Hidden machinery:** Does the implementation rely on reflection, dynamic discovery, convention-based registration, a custom DSL/framework, or other implicit machinery where ordinary explicit C# could satisfy the current requirement?
- **Q5 — Unclear ownership:** After reading the relevant types, is it difficult to identify the single place that owns the behavior or state being reviewed?

Legitimate external-service, persistence, platform, and interoperability boundaries are not failures merely because they currently have one implementation. Assess the current requirement, not hypothetical future extensibility.

## Decision and abstention

`negative` when Q4 is yes or at least two of Q1/Q2/Q3/Q5 are yes. `accepted` when every answer is determined and the threshold is not met. `uncertain` when source evidence is insufficient; any uncertain question requires an uncertain decision. Cite source-relative `path`, optional line/anchor, and a short note. Use `uncertainty` to explain abstention. Missing items in a partial submission remain incomplete; do not convert them to accepted.

## Return format

Copy `judgments-template.json`, retain `schemaVersion`, `kind`, `protocolVersion`, `manifestSha256`, and tokens. Record actual reviewer identity, role, model or human workflow, session ID, UTC completion time, source revision and fingerprint for each judged item, Q1–Q5 answers, decision, source evidence, and uncertainty where applicable. Partial submissions may include only completed items; do not add a status or fabricate absent items.

Format-only partial example (no judgment answers):

```json
{"schemaVersion":1,"kind":"m0015-semantic-review-judgments","protocolVersion":"<copy from manifest>","manifestSha256":"<copy SHA-256>","reviewer":{"reviewerId":"<actual>","role":"<actual>","model":"<actual workflow>","sessionId":"<actual session>","completedAtUtc":"<UTC timestamp>"},"items":[]}
```

Validate the file locally with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\validate-judgments.ps1 -Action validate -PackageRoot . -SubmissionPath .\judgments-<review-session-id>.json
```

The validator reports `incomplete` for a valid partial submission and does not infer judgments. Return the completed JSON under `M0015-review/submissions/`. Keep the handoff unchanged. Do not convert missing or uncertain items to acceptance.
'@
[IO.File]::WriteAllText((Join-Path $packageRoot 'JUDGMENT-PROMPT.md'), $prompt + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
$readme = @'
# M0015 Stage A handoff

Read `JUDGMENT-PROMPT.md`, verify `SHA256SUMS.txt`, and inspect only the frozen ZIP files under `repositories/`. This package is policy-blinded and contains no semantic judgments. Return completed or partial reviewer-authored JSON under `M0015-review/submissions/`; keep this frozen handoff unchanged.
'@
[IO.File]::WriteAllText((Join-Path $packageRoot 'README.md'), $readme + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))

if (Test-Path -LiteralPath $stagedHandoff) { Remove-Item -LiteralPath $stagedHandoff -Recurse -Force }
New-Item -ItemType Directory -Path $stagedHandoff | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stagedHandoff 'repositories') | Out-Null
Get-ChildItem -LiteralPath $packageRoot -File | Where-Object Name -ne 'SHA256SUMS.txt' | Copy-Item -Destination $stagedHandoff -Force
Get-ChildItem -LiteralPath $archiveRoot -File | Copy-Item -Destination (Join-Path $stagedHandoff 'repositories') -Force
$sumLines = foreach ($file in (Get-ChildItem -LiteralPath $stagedHandoff -File -Recurse | Sort-Object FullName)) { "$(Get-Hash $file.FullName)  $([IO.Path]::GetRelativePath($stagedHandoff,$file.FullName).Replace('\','/'))" }
[IO.File]::WriteAllLines((Join-Path $stagedHandoff 'SHA256SUMS.txt'), [string[]]$sumLines, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $stagedHandoff 'SHA256SUMS.txt') -Destination (Join-Path $packageRoot 'SHA256SUMS.txt') -Force
$null = Test-Handoff $stagedHandoff
"Prepared amended Stage A population: $($candidateRows.Count) unique candidates; source A=$($sourceCounts['hygiene-pr15'].uniqueAssessmentCount), source B=$($sourceCounts['semantic-model-source'].uniqueAssessmentCount). One deterministic control per source was selected outside each policy union. Archives contain complete clean tracked trees. Private token/policy map saved outside repository and handoff. Stage directory: $stagedHandoff"
