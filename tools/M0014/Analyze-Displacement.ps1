param(
    [string]$PublicRoot = (Join-Path $env:LOCALAPPDATA 'Temp\m0013-replay'),
    [string]$PrivateRoot = (Join-Path $env:LOCALAPPDATA 'M0013-private'),
    [string]$RepeatRoot = (Join-Path $env:LOCALAPPDATA 'M0014-identity-repeat'),
    [string]$UnitRoot = (Join-Path $env:LOCALAPPDATA 'M0014-population-units'),
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\..\docs\research\ACTIVITY-AGE-SELECTION-METRICS.json'),
    [string]$CheckpointOutputPath = (Join-Path $PSScriptRoot '..\..\docs\research\ACTIVITY-AGE-SELECTION-CHECKPOINTS.jsonl')
)

$ErrorActionPreference = 'Stop'
$validationPath=Join-Path $PSScriptRoot 'Validate-Trace.ps1'
. $validationPath
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manifest = Get-Content (Join-Path $repoRoot 'docs/research/ACTIVITY-AGE-CORPUS.json') -Raw | ConvertFrom-Json
$privateManifest = Get-Content (Join-Path $PrivateRoot 'private-corpus.json') -Raw | ConvertFrom-Json
$allIds = @($manifest.repositories | ForEach-Object id)
$populationUnitCoverage = [System.Collections.Generic.List[object]]::new()
$repos = @{}
foreach ($id in $allIds) {
    $token = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($id))).Substring(0,10).ToLowerInvariant()
    $isPrivate = $id -eq 'carlrabbit/Private'
    $origDir = Join-Path $(if ($isPrivate) { $PrivateRoot } else { $PublicRoot }) "raw-$token"
    $repDir = Join-Path $RepeatRoot $(if ($isPrivate) { "private-$token\raw" } else { "public-$token\raw" })
    $unitDir = Join-Path $UnitRoot $(if ($isPrivate) { "private-$token\raw" } else { "public-$token\raw" })
    $origFiles = @(Get-ChildItem $origDir -File -Filter '*.json' | Sort-Object Name)
    $repFiles = @(Get-ChildItem $repDir -File -Filter '*.json' | Sort-Object Name)
    $unitFiles = @(Get-ChildItem $unitDir -File -Filter '*.json' | Sort-Object Name)
    $expected = if ($isPrivate) { @($privateManifest.checkpoints.checkpoints).Count } else { @(($manifest.repositories | Where-Object id -eq $id).history.checkpoints).Count }
    if ($origFiles.Count -ne $expected) { throw "Retained input checkpoint count mismatch for a corpus member: $($origFiles.Count)/$expected." }
    if ($repFiles.Count -ne $expected) { throw "Repeat input checkpoint count mismatch for a corpus member: $($repFiles.Count)/$expected." }
    if ($unitFiles.Count -ne $expected) { throw "Population-unit input checkpoint count mismatch for a corpus member: $($unitFiles.Count)/$expected." }
    $orig = [System.Collections.Generic.List[object]]::new()
    $rep = [System.Collections.Generic.List[object]]::new()
    $unit = [System.Collections.Generic.List[object]]::new()
    foreach ($f in $origFiles) { $orig.Add((Get-Content $f.FullName -Raw | ConvertFrom-Json)) }
    foreach ($f in $repFiles) { $rep.Add((Get-Content $f.FullName -Raw | ConvertFrom-Json)) }
    foreach ($f in $unitFiles) { $unit.Add((Get-Content $f.FullName -Raw | ConvertFrom-Json)) }
    foreach($checkpoint in $orig) { Assert-M0014TraceVariantCoverage $checkpoint @('A','B','C') 'retained M0013' }
    foreach($checkpoint in $rep) { Assert-M0014TraceVariantCoverage $checkpoint @('B','C') 'first M0014' }
    foreach($checkpoint in $unit) { Assert-M0014TraceVariantCoverage $checkpoint @('B','C') 'population-unit M0014' }
    for($i=0;$i -lt $orig.Count;$i++){
        if($orig[$i].commit -ne $rep[$i].commit -or $orig[$i].commit -ne $unit[$i].commit){
            throw 'A repeat replay revision does not match the frozen input.'
        }
    }
    $selectedPopulationCandidates=0; $missingPopulationUnitIds=0
    foreach($checkpoint in $unit) {
        foreach($variant in @($checkpoint.results | Where-Object { $_.Policy -in @('B','C') })) {
            foreach($rule in @($variant.Rules | Where-Object RuleId -eq 'architecture.boringness.review')) {
                foreach($candidate in @($rule.SelectedCandidates)) {
                    $selectedPopulationCandidates++
                    if(-not $candidate.PopulationUnitId) { $missingPopulationUnitIds++ }
                }
            }
        }
    }
    if($missingPopulationUnitIds -gt 0) { throw 'A selected BORINGness candidate lacks its population-unit identity.' }
    $populationUnitCoverage.Add([ordered]@{
        repository=$(if($isPrivate){'Private'}else{$id.Substring('carlrabbit/'.Length)}); checkpoints=$unit.Count
        selectedPopulationCandidates=$selectedPopulationCandidates; missingPopulationUnitIds=$missingPopulationUnitIds
    })
    $repos[$id] = [ordered]@{ private=$isPrivate; original=$orig.ToArray(); repeat=$rep.ToArray(); unit=$unit.ToArray() }
}

