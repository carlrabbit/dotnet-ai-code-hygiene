param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('validate', 'test')]
    [string]$Action,
    [string]$PackageRoot,
    [string]$SubmissionPath,
    [string]$TestResultsPath
)

$ErrorActionPreference = 'Stop'

function Get-Sha256([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
}

function Read-Json([string]$Path) {
    Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -Depth 40 -DateKind String
}

function Test-M0015Submission([string]$Root, [string]$SubmissionFile) {
    $manifestPath = Join-Path $Root 'FROZEN-MANIFEST.json'
    $candidateListPath = Join-Path $Root 'BLINDED-CANDIDATES.json'
    foreach ($required in @($manifestPath, $candidateListPath, $SubmissionFile)) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required submission input is missing: $required" }
    }
    $manifest = Read-Json $manifestPath
    $candidateList = Read-Json $candidateListPath
    $submission = Read-Json $SubmissionFile
    if ($manifest.schemaVersion -ne 1 -or $manifest.kind -ne 'm0015-stage-a-frozen-review-manifest') { throw 'Unsupported frozen manifest.' }
    if ($candidateList.schemaVersion -ne 1 -or $candidateList.kind -ne 'm0015-policy-blinded-boringness-assessment-list') { throw 'Unsupported blinded candidate list.' }
    if ((Get-Sha256 $candidateListPath) -ne $manifest.blindedCandidateListSha256) { throw 'Blinded candidate list checksum does not match the frozen manifest.' }
    if ($submission.schemaVersion -ne 1 -or $submission.kind -ne 'm0015-boringness-assessment-submission') { throw 'Unsupported judgment submission schema.' }
    if ($submission.manifestSha256 -ne (Get-Sha256 $manifestPath)) { throw 'Submission references a different frozen manifest.' }
    if ([string]::IsNullOrWhiteSpace([string]$submission.reviewer.reviewerId) -or [string]::IsNullOrWhiteSpace([string]$submission.reviewer.role)) { throw 'Reviewer identity and role are required.' }
    if ($submission.reviewer.independenceConfirmed -ne $true) { throw 'Reviewer independence must be explicitly confirmed.' }
    try { $completed = [DateTimeOffset]::Parse([string]$submission.reviewer.completedAtUtc) } catch { throw 'Reviewer completion time must be a UTC timestamp.' }
    if ($completed.Offset -ne [TimeSpan]::Zero) { throw "Reviewer completion time must be a UTC timestamp; got $($submission.reviewer.completedAtUtc) parsed as offset $($completed.Offset)." }

    $expected = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entry in @($candidateList.items)) {
        if ([string]::IsNullOrWhiteSpace([string]$entry.token) -or -not $expected.Add([string]$entry.token)) { throw 'Frozen candidate tokens must be nonempty and unique.' }
    }
    if ($expected.Count -ne [int]$manifest.assessmentCount) { throw 'Assessment count does not match the frozen list.' }
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    if (@($submission.items).Count -ne $expected.Count) { throw 'Submission must contain exactly the frozen assessment population.' }
    foreach ($item in @($submission.items)) {
        $token = [string]$item.token
        if (-not $expected.Contains($token)) { throw "Unknown candidate token: $token" }
        if (-not $seen.Add($token)) { throw "Duplicate candidate token: $token" }
        $answers = @()
        foreach ($question in @('q1','q2','q3','q4','q5')) {
            $answer = [string]$item.$question
            if ($answer -notin @('yes','no','uncertain')) { throw "Invalid or missing $question answer for $token." }
            $answers += $answer
        }
        if ([string]::IsNullOrWhiteSpace([string]$item.rationale)) { throw "A rationale is required for $token." }
        if ($null -eq $item.evidence) { throw "Evidence references must be present for $token (an empty array is allowed only when uncertain)." }
        foreach ($reference in @($item.evidence)) {
            if ([string]::IsNullOrWhiteSpace([string]$reference.path) -or [string]::IsNullOrWhiteSpace([string]$reference.note)) { throw "Evidence references require a source path and note for $token." }
            if ([IO.Path]::IsPathRooted([string]$reference.path) -or ([string]$reference.path -split '[/\\]' | Where-Object { $_ -eq '..' }).Count -gt 0) { throw "Evidence path must be relative to the frozen source archive for $token." }
        }
        if ($item.outcome -notin @('accepted','negative','uncertain')) { throw "Invalid outcome for $token." }
        $allDetermined = @($answers | Where-Object { $_ -eq 'uncertain' }).Count -eq 0
        if (-not $allDetermined -and $item.outcome -ne 'uncertain') { throw "Uncertain Q1-Q5 answers require an uncertain outcome for $token." }
        if ($allDetermined) {
            $escalates = $answers[3] -eq 'yes' -or (@($answers[0],$answers[1],$answers[2],$answers[4] | Where-Object { $_ -eq 'yes' }).Count -ge 2)
            if ($escalates -and $item.outcome -ne 'negative') { throw "Q1-Q5 escalation threshold requires a negative outcome for $token." }
            if (-not $escalates -and $item.outcome -eq 'negative') { throw "Negative outcome does not meet the Q1-Q5 escalation threshold for $token." }
        }
        if ($item.outcome -ne 'uncertain' -and @($item.evidence).Count -eq 0) { throw "Determined outcomes require at least one source evidence reference for $token." }
    }
    if ($seen.Count -ne $expected.Count) { throw 'Submission is missing one or more frozen candidate tokens.' }
    return [pscustomobject]@{ status='valid'; assessedCount=$seen.Count; manifestSha256=(Get-Sha256 $manifestPath) }
}

