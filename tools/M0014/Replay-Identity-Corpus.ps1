param(
    [Parameter(Mandatory = $true)][string]$PrivateRepository,
    [string]$PrivateEvidenceRoot = (Join-Path $env:LOCALAPPDATA 'M0013-private'),
    [string]$RepeatEvidenceRoot = (Join-Path $env:LOCALAPPDATA 'M0014-identity-repeat'),
    [string[]]$RepositoryId = @()
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$publicManifest = Get-Content (Join-Path $repoRoot 'docs/research/ACTIVITY-AGE-CORPUS.json') -Raw | ConvertFrom-Json
$privateManifest = Get-Content (Join-Path $PrivateEvidenceRoot 'private-corpus.json') -Raw | ConvertFrom-Json
$replayDll = Join-Path $repoRoot 'tools/M0013/bin/Release/net11.0/DotNetAiCodeHygiene.Core.Tests.dll'
if (-not (Test-Path $replayDll)) { throw 'Build the unchanged helper with .\eng\M0013.ps1 -Action build-replay first.' }
if (-not (Test-Path (Join-Path $PrivateRepository '.git'))) { throw 'The authorized Private upstream checkout is unavailable.' }
New-Item -ItemType Directory -Force -Path $RepeatEvidenceRoot | Out-Null
$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
& icacls.exe $RepeatEvidenceRoot /inheritance:r /grant:r "${identity}:(OI)(CI)F" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict repeat evidence to the current user.' }

$repoPaths = @{
    'carlrabbit/Private' = $PrivateRepository
    'carlrabbit/dotnet-semantic-type-model' = 'C:\src\dotnet-semantic-type-model'
    'carlrabbit/dotnet-ai-code-hygiene' = 'C:\src\dotnet-ai-code-hygiene'
    'carlrabbit/dotnet-ai-first-2d-game-engine' = 'C:\src\dotnet-ai-first-2d-game-engine'
}
$repoIds = @($publicManifest.repositories | ForEach-Object id)
if ($RepositoryId.Count) {
    $unknown = @($RepositoryId | Where-Object { $_ -notin $repoIds })
    if ($unknown.Count) { throw "Unknown repository id(s): $($unknown -join ', ')." }
    $repoIds = @($repoIds | Where-Object { $_ -in $RepositoryId })
}
$publicTemp = Join-Path $RepeatEvidenceRoot 'public'
New-Item -ItemType Directory -Force -Path $publicTemp | Out-Null
$env:GIT_CONFIG_COUNT = '1'; $env:GIT_CONFIG_KEY_0 = 'core.longpaths'; $env:GIT_CONFIG_VALUE_0 = 'true'
$started = [DateTimeOffset]::UtcNow
$done = 0
foreach ($repoId in $repoIds) {
    $isPrivate = $repoId -eq 'carlrabbit/Private'
    $record = $publicManifest.repositories | Where-Object id -eq $repoId
    $history = if ($isPrivate) { $privateManifest.checkpoints } else { $record.history }
    $seeds = if ($isPrivate) { @($privateManifest.seeds) } else { @($record.seeds) }
    $sourceRoot = $repoPaths[$repoId]
    if (-not (Test-Path (Join-Path $sourceRoot '.git'))) { throw "Frozen checkout unavailable for a corpus member." }
    $token = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($repoId))).Substring(0,10).ToLowerInvariant()
    $repoRootOut = Join-Path $RepeatEvidenceRoot $(if ($isPrivate) { 'private-' + $token } else { 'public-' + $token })
    $worktree = Join-Path $repoRootOut 'worktree'
    $stateRoot = Join-Path $repoRootOut 'state'
    $rawRoot = Join-Path $repoRootOut 'raw'
    New-Item -ItemType Directory -Force -Path $repoRootOut,$stateRoot,$rawRoot | Out-Null
    if (Test-Path $worktree) { throw 'A repeat worktree already exists; remove it after verifying its location.' }
    $null = git -C $sourceRoot worktree add --detach $worktree $history.checkpoints[0].sha
    if ($LASTEXITCODE) { throw 'Could not create a frozen repeat worktree.' }
    try {
        $variants = [System.Collections.Generic.List[object]]::new()
        foreach ($policy in @('B','C')) { foreach ($observation in @('all','none')) { for ($i=0; $i -lt $seeds.Count; $i++) {
            $seedLabel = 'seed-{0:d2}' -f ($i + 1)
            $variants.Add([ordered]@{ id="$policy|$observation|$seedLabel"; policy=$policy; observation=$observation; seed=$seeds[$i]; statePath=(Join-Path $stateRoot "$policy-$observation-$seedLabel.json") })
        } } }
        $variantsPath = Join-Path $stateRoot 'variants.json'
        [IO.File]::WriteAllText($variantsPath, ($variants | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
        $idx = 0
        foreach ($checkpoint in $history.checkpoints) {
            $null = git -C $worktree checkout --detach $checkpoint.sha
            if ($LASTEXITCODE) { throw 'Could not select frozen checkpoint.' }
            $null = git -C $worktree clean -fdx
            if ($LASTEXITCODE) { throw 'Could not clean frozen worktree.' }
            $timestamp = if ($checkpoint.committerTimestamp -is [DateTime]) {
                [DateTimeOffset]::new($checkpoint.committerTimestamp.ToUniversalTime()).ToUnixTimeMilliseconds()
            } else {
                [DateTimeOffset]::Parse([string]$checkpoint.committerTimestamp).ToUnixTimeMilliseconds()
            }
            $configPath = Join-Path $stateRoot 'checkpoint.json'
            $outputPath = Join-Path $rawRoot ("{0:D4}.json" -f $idx)
            $config = [ordered]@{ root=$worktree; repositoryId=$repoId; commit=$checkpoint.sha; timestampUnixMilliseconds=$timestamp; variantsPath=$variantsPath; outputPath=$outputPath }
            [IO.File]::WriteAllText($configPath, ($config | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
            & dotnet $replayDll --config $configPath
            if ($LASTEXITCODE) { throw "Identity replay failed at local checkpoint index $idx." }
            $idx++; $done++
            if ($isPrivate) { Write-Output ("Completed Private checkpoint {0}; {1} total." -f $idx,$done) }
            else { Write-Output ("Completed public checkpoint {0} of {1}; {2} total." -f $idx,$history.checkpoints.Count,$done) }
        }
    }
    finally { $null = git -C $sourceRoot worktree remove --force $worktree }
}
$summary = [ordered]@{ schemaVersion=1; experiment='M0014-identity-repeat'; policies=@('B','C'); observations=@('all','none'); completedCheckpoints=$done; elapsedMinutes=[Math]::Round(([DateTimeOffset]::UtcNow-$started).TotalMinutes,2); rawEvidenceLocation='Local user-restricted evidence store; never commit raw traces.' }
[IO.File]::WriteAllText((Join-Path $RepeatEvidenceRoot 'summary.json'),($summary | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Write-Output ("Completed B/C candidate-identity repeat: {0} checkpoints in {1} minutes." -f $done,$summary.elapsedMinutes)
