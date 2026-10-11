param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [Parameter(Mandatory = $true)]
    [string]$PrivateRepository,
    [string]$PrivateEvidenceRoot = (Join-Path $env:LOCALAPPDATA 'M0013-private'),
    [string]$PrivateWorktreeRoot = (Join-Path $env:LOCALAPPDATA 'Temp\M0013-private-worktrees'),
    [string[]]$RepositoryId = @(),
    [int]$WallClockLimitMinutes = 180
)

$ErrorActionPreference = 'Stop'
$manifest = Get-Content (Join-Path $RepositoryRoot 'docs/research/ACTIVITY-AGE-CORPUS.json') -Raw | ConvertFrom-Json
$replayDll = Join-Path $RepositoryRoot 'tools/M0013/bin/Release/net11.0/DotNetAiCodeHygiene.Core.Tests.dll'
if (-not (Test-Path $replayDll)) { throw 'Build the replay helper with .\eng\M0013.ps1 -Action build-replay first.' }
$tempRoot = Join-Path $env:LOCALAPPDATA 'Temp\m0013-replay'
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
New-Item -ItemType Directory -Force -Path $PrivateWorktreeRoot | Out-Null
$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
& icacls.exe $PrivateWorktreeRoot /inheritance:r /grant:r "${identity}:(OI)(CI)F" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict the replay worktree root to the current user.' }
$env:GIT_CONFIG_COUNT = '1'; $env:GIT_CONFIG_KEY_0 = 'core.longpaths'; $env:GIT_CONFIG_VALUE_0 = 'true'

$privateManifestPath = Join-Path $PrivateEvidenceRoot 'private-corpus.json'
$privateManifest = Get-Content $privateManifestPath -Raw | ConvertFrom-Json
$repoPaths = @{
    'carlrabbit/Private' = $PrivateRepository
    'carlrabbit/dotnet-semantic-type-model' = 'C:\src\dotnet-semantic-type-model'
    'carlrabbit/dotnet-ai-code-hygiene' = 'C:\src\dotnet-ai-code-hygiene'
    'carlrabbit/dotnet-ai-first-2d-game-engine' = 'C:\src\dotnet-ai-first-2d-game-engine'
}
$started = [DateTimeOffset]::UtcNow
$rows = [System.Collections.Generic.List[object]]::new()
$completedCheckpoints = 0
$stopReason = $null
$repositoryOrder = @($manifest.repositories | ForEach-Object id)
if ($RepositoryId.Count -gt 0) {
    $unknown = @($RepositoryId | Where-Object { $_ -notin $repositoryOrder })
    if ($unknown.Count -gt 0) { throw "Unknown repository id(s): $($unknown -join ', ')." }
    $repositoryOrder = @($repositoryOrder | Where-Object { $_ -in $RepositoryId })
}