function Get-RuleMap($checkpoint, $policy, $arm, $seed) {
    $variant = @($checkpoint.results | Where-Object { $_.Policy -eq $policy -and $_.Observation -eq $arm -and $_.SeedId -eq $seed })
    if ($variant.Count -ne 1) { throw 'Missing or duplicate paired variant in identity trace.' }
    return $variant[0]
}

function Get-RuleRow($variant, $ruleId) {
    $rows=@($variant.Rules | Where-Object RuleId -eq $ruleId)
    if ($rows.Count -gt 1) { throw "Duplicate rule row in identity trace for '$ruleId'." }
    if ($rows.Count -eq 0) { return $null }
    return $rows[0]
}

function Get-Selection($variant, $ruleId, $kind) {
    $rule = Get-RuleRow $variant $ruleId
    if ($null -eq $rule) { throw "Cannot read selection for absent rule '$ruleId'." }
    if ($kind -eq 'population-unit') { return @($rule.SelectedCandidates | ForEach-Object { [string]$_.PopulationUnitId } | Where-Object { $_ } | Sort-Object -CaseSensitive) }
    return @($rule.SelectedCandidates | ForEach-Object { [string]$_.Identity } | Where-Object { $_ } | Sort-Object -CaseSensitive)
}

function Get-CanonicalIdentityRows($checkpoints) {
    $rows = [System.Collections.Generic.List[string]]::new()
    for ($c=0; $c -lt $checkpoints.Count; $c++) {
        $cp = $checkpoints[$c]
        foreach ($v in @($cp.results | Where-Object { $_.Policy -in @('B','C') } | Sort-Object Policy,Observation,SeedId)) {
            foreach ($r in @($v.Rules | Sort-Object RuleId)) {
                $ids = @($r.SelectedIds | ForEach-Object { [string]$_ } | Sort-Object -CaseSensitive)
                $candidates = @($r.SelectedCandidates | ForEach-Object { [string]$_.Identity } | Where-Object { $_ } | Sort-Object -CaseSensitive)
                $rows.Add((ConvertTo-Json -Compress -InputObject @($c,$v.Policy,$v.Observation,$v.SeedId,$r.RuleId,$ids,$candidates)))
            }
        }
    }
    return $rows.ToArray()
}

