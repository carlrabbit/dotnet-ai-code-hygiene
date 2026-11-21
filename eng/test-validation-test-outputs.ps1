$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ValidationStages.ps1')
Set-Location $script:ValidationRoot
$lockPath = Join-Path $script:ReceiptRoot 'execution.lock'
try { $lock = [IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None) }
catch { throw 'Another validation command is active; cannot run input-fingerprint regression.' }
try {
$baseline = @(Assert-CompleteValidation)
if ($baseline.Count -ne $script:ValidationStages.Count) { throw 'Regression requires complete current receipts.' }
$hygiene = Join-Path $script:ValidationRoot 'src/DotNetAiCodeHygiene.Core/ProfileManager.cs'
$auto = Join-Path $script:ValidationRoot 'src/DotNetAiWorkbench.Autoformat.Core/AutoformatPolicy.cs'
foreach ($probe in @(@{path=$hygiene; invalid=@('hygiene-tool','self-host'); valid=@('autoformat-tool')}, @{path=$auto; invalid=@('autoformat-tool','hygiene-tool','self-host'); valid=@()})) {
    $original = [IO.File]::ReadAllBytes($probe.path)
    try {
        [IO.File]::WriteAllBytes($probe.path, [byte[]]($original + [byte[]]@(10)))
        foreach ($id in $probe.invalid) { if (Test-StageReceipt ($script:ValidationStages | Where-Object Id -eq $id)) { throw "Input mutation did not invalidate '$id'." } }
        foreach ($id in $probe.valid) { if (-not (Test-StageReceipt ($script:ValidationStages | Where-Object Id -eq $id))) { throw "Unrelated input mutation invalidated '$id'." } }
    } finally { [IO.File]::WriteAllBytes($probe.path, $original) }
    if (@(Assert-CompleteValidation).Count -ne $script:ValidationStages.Count) { throw "Restoring $($probe.path) did not restore current receipts." }
}
Write-Output 'PASS hygiene-only changes preserve Autoformat receipts; shared Autoformat engine changes invalidate both tool gates.'
} finally { $lock.Dispose() }