try {
    foreach ($repoId in $repositoryOrder) {
        $private = $repoId -eq 'carlrabbit/Private'
        $repoRecord = $manifest.repositories | Where-Object id -eq $repoId
        if ($private) { $history = $privateManifest.checkpoints; $seeds = @($privateManifest.seeds) }
        else { $history = $repoRecord.history; $seeds = @($repoRecord.seeds) }
        $sourceRoot = $repoPaths[$repoId]
        if (-not (Test-Path (Join-Path $sourceRoot '.git'))) { throw "Frozen repository checkout is unavailable: $repoId." }
        $repoToken = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($repoId))).Substring(0, 10).ToLowerInvariant()
        $worktree = Join-Path $(if ($private) { $PrivateWorktreeRoot } else { $tempRoot }) $repoToken
        if (Test-Path $worktree) { throw "Replay worktree path already exists: $worktree." }
        git -C $sourceRoot worktree add --detach $worktree $history.checkpoints[0].sha | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Could not create frozen replay worktree for $repoId." }
        try {
            $stateRoot = Join-Path $(if ($private) { $PrivateEvidenceRoot } else { $tempRoot }) "state-$repoToken"
            New-Item -ItemType Directory -Force -Path $stateRoot | Out-Null
            $variants = [System.Collections.Generic.List[object]]::new()
            foreach ($policy in @('A','B','C')) {
                foreach ($observation in @('all','none')) {
                    for ($seedIndex = 0; $seedIndex -lt $seeds.Count; $seedIndex++) {
                        $seedLabel = 'seed-{0:d2}' -f ($seedIndex + 1)
                        $statePath = Join-Path $stateRoot "$policy-$observation-$seedLabel.json"
                        $variants.Add([ordered]@{ id = "$policy|$observation|$seedLabel"; policy = $policy; observation = $observation; seed = $seeds[$seedIndex]; statePath = $statePath })
                    }
                }
            }
            foreach ($variant in $variants) {
                if (Test-Path -LiteralPath $variant.statePath) { Remove-Item -LiteralPath $variant.statePath -Force }
            }
            $variantsPath = Join-Path $stateRoot 'variants.json'
            [IO.File]::WriteAllText($variantsPath, ($variants | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
            foreach ($checkpoint in $history.checkpoints) {
                if (([DateTimeOffset]::UtcNow - $started).TotalMinutes -ge $WallClockLimitMinutes) {
                    $stopReason = "Pre-registered $WallClockLimitMinutes-minute replay ceiling reached."
                    break
                }
                git -C $worktree checkout --detach $checkpoint.sha | Out-Null
                if ($LASTEXITCODE -ne 0) { throw "Could not checkout a frozen checkpoint for $repoId." }
                git -C $worktree clean -fdx | Out-Null
                if ($LASTEXITCODE -ne 0) { throw "Could not clean replay worktree for $repoId." }
                $rawRoot = Join-Path $(if ($private) { $PrivateEvidenceRoot } else { $tempRoot }) "raw-$repoToken"
                New-Item -ItemType Directory -Force -Path $rawRoot | Out-Null
                $configPath = Join-Path $stateRoot 'checkpoint.json'
                $outputPath = Join-Path $rawRoot ("{0:D4}.json" -f $completedCheckpoints)
                $timestamp = if ($checkpoint.committerTimestamp -is [DateTime]) {
                    [DateTimeOffset]::new($checkpoint.committerTimestamp.ToUniversalTime()).ToUnixTimeMilliseconds()
                } else {
                    [DateTimeOffset]::Parse([string]$checkpoint.committerTimestamp).ToUnixTimeMilliseconds()
                }
                $config = [ordered]@{ root = $worktree; repositoryId = $repoId; commit = $checkpoint.sha; timestampUnixMilliseconds = $timestamp; variantsPath = $variantsPath; outputPath = $outputPath }
                [IO.File]::WriteAllText($configPath, ($config | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
                $clock = [Diagnostics.Stopwatch]::StartNew()
                & dotnet $replayDll --config $configPath
                if ($LASTEXITCODE -ne 0) { throw "Replay process failed for $repoId checkpoint $($checkpoint.firstParentIndex)." }
                $clock.Stop()
                $raw = Get-Content $outputPath -Raw | ConvertFrom-Json
                $rows.Add([ordered]@{
                    repositoryId = $repoId
                    visibility = $(if ($private) { 'private' } else { 'public' })
                    firstParentIndex = $checkpoint.firstParentIndex
                    commit = $(if ($private) { $null } else { $checkpoint.sha })
                    timestampUnixMilliseconds = $(if ($private) { $null } else { $timestamp })
                    measuredCheckpointMilliseconds = $clock.ElapsedMilliseconds
                    workspaceSetupMilliseconds = $raw.workspaceSetupMilliseconds
                    variants = @($raw.results | ForEach-Object {
                        [ordered]@{
                            policy = $_.Policy; observation = $_.Observation; seedId = $_.SeedId
                            firstRunMilliseconds = $_.FirstRunMilliseconds; repeatRunMilliseconds = $_.RepeatRunMilliseconds
                            samplingStateBytes = $_.SamplingStateBytes; eligibleProjects = $_.EligibleProjects
                            eligibleSourceFiles = $_.EligibleSourceFiles; eligibleSourceLines = $_.EligibleSourceLines
                            projectAgeChangeCount = $_.ProjectAgeChangeCount; projectAgeDeltaUnits = $_.ProjectAgeDeltaUnits
                            repeatProjectAgeDeltaUnits = $_.RepeatProjectAgeDeltaUnits
                            rules = @($_.Rules | ForEach-Object {
                                [ordered]@{ ruleId = $_.RuleId; populationCount = $_.PopulationCount; dueBeforeObservation = $_.DueBeforeObservation; selectedCount = $_.SelectedCount; acceptedCount = $_.AcceptedCount; dueAfterObservation = $_.DueAfterObservation }
                            })
                        }
                    })
                })
                $completedCheckpoints++
                $publicProgress = [ordered]@{ lastRepository = $repoId; completedCheckpoints = $completedCheckpoints; elapsedMinutes = [Math]::Round(([DateTimeOffset]::UtcNow - $started).TotalMinutes, 2); lastCheckpointMilliseconds = $clock.ElapsedMilliseconds }
                if ($private) {
                    [IO.File]::WriteAllText((Join-Path $PrivateEvidenceRoot 'replay-progress.json'), ($publicProgress | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
                }
                Write-Output ("Completed {0} checkpoint {1}; {2} total." -f $repoId, $checkpoint.firstParentIndex, $completedCheckpoints)
            }
        }
        finally {
            git -C $sourceRoot worktree remove --force $worktree | Out-Null
        }
        if ($stopReason) { break }
    }
}
finally {
    $record = [ordered]@{
        schemaVersion = 1; experiment = 'M0013'; frozenAtUtc = $manifest.frozenAtUtc
        runStartedAtUtc = $started.ToString('o'); runFinishedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        environment = $manifest.environment; fixedSeedCount = 5; policyArms = @('A','B','C')
        syntheticObservationArms = @('all','none'); ceilingMinutes = $WallClockLimitMinutes
        completedCheckpointCount = $completedCheckpoints; stopReason = $stopReason
        replayCases = $rows
        privateDataRedaction = 'Private exact revisions, timestamps, candidate data, and paths are omitted. The local ACL-restricted raw run is retained outside the repository.'
    }
    $target = Join-Path $RepositoryRoot 'docs/research/ACTIVITY-AGE-REPLAY.json'
    [IO.File]::WriteAllText($target, ($record | ConvertTo-Json -Depth 14), [Text.UTF8Encoding]::new($false))
    $privateSummary = [ordered]@{ completedCheckpointCount = @($rows | Where-Object visibility -eq 'private').Count; stopReason = $stopReason; elapsedMinutes = [Math]::Round(([DateTimeOffset]::UtcNow - $started).TotalMinutes, 2) }
    [IO.File]::WriteAllText((Join-Path $PrivateEvidenceRoot 'replay-summary.json'), ($privateSummary | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
}

if ($stopReason) { Write-Warning $stopReason } else { Write-Output "Completed replay checkpoints: $completedCheckpoints" }