function Get-Hash([string[]]$rows) {
    $bytes = [Text.Encoding]::UTF8.GetBytes(($rows -join "`n"))
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-Distribution([double[]]$values) {
    $sorted=@($values | Sort-Object)
    $middle=[int][Math]::Floor(($sorted.Count-1)/2)
    return [ordered]@{min=$sorted[0]; median=$sorted[$middle]; max=$sorted[-1]}
}

$repoMetrics = [System.Collections.Generic.List[object]]::new()
$checkpointMetrics = [System.Collections.Generic.List[object]]::new()
$repeatMetrics = [System.Collections.Generic.List[object]]::new()
$sourceCounts = [System.Collections.Generic.List[object]]::new()
$firstSelectionRows = [System.Collections.Generic.List[object]]::new()
foreach ($id in $allIds) {
    $data = $repos[$id]; $original = $data.original; $repeat = $data.repeat; $unitHistory = $data.unit
    $sourceCounts.Add([ordered]@{repository=$(if($data.private){'Private'}else{$id.Substring('carlrabbit/'.Length)}); count=$original.Count})
    $arms = [System.Collections.Generic.List[object]]::new()
    $ruleSet=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($cp in $original){ foreach($variant in $cp.results){ foreach($rule in $variant.Rules){ $null=$ruleSet.Add([string]$rule.RuleId) } } }
    $rules=@($ruleSet | Sort-Object -CaseSensitive)
    foreach ($arm in @('all','none')) {
        foreach ($ruleId in $rules) {
            $kinds = if ($ruleId -eq 'architecture.boringness.review') { @('population-unit','sampled-type') } else { @('review-subject') }
            foreach ($kind in $kinds) {
              foreach ($seed in @('seed-01','seed-02','seed-03','seed-04','seed-05')) {
                $pairCount=0; $both=0; $bOnly=0; $cOnly=0; $union=0; $jaccardSum=0.0
                $bOccurrences=0; $cOccurrences=0; $bDue=0; $cDue=0; $bSelected=0; $cSelected=0; $bCap=0; $cCap=0
                $bFirst=[Collections.Generic.Dictionary[string,int]]::new([StringComparer]::Ordinal); $cFirst=[Collections.Generic.Dictionary[string,int]]::new([StringComparer]::Ordinal)
                for ($cpIndex=0; $cpIndex -lt $original.Count; $cpIndex++) {
                        $sourceCheckpoint = if($ruleId -eq 'architecture.boringness.review'){$unitHistory[$cpIndex]}else{$original[$cpIndex]}
                        $bv=Get-RuleMap $sourceCheckpoint 'B' $arm $seed
                        $cv=Get-RuleMap $sourceCheckpoint 'C' $arm $seed
                        $br=Get-RuleRow $bv $ruleId; $cr=Get-RuleRow $cv $ruleId
                        if (($null -eq $br) -and ($null -eq $cr)) { continue }
                        if (($null -eq $br) -or ($null -eq $cr)) { throw "Asymmetric rule applicability for '$ruleId' in a paired checkpoint." }
                        $bs=@(Get-Selection $bv $ruleId $kind); $cs=@(Get-Selection $cv $ruleId $kind)
                        $bSet=[Collections.Generic.HashSet[string]]::new([string[]]$bs,[StringComparer]::Ordinal)
                        $cSet=[Collections.Generic.HashSet[string]]::new([string[]]$cs,[StringComparer]::Ordinal)
                        $intersection=[Collections.Generic.HashSet[string]]::new($bSet,[StringComparer]::Ordinal); $intersection.IntersectWith($cSet)
                        $u=[Collections.Generic.HashSet[string]]::new($bSet,[StringComparer]::Ordinal); $u.UnionWith($cSet)
                        $pairCount++; $both += $intersection.Count; $bOnly += ($bSet.Count-$intersection.Count); $cOnly += ($cSet.Count-$intersection.Count); $union += $u.Count
                        $jaccardSum += $(if($u.Count){$intersection.Count/$u.Count}else{1.0})
                        $bOccurrences += $bs.Count; $cOccurrences += $cs.Count
                        foreach($candidateId in $bs){ if(-not $bFirst.ContainsKey($candidateId)){$bFirst[$candidateId]=$cpIndex} }
                        foreach($candidateId in $cs){ if(-not $cFirst.ContainsKey($candidateId)){$cFirst[$candidateId]=$cpIndex} }
                        $bDue += [int]$br.DueBeforeObservation; $cDue += [int]$cr.DueBeforeObservation
                        $bSelected += [int]$br.SelectedCount; $cSelected += [int]$cr.SelectedCount
                        if ([int]$br.SelectedCount -ge 5) { $bCap++ }; if ([int]$cr.SelectedCount -ge 5) { $cCap++ }
                        if(-not $data.private) {
                            $checkpointMetrics.Add([ordered]@{
                                repository=$id.Substring('carlrabbit/'.Length); checkpointIndex=$cpIndex
                                observation=$arm; seed=$seed; rule=$ruleId; identityKind=$kind
                                intersectionOccurrences=$intersection.Count
                                bOnlyOccurrences=$bSet.Count-$intersection.Count; cOnlyOccurrences=$cSet.Count-$intersection.Count
                                unionOccurrences=$u.Count; jaccard=$(if($u.Count){[Math]::Round($intersection.Count/$u.Count,6)}else{1.0})
                                bSelectedOccurrences=$bs.Count; cSelectedOccurrences=$cs.Count
                                bDueEvents=[int]$br.DueBeforeObservation; cDueEvents=[int]$cr.DueBeforeObservation
                                bSelectedTickets=[int]$br.SelectedCount; cSelectedTickets=[int]$cr.SelectedCount
                                bCapSaturated=([int]$br.SelectedCount -ge 5); cCapSaturated=([int]$cr.SelectedCount -ge 5); selectionCap=5
                            })
                        }
                }
                if($pairCount -eq 0){ continue }
                $same=0; $bEarlier=0; $cEarlier=0; $onlyB=$bFirst.Count; $onlyC=$cFirst.Count; $shared=0; $advance=[System.Collections.Generic.List[int]]::new()
                foreach($candidateId in $bFirst.Keys){ if($cFirst.ContainsKey($candidateId)){ $shared++; $onlyB--; $onlyC--; $delta=$cFirst[$candidateId]-$bFirst[$candidateId]; $advance.Add($delta); if($delta -gt 0){$bEarlier++}elseif($delta -lt 0){$cEarlier++}else{$same++} } }
                $repoName=if($data.private){'Private'}else{$id.Substring('carlrabbit/'.Length)}
                $arms.Add([ordered]@{
                    repository=$repoName; observation=$arm; seed=$seed; rule=$ruleId; identityKind=$kind; pairedCheckpointCount=$pairCount
                    overlapOccurrences=$both; bOnlyOccurrences=$bOnly; cOnlyOccurrences=$cOnly; unionOccurrences=$union
                    meanJaccard=[Math]::Round($jaccardSum/$pairCount,6); bSelectedOccurrences=$bOccurrences; cSelectedOccurrences=$cOccurrences
                    bUniqueIdentities=$bFirst.Count; cUniqueIdentities=$cFirst.Count; sharedIdentities=$shared
                    firstSelectionBOnly=$onlyB; firstSelectionCOnly=$onlyC; firstSelectionBEarlier=$bEarlier; firstSelectionCEarlier=$cEarlier; firstSelectionSame=$same
                    medianAdvanceCheckpoints=if($advance.Count){[Math]::Round([double](($advance | Sort-Object)[[Math]::Floor(($advance.Count-1)/2)]),2)}else{$null}
                    bDueEvents=$bDue; cDueEvents=$cDue; bSelectedTickets=$bSelected; cSelectedTickets=$cSelected
                    bCapSaturatedPairs=$bCap; cCapSaturatedPairs=$cCap; capSaturationDenominator=$pairCount
                })
              }
            }
        }
    }
    if (-not $data.private) { $repoMetrics.AddRange([object[]]$arms.ToArray()) }
    else {
        foreach ($group in @($arms | Group-Object { "$($_.observation)|$($_.rule)|$($_.identityKind)" })) {
            $rows=@($group.Group)
            $repoMetrics.Add([ordered]@{
                repository='Private'; observation=$rows[0].observation; rule=$rows[0].rule; identityKind=$rows[0].identityKind; seedCount=$rows.Count; pairedCheckpointsPerSeed=$rows[0].pairedCheckpointCount
                meanJaccardBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object meanJaccard)))
                overlapOccurrencesBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object overlapOccurrences)))
                bOnlyOccurrencesBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object bOnlyOccurrences)))
                cOnlyOccurrencesBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object cOnlyOccurrences)))
                bDueEventsBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object bDueEvents)))
                cDueEventsBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object cDueEvents)))
                bSelectedTicketsBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object bSelectedTickets)))
                cSelectedTicketsBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object cSelectedTickets)))
                bFirstEarlierBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object firstSelectionBEarlier)))
                cFirstEarlierBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object firstSelectionCEarlier)))
                bOnlyFirstSelectionBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object firstSelectionBOnly)))
                cOnlyFirstSelectionBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object firstSelectionCOnly)))
                firstSelectionSameBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object firstSelectionSame)))
                medianAdvanceCheckpointsBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object medianAdvanceCheckpoints)))
                bCapSaturationPairsBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object bCapSaturatedPairs)))
                cCapSaturationPairsBySeed=(Get-Distribution ([double[]]@($rows | ForEach-Object cCapSaturatedPairs)))
            })
        }
    }
    $origRows=Get-CanonicalIdentityRows $original; $repeatRows=Get-CanonicalIdentityRows $repeat; $unitRows=Get-CanonicalIdentityRows $unitHistory
    $mismatches=0; $repeatMismatch=0
    $max=[Math]::Max($origRows.Count,$repeatRows.Count)
    for($i=0;$i -lt $max;$i++){ if($i -ge $origRows.Count -or $i -ge $repeatRows.Count -or $origRows[$i] -cne $repeatRows[$i]){$mismatches++} }
    $repeatMax=[Math]::Max($repeatRows.Count,$unitRows.Count)
    for($i=0;$i -lt $repeatMax;$i++){ if($i -ge $repeatRows.Count -or $i -ge $unitRows.Count -or $repeatRows[$i] -cne $unitRows[$i]){$repeatMismatch++} }
    $repeatMetrics.Add([ordered]@{repository=$(if($data.private){'Private'}else{$id.Substring('carlrabbit/'.Length)}); checkpoints=$original.Count; candidateIdentityRowsCompared=$max; mismatchingRows=$mismatches; retainedM0013MatchesM0014=($mismatches -eq 0); retainedVsM0014Hash=$(if($data.private){$null}else{Get-Hash $origRows}); m0014RepeatRowsCompared=$repeatMax; m0014RepeatMismatches=$repeatMismatch; m0014IndependentRepeatsMatch=($repeatMismatch -eq 0); m0014FirstRepeatHash=$(if($data.private){$null}else{Get-Hash $repeatRows}); m0014UnitReplayHash=$(if($data.private){$null}else{Get-Hash $unitRows})})
}

