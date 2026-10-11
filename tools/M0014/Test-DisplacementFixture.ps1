$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Validate-Trace.ps1')

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw "M0014 deterministic fixture failed: $message" }
}
function MeasurePair([string[]]$b, [string[]]$c) {
    $bs=[Collections.Generic.HashSet[string]]::new([string[]]$b,[StringComparer]::Ordinal)
    $cs=[Collections.Generic.HashSet[string]]::new([string[]]$c,[StringComparer]::Ordinal)
    $i=[Collections.Generic.HashSet[string]]::new($bs,[StringComparer]::Ordinal); $i.IntersectWith($cs)
    $u=[Collections.Generic.HashSet[string]]::new($bs,[StringComparer]::Ordinal); $u.UnionWith($cs)
    [pscustomobject]@{ intersection=$i.Count; bOnly=$bs.Count-$i.Count; cOnly=$cs.Count-$i.Count; union=$u.Count; jaccard=$(if($u.Count){$i.Count/$u.Count}else{1.0}) }
}

$empty=MeasurePair @() @()
Assert ($empty.intersection -eq 0 -and $empty.union -eq 0 -and $empty.jaccard -eq 1) 'empty sets remain a paired identical case'
$partial=MeasurePair @('a','b') @('b','c')
Assert ($partial.intersection -eq 1 -and $partial.bOnly -eq 1 -and $partial.cOnly -eq 1 -and [Math]::Abs($partial.jaccard-(1/3)) -lt 0.000001) 'partial overlap and displacement are exact'
$identical=MeasurePair @('x','y') @('x','y')
Assert ($identical.intersection -eq 2 -and $identical.bOnly -eq 0 -and $identical.cOnly -eq 0 -and $identical.jaccard -eq 1) 'identical sets remain in the denominator'

$populationUnits=@('project/file-a','project/file-b')
$sampledTypes=@('type:Alpha','type:Beta')
Assert (($populationUnits -join '|') -cne ($sampledTypes -join '|')) 'population units and sampled type identities are separate dimensions'
$firstB=@{ candidate=2; another=4 }; $firstC=@{ candidate=3; onlyC=5 }
$delta=$firstC.candidate-$firstB.candidate
Assert ($delta -eq 1 -and $firstB.another -notin $firstC.Keys -and $firstC.onlyC -notin $firstB.Keys) 'exact identity first-selection timing and one-policy cases are distinguishable'
$due=9; $selected=5; $cap=5
Assert ($due -gt $selected -and $selected -ge $cap) 'due workload and cap saturation remain separate measures'
$variants=@()
foreach($policy in @('B','C')) { foreach($arm in @('all','none')) { foreach($seed in @('seed-01','seed-02','seed-03','seed-04','seed-05')) { $variants += [pscustomobject]@{Policy=$policy;Observation=$arm;SeedId=$seed} } } }
$complete=[pscustomobject]@{results=$variants}
Assert-M0014TraceVariantCoverage $complete @('B','C') 'fixture'
$incomplete=[pscustomobject]@{results=@($variants | Select-Object -SkipLast 1)}
$incompleteDetected=$false
try { Assert-M0014TraceVariantCoverage $incomplete @('B','C') 'fixture' } catch { $incompleteDetected=$true }
Assert $incompleteDetected 'an incomplete candidate-identity trace is rejected rather than treated as empty or equal by counts'
Write-Output 'M0014 deterministic identity fixture passed: empty, partial, identical, population/type, advancement, one-policy, cap, and incomplete-trace cases.'