if ($Action -eq 'validate') {
    if ([string]::IsNullOrWhiteSpace($PackageRoot) -or [string]::IsNullOrWhiteSpace($SubmissionPath)) { throw 'Pass -PackageRoot and -SubmissionPath.' }
    $result = Test-M0015Submission (Resolve-Path $PackageRoot).Path (Resolve-Path $SubmissionPath).Path
    $result | ConvertTo-Json -Compress
    exit 0
}

$testRoot = Join-Path $env:TEMP ('m0015-validator-fixtures-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$fixtureRows = [System.Collections.Generic.List[object]]::new()
try {
    $tokens = @('SYNTHETIC-TOKEN-01','SYNTHETIC-TOKEN-02')
    $candidates = [ordered]@{ schemaVersion=1; kind='m0015-policy-blinded-boringness-assessment-list'; items=@($tokens | ForEach-Object { [ordered]@{ token=$_ } }) }
    $candidatePath = Join-Path $testRoot 'BLINDED-CANDIDATES.json'
    [IO.File]::WriteAllText($candidatePath, ($candidates | ConvertTo-Json -Depth 10) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    $manifest = [ordered]@{ schemaVersion=1; kind='m0015-stage-a-frozen-review-manifest'; assessmentCount=2; blindedCandidateListSha256=(Get-Sha256 $candidatePath) }
    $manifestPath = Join-Path $testRoot 'FROZEN-MANIFEST.json'
    [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 10) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    $manifestHash = Get-Sha256 $manifestPath
    $reviewer = [ordered]@{ reviewerId='SYNTHETIC-REVIEWER'; role='schema-fixture'; independenceConfirmed=$true; completedAtUtc='2026-01-01T00:00:00Z' }
    $makeItem = { param($token,$q1,$q2,$q3,$q4,$q5,$outcome) [ordered]@{ token=$token; q1=$q1; q2=$q2; q3=$q3; q4=$q4; q5=$q5; outcome=$outcome; rationale='Synthetic fixture rationale only.'; evidence=@([ordered]@{ path='src/Synthetic.cs'; note='Synthetic validator fixture; not a code assessment.' }) } }
    $validItems = @((&$makeItem $tokens[0] no no no no no accepted), (&$makeItem $tokens[1] no no no yes no negative))
    $makeSubmission = { param($items) [ordered]@{ schemaVersion=1; kind='m0015-boringness-assessment-submission'; manifestSha256=$manifestHash; reviewer=$reviewer; items=@($items) } }
    $cases = @(
        [pscustomobject]@{id='valid-synthetic-accepted-and-negative'; items=$validItems; expected='pass'},
        [pscustomobject]@{id='reject-duplicate-token'; items=@($validItems[0],$validItems[0]); expected='fail'; expectedError='Duplicate candidate token'},
        [pscustomobject]@{id='reject-missing-token'; items=@($validItems[0]); expected='fail'; expectedError='exactly the frozen assessment population'},
        [pscustomobject]@{id='reject-escalation-mismatch'; items=@((&$makeItem $tokens[0] yes yes no no no accepted),$validItems[1]); expected='fail'; expectedError='Q1-Q5 escalation threshold'},
        [pscustomobject]@{id='reject-unknown-token'; items=@($validItems[0],(&$makeItem 'SYNTHETIC-UNKNOWN' no no no no no accepted)); expected='fail'; expectedError='Unknown candidate token'},
        [pscustomobject]@{id='reject-mismatched-manifest'; items=$validItems; expected='fail'; expectedError='different frozen manifest'; overrideManifest='0'*64}
    )
    foreach ($case in $cases) {
        $submission = &$makeSubmission $case.items
        if ($case.overrideManifest) { $submission.manifestSha256 = $case.overrideManifest }
        $submissionPath = Join-Path $testRoot ($case.id + '.json')
        [IO.File]::WriteAllText($submissionPath, ($submission | ConvertTo-Json -Depth 12) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
        $actual = 'pass'; $message = ''
        try { $null = Test-M0015Submission $testRoot $submissionPath } catch { $actual = 'fail'; $message = $_.Exception.Message }
        if ($actual -ne $case.expected) { throw "Synthetic validator fixture $($case.id) expected $($case.expected), got $actual. $message" }
        if ($case.expectedError -and $message -notlike "*$($case.expectedError)*") { throw "Synthetic validator fixture $($case.id) failed for the wrong reason: $message" }
        $fixtureRows.Add([ordered]@{ id=$case.id; expected=$case.expected; actual=$actual; expectedError=$case.expectedError; observedError=$message; syntheticOnly=$true })
    }
    $output = [ordered]@{ schemaVersion=1; kind='m0015-synthetic-validator-test-results'; fixtures=$fixtureRows }
    if (-not [string]::IsNullOrWhiteSpace($TestResultsPath)) { [IO.File]::WriteAllText($TestResultsPath, ($output | ConvertTo-Json -Depth 10) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false)) }
    "Validator fixtures passed: $($fixtureRows.Count); valid cases=1; rejected synthetic schema cases=$($fixtureRows.Count - 1); no semantic judgments evaluated."
} finally {
    Remove-Item -LiteralPath $testRoot -Recurse -Force
}
