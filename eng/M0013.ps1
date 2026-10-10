param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('freeze', 'inventory', 'build-replay', 'replay', 'replay-corpus', 'analyze')]
    [string]$Action,
    [string]$ConfigPath,
    [string]$BaselinePath,
    [string]$CurrentPath,
    [string]$OutputPath,
    [string]$PrivateRepository,
    [string]$PrivateEvidenceRoot,
    [string]$PrivateWorktreeRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repoRoot

switch ($Action) {
    'freeze' {
        $privateArguments = @{}
        if ($PrivateRepository) { $privateArguments.PrivateRepository = $PrivateRepository }
        if ($PrivateEvidenceRoot) { $privateArguments.PrivateEvidenceRoot = $PrivateEvidenceRoot }
        & (Join-Path $repoRoot 'tools/M0013/Freeze-Corpus.ps1') @privateArguments
        if ($LASTEXITCODE -notin 0, $null) { throw "Corpus freeze failed with exit code $LASTEXITCODE." }
    }
    'inventory' {
        $privateArguments = @{}
        if ($PrivateRepository) { $privateArguments.PrivateRepository = $PrivateRepository }
        if ($PrivateEvidenceRoot) { $privateArguments.PrivateEvidenceRoot = $PrivateEvidenceRoot }
        if ($PrivateWorktreeRoot) { $privateArguments.PrivateWorktreeRoot = $PrivateWorktreeRoot }
        & (Join-Path $repoRoot 'tools/M0013/Inventory-Eligibility.ps1') @privateArguments
        if ($LASTEXITCODE -notin 0, $null) { throw "Eligibility inventory failed with exit code $LASTEXITCODE." }
    }
    'build-replay' {
        dotnet restore .\tools\M0013\Replay.csproj --ignore-failed-sources
        if ($LASTEXITCODE -ne 0) { throw "Replay helper restore failed with exit code $LASTEXITCODE." }
        dotnet build .\tools\M0013\Replay.csproj --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Replay helper build failed with exit code $LASTEXITCODE." }
    }
    'replay' {
        if ([string]::IsNullOrWhiteSpace($ConfigPath)) { throw 'Pass -ConfigPath for one frozen checkpoint replay.' }
        $exe = Join-Path $repoRoot 'tools/M0013/bin/Release/net11.0/DotNetAiCodeHygiene.Core.Tests.dll'
        if (-not (Test-Path $exe)) { throw 'Build the experiment replay helper first with -Action build-replay.' }
        dotnet $exe --config (Resolve-Path $ConfigPath).Path
        if ($LASTEXITCODE -ne 0) { throw "Checkpoint replay failed with exit code $LASTEXITCODE." }
    }
    'replay-corpus' {
        $privateArguments = @{}
        if ($PrivateRepository) { $privateArguments.PrivateRepository = $PrivateRepository }
        if ($PrivateEvidenceRoot) { $privateArguments.PrivateEvidenceRoot = $PrivateEvidenceRoot }
        if ($PrivateWorktreeRoot) { $privateArguments.PrivateWorktreeRoot = $PrivateWorktreeRoot }
        & (Join-Path $repoRoot 'tools/M0013/Replay-Corpus.ps1') @privateArguments
        if ($LASTEXITCODE -notin 0, $null) { throw "Corpus replay failed with exit code $LASTEXITCODE." }
    }
    'analyze' {
        $analysisArguments = @{}
        if ($BaselinePath) { $analysisArguments.BaselinePath = $BaselinePath }
        if ($CurrentPath) { $analysisArguments.CurrentPath = $CurrentPath }
        if ($OutputPath) { $analysisArguments.OutputPath = $OutputPath }
        & (Join-Path $repoRoot 'tools/M0013/Analyze-Replay.ps1') @analysisArguments
        if ($LASTEXITCODE -notin 0, $null) { throw "Replay analysis failed with exit code $LASTEXITCODE." }
    }
}