$report = [ordered]@{
    schemaVersion=1; experiment='M0014'; inputProtocol='Frozen M0013 checkpoints and seeds; exact emitted identity; B:C paired by repository/checkpoint/seed/arm/rule. Population-unit identities were added to the second B/C-only replay; Private rows aggregate-only.'
    sourceCheckpointCounts=$sourceCounts.ToArray()
    populationUnitIdCoverage=$populationUnitCoverage.ToArray()
    checkpointMetricsArtifact=[IO.Path]::GetFileName($CheckpointOutputPath); checkpointMetricRows=$checkpointMetrics.Count
    metrics=$repoMetrics.ToArray(); repeatability=$repeatMetrics.ToArray()
}
if ([IO.Path]::IsPathRooted($OutputPath)) { $resolvedOutput=[IO.Path]::GetFullPath($OutputPath) }
else { $resolvedOutput=[IO.Path]::GetFullPath((Join-Path $repoRoot $OutputPath)) }
if ([IO.Path]::IsPathRooted($CheckpointOutputPath)) { $resolvedCheckpointOutput=[IO.Path]::GetFullPath($CheckpointOutputPath) }
else { $resolvedCheckpointOutput=[IO.Path]::GetFullPath((Join-Path $repoRoot $CheckpointOutputPath)) }
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resolvedOutput)) | Out-Null
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resolvedCheckpointOutput)) | Out-Null
[IO.File]::WriteAllText($resolvedOutput,($report | ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))
if($checkpointMetrics.Count) {
    $checkpointLines=@($checkpointMetrics | ForEach-Object { ConvertTo-Json -InputObject $_ -Depth 8 -Compress })
    [IO.File]::WriteAllText($resolvedCheckpointOutput,(($checkpointLines -join "`n")+"`n"),[Text.UTF8Encoding]::new($false))
}
Write-Output "M0014 paired identity analysis written to $resolvedOutput"
Write-Output "Public checkpoint metrics written to $resolvedCheckpointOutput"
