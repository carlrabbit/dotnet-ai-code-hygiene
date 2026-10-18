$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ValidationStages.ps1')
Set-Location $script:ValidationRoot

$lockPath = Join-Path $script:ReceiptRoot 'execution.lock'
try { $lock = [IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None) }
catch { throw 'Another validation command is active; cannot run test-output regression.' }

try {
    $baseline = @(Assert-CompleteValidation)
    if ($baseline.Count -ne $script:ValidationStages.Count) { throw 'Regression requires a complete, current baseline receipt set.' }

    $target = Join-Path $script:ValidationRoot 'tests/DotNetAiCodeHygiene.Core.Tests/bin/Release/net11.0/DotNetAiCodeHygiene.Core.Tests.dll'
    $original = [IO.File]::ReadAllBytes($target)
    $affectedStages = @('solution','core-tests','cli-tests','pack-current','installed-consumers','self-host')
    try {
        $changed = [byte[]]$original.Clone()
        $changed[0] = $changed[0] -bxor 1
        [IO.File]::WriteAllBytes($target,$changed)
        foreach ($id in $affectedStages) {
            $stage = $script:ValidationStages | Where-Object Id -eq $id
            if (Test-StageReceipt $stage) { throw "Changed Core test assembly did not invalidate '$id'." }
        }
        Write-Output 'PASS changed compiled Core test assembly invalidates its build receipt and dependent stages.'

        [IO.File]::WriteAllBytes($target,$original)
        if (-not (Test-StageReceipt ($script:ValidationStages | Where-Object Id -eq 'solution'))) { throw 'Restored Core test assembly did not restore its solution receipt.' }

        Remove-Item -LiteralPath $target -Force
        foreach ($id in $affectedStages) {
            $stage = $script:ValidationStages | Where-Object Id -eq $id
            if (Test-StageReceipt $stage) { throw "Missing Core test assembly did not invalidate '$id'." }
        }
        Write-Output 'PASS missing compiled Core test assembly invalidates its build receipt and dependent stages.'
    }
    finally { [IO.File]::WriteAllBytes($target,$original) }

    $restored = @(Assert-CompleteValidation)
    if ($restored.Count -ne $script:ValidationStages.Count) { throw 'Restored test assembly did not restore complete aggregate evidence.' }
    Write-Output 'PASS restored test assembly and receipts verify after the binary probes.'
}
finally { $lock.Dispose() }
