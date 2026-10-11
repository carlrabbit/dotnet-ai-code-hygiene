function Assert-M0014TraceVariantCoverage {
    param(
        [Parameter(Mandatory)]$Checkpoint,
        [Parameter(Mandatory)][string[]]$Policies,
        [Parameter(Mandatory)][string]$TraceKind
    )

    $expected=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($policy in $Policies) {
        foreach($arm in @('all','none')) {
            foreach($seed in @('seed-01','seed-02','seed-03','seed-04','seed-05')) {
                $null=$expected.Add("$policy|$arm|$seed")
            }
        }
    }

    $actual=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($variant in @($Checkpoint.results)) {
        $key="$($variant.Policy)|$($variant.Observation)|$($variant.SeedId)"
        if(-not $actual.Add($key)) { throw "Duplicate policy/arm/seed variant in $TraceKind trace." }
    }

    if($actual.Count -ne $expected.Count) { throw "Incomplete policy/arm/seed variant set in $TraceKind trace." }
    foreach($key in $expected) {
        if(-not $actual.Contains($key)) { throw "Missing policy/arm/seed variant in $TraceKind trace." }
    }
}
