param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('validate', 'test')]
    [string]$Action,
    [string]$PackageRoot,
    [string]$SubmissionPath,
    [string]$TestResultsPath
)

$ErrorActionPreference = 'Stop'
function Get-Sha256([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant() }
function Read-Json([string]$Path) { Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -Depth 40 -DateKind String }

function Test-Judgments([string]$Root, [string]$SubmissionFile) {
    $manifestPath = Join-Path $Root 'assessment-manifest.json'
    $candidatePath = Join-Path $Root 'blinded-candidates.json'
    foreach ($required in @($manifestPath,$candidatePath,$SubmissionFile)) { if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required input is missing: $required" } }
    $manifest = Read-Json $manifestPath; $candidateList = Read-Json $candidatePath; $submission = Read-Json $SubmissionFile
    if ($manifest.schemaVersion -ne 1 -or $manifest.kind -ne 'm0015-stage-a-assessment-manifest') { throw 'Unsupported assessment manifest.' }
    if ($candidateList.schemaVersion -ne 1 -or $candidateList.kind -ne 'm0015-blinded-assessment-frame') { throw 'Unsupported blinded candidate list.' }
    if ((Get-Sha256 $candidatePath) -ne $manifest.blindedCandidateListSha256) { throw 'Blinded candidate list hash differs from the frozen manifest.' }
    if ($submission.schemaVersion -ne 1 -or $submission.kind -ne 'm0015-semantic-review-judgments') { throw 'Unsupported judgment submission schema.' }
    if ($submission.protocolVersion -ne $manifest.protocolVersion) { throw 'Submission references a different protocol version.' }
    if ($submission.manifestSha256 -ne (Get-Sha256 $manifestPath)) { throw 'Submission references a different manifest hash.' }
    foreach ($field in @('reviewerId','role','model','sessionId','completedAtUtc')) { if ([string]::IsNullOrWhiteSpace([string]$submission.reviewer.$field)) { throw "Reviewer provenance field is missing: $field" } }
    try { $completed = [DateTimeOffset]::Parse([string]$submission.reviewer.completedAtUtc) } catch { throw 'Reviewer completion time must be an ISO UTC timestamp.' }
    if ($completed.Offset -ne [TimeSpan]::Zero) { throw 'Reviewer completion time must be UTC.' }
    if (@($submission.items).Count -gt [int]$manifest.assessmentCount) { throw 'Submission contains more items than the frozen assessment frame.' }

    $expected = @{}; foreach ($subject in $manifest.subjects) { if ($expected.ContainsKey([string]$subject.token)) { throw 'Frozen manifest has duplicate subject tokens.' }; $expected[[string]$subject.token] = $subject }
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($item in @($submission.items)) {
        $token = [string]$item.token
        if (-not $expected.ContainsKey($token)) { throw "Unknown candidate token: $token" }
        if (-not $seen.Add($token)) { throw "Duplicate/conflicting answer token: $token" }
        $subject = $expected[$token]
        if ([string]$item.sourceRevision -ne [string]$subject.revision) { throw "Source revision mismatch for $token." }
        if ([string]$item.fingerprint -ne [string]$subject.fingerprint) { throw "Source fingerprint mismatch for $token." }
        $answers = @()
        foreach ($question in @('q1','q2','q3','q4','q5')) {
            $answer = [string]$item.$question
            if ($answer -notin @('yes','no','uncertain')) { throw "Missing or invalid $question answer for $token." }
            $answers += $answer
        }
        if ($item.decision -notin @('accepted','negative','uncertain')) { throw "Missing or invalid decision for $token." }
        $uncertainAnswers = @($answers | Where-Object { $_ -eq 'uncertain' }).Count -gt 0
        $reason = [string]$item.uncertainty
        if ($uncertainAnswers -and $item.decision -ne 'uncertain') { throw "Uncertain Q1-Q5 answers require uncertain/abstained decision for $token." }
        if ($item.decision -eq 'uncertain' -and [string]::IsNullOrWhiteSpace($reason)) { throw "Uncertain/abstained decision requires an uncertainty explanation for $token." }
        if (-not $uncertainAnswers -and $item.decision -ne 'uncertain') {
            $escalates = ($answers[3] -eq 'yes') -or (@($answers[0],$answers[1],$answers[2],$answers[4] | Where-Object { $_ -eq 'yes' }).Count -ge 2)
            if ($escalates -and $item.decision -ne 'negative') { throw "Q1-Q5 escalation requires negative decision for $token." }
            if (-not $escalates -and $item.decision -ne 'accepted') { throw "Q1-Q5 does not support negative decision for $token." }
        }
        if ($null -eq $item.evidence) { throw "Evidence array is missing for $token." }
        foreach ($reference in @($item.evidence)) {
            if ([string]::IsNullOrWhiteSpace([string]$reference.path) -or [string]::IsNullOrWhiteSpace([string]$reference.note)) { throw "Evidence requires source-relative path and note for $token." }
            if ([IO.Path]::IsPathRooted([string]$reference.path) -or ([string]$reference.path -split '[/\\]' | Where-Object { $_ -eq '..' }).Count -gt 0) { throw "Evidence path must remain within the frozen source archive for $token." }
        }
        if ($item.decision -ne 'uncertain' -and @($item.evidence).Count -eq 0) { throw "Determined decision requires source evidence for $token." }
    }
    $missing = @($expected.Keys | Where-Object { -not $seen.Contains($_) } | Sort-Object)
    $status = if ($missing.Count -eq 0) { 'valid-complete' } else { 'incomplete' }
    return [pscustomobject]@{status=$status;assessedCount=$seen.Count;expectedCount=$expected.Count;missingTokens=$missing;manifestSha256=(Get-Sha256 $manifestPath)}
}

if ($Action -eq 'validate') {
    if ([string]::IsNullOrWhiteSpace($PackageRoot) -or [string]::IsNullOrWhiteSpace($SubmissionPath)) { throw 'Pass -PackageRoot and -SubmissionPath.' }
    $result = Test-Judgments (Resolve-Path $PackageRoot).Path (Resolve-Path $SubmissionPath).Path
    $result | ConvertTo-Json -Depth 6 -Compress
    exit 0
}

$testRoot = Join-Path $env:TEMP ('m0015-judgment-validator-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$fixtureRows = [System.Collections.Generic.List[object]]::new()
try {
    $tokens = @('SYNTHETIC-TOKEN-01','SYNTHETIC-TOKEN-02')
    $fakeRevision = '1' * 40; $fakeFingerprint = 'A' * 64
    $candidate = [ordered]@{schemaVersion=1;kind='m0015-blinded-assessment-frame';protocolVersion='synthetic-protocol';items=@($tokens | ForEach-Object { [ordered]@{token=$_} })}
    $candidatePath = Join-Path $testRoot 'blinded-candidates.json'; [IO.File]::WriteAllText($candidatePath,($candidate | ConvertTo-Json -Depth 10)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
    $subjects = @($tokens | ForEach-Object { [ordered]@{token=$_;revision=$fakeRevision;fingerprint=$fakeFingerprint} })
    $manifest = [ordered]@{schemaVersion=1;kind='m0015-stage-a-assessment-manifest';protocolVersion='synthetic-protocol';assessmentCount=2;subjects=$subjects;blindedCandidateListSha256=(Get-Sha256 $candidatePath)}
    $manifestPath=Join-Path $testRoot 'assessment-manifest.json'; [IO.File]::WriteAllText($manifestPath,($manifest | ConvertTo-Json -Depth 12)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
    $manifestHash=Get-Sha256 $manifestPath
    $reviewer=[ordered]@{reviewerId='SYNTHETIC-REVIEWER';role='format-fixture';model='synthetic-only';sessionId='SYNTHETIC-SESSION';completedAtUtc='2026-01-01T00:00:00Z'}
    $makeItem={param($token,$answers,$decision) [ordered]@{token=$token;sourceRevision=$fakeRevision;fingerprint=$fakeFingerprint;q1=$answers[0];q2=$answers[1];q3=$answers[2];q4=$answers[3];q5=$answers[4];decision=$decision;evidence=@([ordered]@{path='src/Synthetic.cs';note='Synthetic format fixture only.'});uncertainty=if($decision -eq 'uncertain'){'Synthetic abstention fixture only.'}else{$null}}}
    $accepted=&$makeItem $tokens[0] @('no','no','no','no','no') 'accepted'
    $negative=&$makeItem $tokens[1] @('no','no','no','yes','no') 'negative'
    $uncertain=&$makeItem $tokens[1] @('uncertain','no','no','no','no') 'uncertain'
    $makeSubmission={param($items) [ordered]@{schemaVersion=1;kind='m0015-semantic-review-judgments';protocolVersion='synthetic-protocol';manifestSha256=$manifestHash;reviewer=$reviewer;items=@($items)}}
    $cases=@(
        [pscustomobject]@{id='valid-complete-synthetic-outcomes';items=@($accepted,$negative);expected='valid-complete'},
        [pscustomobject]@{id='valid-partial-is-incomplete';items=@($accepted);expected='incomplete'},
        [pscustomobject]@{id='valid-uncertain-abstention';items=@($uncertain);expected='incomplete'},
        [pscustomobject]@{id='reject-unknown-token';items=@($accepted,(&$makeItem 'SYNTHETIC-UNKNOWN' @('no','no','no','no','no') 'accepted'));expectedError='Unknown candidate token'},
        [pscustomobject]@{id='reject-duplicate-conflicting-token';items=@($accepted,(&$makeItem $tokens[0] @('yes','yes','no','no','no') 'negative'));expectedError='Duplicate/conflicting answer token'},
        [pscustomobject]@{id='reject-wrong-source-revision';items=@($accepted);overrideRevision='2'*40;expectedError='Source revision mismatch'},
        [pscustomobject]@{id='reject-wrong-manifest';items=@($accepted);overrideManifest='0'*64;expectedError='different manifest hash'},
        [pscustomobject]@{id='reject-missing-answer';items=@($accepted);omitQ5=$true;expectedError='Missing or invalid q5'},
        [pscustomobject]@{id='reject-inconsistent-escalation';items=@((&$makeItem $tokens[0] @('yes','yes','no','no','no') 'accepted'));expectedError='Q1-Q5 escalation requires negative'},
        [pscustomobject]@{id='reject-uncertain-as-accepted';items=@((&$makeItem $tokens[0] @('uncertain','no','no','no','no') 'accepted'));expectedError='Uncertain Q1-Q5 answers require'}
    )
    foreach ($case in $cases) {
        $submission=&$makeSubmission $case.items
        $submission=$submission | ConvertTo-Json -Depth 16 | ConvertFrom-Json -Depth 16 -DateKind String
        if ($case.overrideManifest) {$submission.manifestSha256=$case.overrideManifest}
        if ($case.overrideRevision) {$submission.items[0].sourceRevision=$case.overrideRevision}
        if ($case.omitQ5) {$submission.items[0].PSObject.Properties.Remove('q5')}
        $path=Join-Path $testRoot ($case.id+'.json'); [IO.File]::WriteAllText($path,($submission | ConvertTo-Json -Depth 16)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
        $actual='';$message=''
        try {$result=Test-Judgments $testRoot $path;$actual=$result.status} catch {$actual='invalid';$message=$_.Exception.Message}
        if ($case.expected -and $actual -ne $case.expected) {throw "Synthetic fixture $($case.id) expected $($case.expected), got $actual. $message"}
        if ($case.expectedError -and ($actual -ne 'invalid' -or $message -notlike "*$($case.expectedError)*")) {throw "Synthetic fixture $($case.id) failed for the wrong reason: $message"}
        $fixtureRows.Add([ordered]@{id=$case.id;expected=if($case.expected){$case.expected}else{'invalid'};actual=$actual;observedError=$message;syntheticOnly=$true})
    }
    $output=[ordered]@{schemaVersion=1;kind='m0015-synthetic-judgment-validator-results';fixtures=$fixtureRows;semanticJudgmentsEvaluated=$false}
    if (-not [string]::IsNullOrWhiteSpace($TestResultsPath)) {[IO.File]::WriteAllText($TestResultsPath,($output | ConvertTo-Json -Depth 10)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))}
    "Synthetic judgment-validator fixtures passed: $($fixtureRows.Count); valid complete/partial/uncertain cases=3; rejected format cases=$($fixtureRows.Count - 3); no semantic judgments evaluated."
} finally {Remove-Item -LiteralPath $testRoot -Recurse -Force}
