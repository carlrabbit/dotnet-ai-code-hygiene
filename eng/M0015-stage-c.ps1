# M0015 Stage C: independently recompute and verify judgment outcomes against frozen selections.
# This deliberately does NOT create or modify the committed Stage C artifact.
param(
    [Parameter(Mandatory = $true)]
    [string]$SubmissionPath,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$handoff = Join-Path $root 'tools/M0015/review-handoff'
$results = Join-Path $root 'tools/M0015/results'
$mapPath = Join-Path $root 'tools/M0015/review-handoff-private/token-policy-map.json'
$stagePath = Join-Path $results 'stage-c-semantic-assessment.json'

function Read-Json([string]$path) {
    Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -Depth 100 -DateKind String
}
function Assert([bool]$ok, [string]$message) {
    if (-not $ok) { throw "Stage C verification: $message" }
}
function Same-Set($actual, $expected) {
    $a = @($actual | ForEach-Object { [string]$_ } | Sort-Object)
    $b = @($expected | ForEach-Object { [string]$_ } | Sort-Object)
    return (($a.Count -eq $b.Count) -and (($a -join [char]0) -ceq ($b -join [char]0)))
}
function Policy-Selection($comparison, [string]$policy) {
    $prop = $comparison.policies.PSObject.Properties[$policy]
    Assert ($null -ne $prop) "missing source policy $policy"
    return @($prop.Value.selected)
}

$submission = (Resolve-Path -LiteralPath $SubmissionPath).Path
$validationJson = & (Join-Path $handoff 'validate-judgments.ps1') -Action validate -PackageRoot $handoff -SubmissionPath $submission
$validation = $validationJson | ConvertFrom-Json -Depth 20
Assert ($validation.status -eq 'valid-complete') 'source submission is not valid-complete'

$actual = Read-Json $submission
$manifest = Read-Json (Join-Path $handoff 'assessment-manifest.json')
$map = Read-Json $mapPath
$committed = Read-Json $stagePath
$sha = (Get-FileHash -LiteralPath $submission -Algorithm SHA256).Hash.ToUpperInvariant()
Assert ($sha -ceq $committed.submissionSha256) 'submitted file SHA-256 differs from committed Stage C'
Assert ($sha -ceq $committed.submissionReceipt.frozenCopySha256) 'frozen copy receipt SHA-256 differs'
Assert ($committed.manifestSha256 -ceq $actual.manifestSha256) 'manifest reference differs'
Assert ($committed.protocolVersion -ceq $actual.protocolVersion) 'protocol version differs'
Assert ($manifest.assessmentCount -eq @($actual.items).Count) 'frozen assessment count differs'
Assert ($map.protocolVersion -ceq $actual.protocolVersion) 'private mapping protocol differs'
Assert ($committed.assessmentCount -eq @($actual.items).Count) 'committed assessment count differs'
Assert (@($map.entries).Count -eq @($actual.items).Count) 'mapping does not cover all assessed subjects'

foreach ($field in @('reviewerId', 'role', 'model', 'sessionId', 'completedAtUtc')) {
    Assert ([string]$actual.reviewer.$field -ceq [string]$committed.reviewerProvenance.$field) "reviewer provenance differs: $field"
}

$byToken = @{}
foreach ($judgment in $actual.items) { $byToken[[string]$judgment.token] = $judgment }
$byId = @{}
$verified = [System.Collections.Generic.List[object]]::new()
$decisions = @{ actionable = 0; accepted = 0; uncertain = 0 }
$questions = @{ q1 = 0; q2 = 0; q3 = 0; q4 = 0; q5 = 0 }

foreach ($entry in $map.entries) {
    $token = [string]$entry.token
    Assert ($byToken.ContainsKey($token)) "missing submission token $token"
    Assert (-not $byId.ContainsKey($token)) "duplicate mapping token $token"
    $byId[$token] = $entry
    $j = $byToken[$token]
    $row = @($committed.candidates | Where-Object { $_.token -ceq $token })
    Assert ($row.Count -eq 1) "committed candidate identity missing or duplicate: $token"
    $c = $row[0]
    foreach ($field in @('gateId','candidateId','repository','project','path','symbol','fingerprint')) {
        Assert ([string]$c.$field -ceq [string]$entry.$field) "candidate $token mismatch: $field"
    }
    Assert ($c.independentControl -eq $entry.independentControl) "control designation mismatch: $token"
    Assert (Same-Set $c.policyMembership $entry.policyMembership) "policy membership mismatch: $token"
    foreach ($field in @('q1','q2','q3','q4','q5','decision')) {
        Assert ([string]$c.$field -ceq [string]$j.$field) "judgment mismatch for $token/$field"
    }
    switch ($j.decision) {
        negative { $decisions.actionable++ }
        accepted { $decisions.accepted++ }
        uncertain { $decisions.uncertain++ }
        default { throw "Unexpected decision for $token" }
    }
    foreach ($field in @('q1','q2','q3','q4','q5')) {
        if ($j.$field -eq 'yes') { $questions[$field]++ }
    }
    $verified.Add([pscustomobject]@{
        token = $token
        gateId = [string]$entry.gateId
        candidateId = [string]$entry.candidateId
        change = [string]$c.change
        policyMembership = @($entry.policyMembership)
        decision = [string]$j.decision
        independentControl = [bool]$entry.independentControl
    })
}
foreach ($field in @('actionable','accepted','uncertain')) {
    Assert ($committed.decisionCounts.$field -eq $decisions[$field]) "decision tally mismatch: $field"
}
foreach ($field in @('q1','q2','q3','q4','q5')) {
    Assert ($committed.yesAnswerCounts.$field -eq $questions[$field]) "positive-signal tally mismatch: $field"
}

$gateReport = [System.Collections.Generic.List[object]]::new()
foreach ($gate in $committed.gates) {
    $id = [string]$gate.gateId
    $comparison = Read-Json (Join-Path $results "$id-comparison.json")
    Assert ($comparison.gateId -ceq $id) "frozen gate identity mismatch: $id"
    $members = @($verified | Where-Object { $_.gateId -ceq $id })
    $controls = @($members | Where-Object { $_.independentControl })
    Assert ($members.Count -eq $gate.assessedCount) "assessed count mismatch: $id"
    Assert ($controls.Count -eq $gate.controlCount) "control count mismatch: $id"
    Assert (($members.Count - $controls.Count) -eq $gate.selectionUnionCount) "selected-union count mismatch: $id"
    $policyReport = [System.Collections.Generic.List[object]]::new()
    foreach ($policy in @('uniform','change-first','history-aware')) {
        $selected = @(Policy-Selection $comparison $policy)
        $byPolicy = @($members | Where-Object { $_.policyMembership -contains $policy })
        Assert (Same-Set ($selected | ForEach-Object { $_.id }) ($byPolicy | ForEach-Object { $_.candidateId })) "policy identity mismatch: $id/$policy"
        $record = @($gate.policies | Where-Object { $_.policy -ceq $policy })
        Assert ($record.Count -eq 1) "committed policy result missing: $id/$policy"
        $p = $record[0]
        Assert (Same-Set $p.selectedTokens ($byPolicy | ForEach-Object { $_.token })) "selected tokens mismatch: $id/$policy"
        Assert ($p.selectedCount -eq $selected.Count) "selected count mismatch: $id/$policy"
        $changed = @($byPolicy | Where-Object { $_.change -eq 'NewOrChanged' }).Count
        $negative = @($byPolicy | Where-Object { $_.decision -eq 'negative' }).Count
        $accepted = @($byPolicy | Where-Object { $_.decision -eq 'accepted' }).Count
        $uncertain = @($byPolicy | Where-Object { $_.decision -eq 'uncertain' }).Count
        Assert ($p.changedCount -eq $changed) "changed count mismatch: $id/$policy"
        Assert ($p.backgroundOrUnknownCount -eq ($selected.Count - $changed)) "background count mismatch: $id/$policy"
        Assert ($p.actionableCount -eq $negative -and $p.acceptedCount -eq $accepted -and $p.uncertainCount -eq $uncertain) "judgment yield mismatch: $id/$policy"
        $policyReport.Add([pscustomobject]@{ policy=$policy; selected=$selected.Count; changed=$changed; actionable=$negative; accepted=$accepted; uncertain=$uncertain })
    }
    foreach ($overlap in $gate.pairwiseOverlap) {
        $aa = @(Policy-Selection $comparison $overlap.policyA | ForEach-Object { $_.id })
        $bb = @(Policy-Selection $comparison $overlap.policyB | ForEach-Object { $_.id })
        $shared = @($aa | Where-Object { $bb -contains $_ })
        Assert ($shared.Count -eq $overlap.sharedCount) "pairwise overlap mismatch: $id"
        $sharedNegative = @($members | Where-Object { ($shared -contains $_.candidateId) -and $_.decision -eq 'negative' }).Count
        Assert ($sharedNegative -eq $overlap.sharedActionableCount) "pairwise actionable overlap mismatch: $id"
    }
    $d = $gate.displacement
    $sourceD = $comparison.displacement
    foreach ($field in @('changedSelectionCountDifference','changedCandidateIdentityDisplacementCount')) {
        Assert ($d.$field -eq $sourceD.$field) "displacement count mismatch: $id/$field"
    }
    Assert (Same-Set ($d.omittedChangedCandidates | ForEach-Object { $_.id }) ($sourceD.changeFirstChangedCandidatesOmittedByHistoryAware | ForEach-Object { $_.id })) "omitted changed identities differ: $id"
    Assert (Same-Set ($d.historyAwareReplacements | ForEach-Object { $_.id }) ($sourceD.historyAwareReplacementCandidates | ForEach-Object { $_.id })) "replacement identities differ: $id"
    foreach ($replacement in $d.historyAwareReplacements) {
        $source = @($sourceD.historyAwareReplacementCandidates | Where-Object { $_.id -ceq $replacement.id })
        Assert ($source.Count -eq 1 -and $source[0].selectionStage -ceq $replacement.selectionStage) "replacement stage differs: $id"
    }
    $gateReport.Add([pscustomobject]@{ gateId=$id; assessed=$members.Count; controls=$controls.Count; policyResults=@($policyReport.ToArray()); changedDisplaced=$d.changedCandidateIdentityDisplacementCount })
}
Assert (@($gateReport).Count -eq 2) 'expected two source-changing gate outcome comparisons'

$budgetReports = [System.Collections.Generic.List[object]]::new()
foreach ($scenario in $committed.budgetScenarios) {
    $comparison = Read-Json (Join-Path $results "$($scenario.gateId)-comparison.json")
    $selected = @(Policy-Selection $comparison $scenario.policy)
    $changed = @($selected | Where-Object { $_.change -eq 'NewOrChanged' }).Count
    Assert ($scenario.selectedCount -eq $selected.Count) "budget selected-count mismatch: $($scenario.gateId)/$($scenario.policy)"
    Assert ($scenario.changedSelected -eq $changed) "budget changed-count mismatch: $($scenario.gateId)/$($scenario.policy)"
    Assert ($scenario.backgroundOrUnknownSelected -eq ($selected.Count - $changed)) "budget background-count mismatch: $($scenario.gateId)/$($scenario.policy)"
    Assert ($scenario.budgetCap -eq $comparison.budget) "budget cap mismatch: $($scenario.gateId)/$($scenario.policy)"
    Assert ($scenario.newOrChangedCandidatePopulation -eq $comparison.population.newOrChanged) "budget population mismatch: $($scenario.gateId)"
    Assert ($scenario.unknownImpactCandidatePopulation -eq $comparison.population.unknownImpact) "budget unknown impact mismatch: $($scenario.gateId)"
    Assert ($scenario.zeroBudgetSelectionCount -eq 0 -and $scenario.zeroBudgetReviewCount -eq 0 -and $scenario.budgetIsCeiling) 'zero-budget or ceiling assertion differs'
    $budgetReports.Add([pscustomobject]@{ gateId=$scenario.gateId; policy=$scenario.policy; budgetCap=$scenario.budgetCap; selected=$selected.Count; changed=$changed })
}
Assert (@($budgetReports).Count -eq 9) 'expected nine gate/policy budget scenarios'

$output = [ordered]@{
    status = 'verified'
    submissionSha256 = $sha
    manifestSha256 = [string]$actual.manifestSha256
    assessmentCount = @($verified).Count
    decisionCounts = $decisions
    yesAnswerCounts = $questions
    gates = @($gateReport.ToArray())
    budgetScenarios = @($budgetReports.ToArray())
    note = 'Deterministic verification of Stage C semantic metrics and identity joins; delivery receipt is separately provenance-bound, not regenerated.'
}
$json = $output | ConvertTo-Json -Depth 30
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}
$json
