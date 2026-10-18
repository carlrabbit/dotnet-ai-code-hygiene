$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ValidationStages.ps1')

Set-Location $script:ValidationRoot
$lockPath = Join-Path $script:ReceiptRoot 'execution.lock'
try { $lock = [IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None) }
catch { throw 'Another validation command is active; cannot run receipt-verification regression.' }

try {
    $baseline = @(Assert-CompleteValidation)
    if ($baseline.Count -ne $script:ValidationStages.Count) { throw 'Regression requires a complete, current baseline receipt set.' }

    $target = Get-StageReceiptPath 'core-tests'
    $original = [IO.File]::ReadAllBytes($target)
    try {
        [IO.File]::WriteAllText($target,'{"schemaVersion":1,"id":"core-tests","status":"passed","fingerprint":"deliberately-stale"}')
        $rejected = $false
        try { $null = @(Assert-CompleteValidation) }
        catch {
            if ($_.Exception.Message -notmatch 'incomplete or stale: core-tests') { throw }
            $rejected = $true
        }
        if (-not $rejected) { throw 'Final aggregate verification accepted a stale Core receipt.' }
        Write-Output 'PASS final aggregate verification rejects altered stage evidence.'
    }
    finally { [IO.File]::WriteAllBytes($target,$original) }

    $restored = @(Assert-CompleteValidation)
    if ($restored.Count -ne $script:ValidationStages.Count) { throw 'Restored receipts did not pass final aggregate verification.' }
    Write-Output 'PASS restored aggregate receipts verify after the regression probe.'
}
finally { $lock.Dispose() }
