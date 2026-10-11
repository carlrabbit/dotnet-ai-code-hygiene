param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$BaselinePath = (Join-Path $env:LOCALAPPDATA 'Temp\m0013-first-replay.json'),
    [string]$CurrentPath = (Join-Path $RepositoryRoot 'docs/research/ACTIVITY-AGE-REPLAY.json'),
    [string]$OutputPath = (Join-Path $RepositoryRoot 'docs/research/ACTIVITY-AGE-RESULTS.json')
)

$ErrorActionPreference = 'Stop'
$first = Get-Content -LiteralPath $BaselinePath -Raw | ConvertFrom-Json
$second = Get-Content -LiteralPath $CurrentPath -Raw | ConvertFrom-Json
if ($first.completedCheckpointCount -le 0 -or
    $first.completedCheckpointCount -ne $second.completedCheckpointCount -or
    $first.replayCases.Count -ne $first.completedCheckpointCount -or
    $second.replayCases.Count -ne $second.completedCheckpointCount) {
    throw 'Both replay runs must contain the same complete frozen checkpoint segment before comparison.'
}
$expectedCheckpoints = [int]$first.completedCheckpointCount

$mismatches = [System.Collections.Generic.List[object]]::new()
$mismatchCount = 0
$comparedStates = 0
$comparedCheckpoints = 0
$behaviorFields = @('eligibleProjects','eligibleSourceFiles','eligibleSourceLines','projectAgeChangeCount','projectAgeDeltaUnits','repeatProjectAgeDeltaUnits','samplingStateBytes')
$ruleFields = @('ruleId','populationCount','dueBeforeObservation','selectedCount','acceptedCount','dueAfterObservation')
for ($checkpointIndex = 0; $checkpointIndex -lt $expectedCheckpoints; $checkpointIndex++) {
    $a = $first.replayCases[$checkpointIndex]
    $b = $second.replayCases[$checkpointIndex]
    if ($a.repositoryId -cne $b.repositoryId -or $a.firstParentIndex -ne $b.firstParentIndex -or $a.variants.Count -ne 30 -or $b.variants.Count -ne 30) {
        $mismatchCount++
        if ($mismatches.Count -lt 100) { $mismatches.Add([ordered]@{ checkpointIndex = $checkpointIndex; reason = 'checkpoint identity or arm count differs' }) }
        continue
    }
    $comparedCheckpoints++
    for ($variantIndex = 0; $variantIndex -lt 30; $variantIndex++) {
        $av = $a.variants[$variantIndex]; $bv = $b.variants[$variantIndex]
        $equal = $av.policy -ceq $bv.policy -and $av.observation -ceq $bv.observation
        foreach ($field in $behaviorFields) { if ($av.$field -ne $bv.$field) { $equal = $false } }
        if ($av.rules.Count -ne $bv.rules.Count) { $equal = $false }
        else {
            for ($ruleIndex = 0; $ruleIndex -lt $av.rules.Count; $ruleIndex++) {
                foreach ($field in $ruleFields) { if ($av.rules[$ruleIndex].$field -ne $bv.rules[$ruleIndex].$field) { $equal = $false } }
            }
        }
        if (-not $equal) {
            $mismatchCount++
            if ($mismatches.Count -lt 100) { $mismatches.Add([ordered]@{ checkpointIndex = $checkpointIndex; repositoryId = $a.repositoryId; firstParentIndex = $a.firstParentIndex; variantIndex = $variantIndex; policy = $av.policy; observation = $av.observation }) }
        }
        $comparedStates++
    }
}

function Get-Percentile([double[]]$Values, [double]$Percentile) {
    $sorted = @($Values | Sort-Object)
    if ($sorted.Count -eq 0) { return 0 }
    $index = [Math]::Min($sorted.Count - 1, [Math]::Max(0, [Math]::Ceiling($Percentile * $sorted.Count) - 1))
    return [Math]::Round($sorted[$index], 2)
}

$flat = foreach ($checkpoint in $second.replayCases) {
    foreach ($variant in $checkpoint.variants) {
        $boringness = $variant.rules | Where-Object ruleId -eq 'architecture.boringness.review' | Select-Object -First 1
        [pscustomobject]@{
            repositoryId = $checkpoint.repositoryId; visibility = $checkpoint.visibility
            policy = $variant.policy; observation = $variant.observation
            due = [int]$boringness.dueBeforeObservation; selected = [int]$boringness.selectedCount
            accepted = [int]$boringness.acceptedCount; dueAfter = [int]$boringness.dueAfterObservation
            stateBytes = [double]$variant.samplingStateBytes
            samplerPassMilliseconds = [double]($variant.firstRunMilliseconds + $variant.repeatRunMilliseconds)
            activityAgeDeltaUnits = [double]$variant.projectAgeDeltaUnits
            repeatActivityAgeDeltaUnits = [double]$variant.repeatProjectAgeDeltaUnits
        }
    }
}
$summaries = foreach ($group in ($flat | Group-Object repositoryId,policy,observation)) {
    $items = @($group.Group)
    [ordered]@{
        repositoryId = $items[0].repositoryId; visibility = $items[0].visibility
        policy = $items[0].policy; observation = $items[0].observation
        checkpointSeedStates = $items.Count
        boringnessDueEvents = ($items | Measure-Object due -Sum).Sum
        boringnessSelectedTickets = ($items | Measure-Object selected -Sum).Sum
        syntheticAcceptedTickets = ($items | Measure-Object accepted -Sum).Sum
        remainingDueEventsAfterObservation = ($items | Measure-Object dueAfter -Sum).Sum
        stateBytesP50 = Get-Percentile $items.stateBytes 0.50
        stateBytesP95 = Get-Percentile $items.stateBytes 0.95
        stateBytesMaximum = ($items | Measure-Object stateBytes -Maximum).Maximum
        samplerPassMillisecondsP50 = Get-Percentile $items.samplerPassMilliseconds 0.50
        samplerPassMillisecondsP95 = Get-Percentile $items.samplerPassMilliseconds 0.95
        activityAgeDeltaUnitsTotal = [Math]::Round(($items | Measure-Object activityAgeDeltaUnits -Sum).Sum, 6)
        repeatedCheckpointAgeDeltaUnitsTotal = [Math]::Round(($items | Measure-Object repeatActivityAgeDeltaUnits -Sum).Sum, 6)
    }
}
$checkpointSummaries = foreach ($group in ($second.replayCases | Group-Object repositoryId)) {
    $times = [double[]]@($group.Group | ForEach-Object { $_.measuredCheckpointMilliseconds })
    $setup = [double[]]@($group.Group | ForEach-Object { $_.workspaceSetupMilliseconds })
    [ordered]@{
        repositoryId = $group.Name; checkpointCount = $group.Count
        checkpointMillisecondsP50 = Get-Percentile $times 0.50
        checkpointMillisecondsP95 = Get-Percentile $times 0.95
        checkpointMillisecondsMaximum = ($times | Measure-Object -Maximum).Maximum
        workspaceSetupMillisecondsP50 = Get-Percentile $setup 0.50
        workspaceSetupMillisecondsP95 = Get-Percentile $setup 0.95
    }
}
$record = [ordered]@{
    schemaVersion = 1; experiment = 'M0013'; generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    reproducibility = [ordered]@{
        baselineRunStartedAtUtc = $first.runStartedAtUtc; repeatRunStartedAtUtc = $second.runStartedAtUtc
        comparedCheckpoints = $comparedCheckpoints; expectedCheckpoints = $expectedCheckpoints
        comparedPolicySeedObservationStates = $comparedStates; expectedPolicySeedObservationStates = $expectedCheckpoints * 30
        mismatchingStates = $mismatchCount; mismatchExamples = @($mismatches | Select-Object -First 10)
        comparedFields = @($behaviorFields + $ruleFields)
        timingFieldsExcluded = @('measuredCheckpointMilliseconds','workspaceSetupMilliseconds','firstRunMilliseconds','repeatRunMilliseconds')
        baselineSelection = 'Both compared runs use corrected seed labels and exact post-observation due counts; earlier instrument-development passes are excluded.'
    }
    checkpointPerformance = @($checkpointSummaries)
    policyWorkload = @($summaries)
    semanticQualityEvidence = [ordered]@{
        actualBlindedJudgments = 0; equalReviewBudgetsCompleted = @()
        reason = 'No independent reviewer identity/capacity was supplied; synthetic observations are scheduling-only. Effectiveness is inconclusive.'
    }
}
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
[IO.File]::WriteAllText($OutputPath, ($record | ConvertTo-Json -Depth 12), [Text.UTF8Encoding]::new($false))
Write-Output "Compared $comparedStates persisted arm states at $comparedCheckpoints checkpoints; mismatch count $mismatchCount."
Write-Output "Wrote aggregate replay analysis to $OutputPath."
