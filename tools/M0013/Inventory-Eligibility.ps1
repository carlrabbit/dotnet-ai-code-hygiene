param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [Parameter(Mandatory = $true)]
    [string]$PrivateRepository,
    [string]$PrivateEvidenceRoot = (Join-Path $env:LOCALAPPDATA 'M0013-private'),
    [string]$PrivateWorktreeRoot = (Join-Path $env:LOCALAPPDATA 'Temp\M0013-private-worktrees'),
    [string[]]$RepositoryId = @()
)

$ErrorActionPreference = 'Stop'
$manifestPath = Join-Path $RepositoryRoot 'docs/research/ACTIVITY-AGE-CORPUS.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$cli = Join-Path $RepositoryRoot 'src/DotNetAiCodeHygiene.Cli/bin/Release/net11.0/DotNetAiCodeHygiene.Cli.dll'
if (-not (Test-Path $cli)) { throw 'Build the M0012 CLI with the repository canonical build before inventory.' }

$sources = @(
    [ordered]@{ id = 'carlrabbit/Private'; path = $PrivateRepository; sensitive = $true },
    [ordered]@{ id = 'carlrabbit/dotnet-semantic-type-model'; path = 'C:\src\dotnet-semantic-type-model'; sensitive = $false },
    [ordered]@{ id = 'carlrabbit/dotnet-ai-code-hygiene'; path = 'C:\src\dotnet-ai-code-hygiene'; sensitive = $false },
    [ordered]@{ id = 'carlrabbit/dotnet-ai-first-2d-game-engine'; path = 'C:\src\dotnet-ai-first-2d-game-engine'; sensitive = $false }
)
$results = [System.Collections.Generic.List[object]]::new()
$tempRoot = Join-Path $env:LOCALAPPDATA 'Temp\m0013-eligibility'
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
if (-not (Test-Path $PrivateWorktreeRoot)) { New-Item -ItemType Directory -Path $PrivateWorktreeRoot | Out-Null }
$currentIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
& icacls.exe $PrivateWorktreeRoot /inheritance:r /grant:r "${currentIdentity}:(OI)(CI)F" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict the private worktree root to the current user.' }
$env:GIT_CONFIG_COUNT = '1'
$env:GIT_CONFIG_KEY_0 = 'core.longpaths'
$env:GIT_CONFIG_VALUE_0 = 'true'

foreach ($source in ($sources | Where-Object { $RepositoryId.Count -eq 0 -or $_.id -in $RepositoryId })) {
    $record = $manifest.repositories | Where-Object id -eq $source.id
    if ($source.sensitive) {
        $privateManifest = Get-Content (Join-Path $PrivateEvidenceRoot 'private-corpus.json') -Raw | ConvertFrom-Json
        $head = $privateManifest.checkpoints.checkpoints[-1].sha
        $worktree = Join-Path $PrivateWorktreeRoot 'inventory'
        $out = Join-Path $PrivateEvidenceRoot 'inventory-output.json'
    } else {
        $head = $record.history.headSha
        $safeName = $source.id.Split('/')[-1]
        $worktree = Join-Path $tempRoot $safeName
        $out = Join-Path $tempRoot ($safeName + '.json')
    }
    if (Test-Path $worktree) { throw 'The expected M0013 inventory worktree path already exists.' }
    if (Test-Path $out) { Remove-Item -LiteralPath $out -Force }
    $worktreeAdded = $false
    $processExit = $null
    $bootstrapOut = $null
    try {
        git -C $source.path worktree add --detach $worktree $head | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'worktree-add-failed' }
        $worktreeAdded = $true
        if ($source.sensitive) { $initialSeed = $privateManifest.seeds[0] } else { $initialSeed = $record.seeds[0] }
        $statePath = Join-Path $worktree '.hygiene/.state/sampling.json'
        New-Item -ItemType Directory -Force -Path (Split-Path $statePath -Parent) | Out-Null
        $state = [ordered]@{ SchemaVersion = 2; Seed = $initialSeed; Rules = @(); ActivityStateVersion = 3; Projects = @() }
        [System.IO.File]::WriteAllText($statePath, ($state | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))

        $bootstrapOut = [System.IO.Path]::ChangeExtension($out, '.bootstrap.txt')
        $bootstrap = Start-Process -FilePath 'dotnet' -ArgumentList @($cli, 'bootstrap', '--output', 'json') -WorkingDirectory $worktree -NoNewWindow -Wait -PassThru -RedirectStandardOutput $bootstrapOut -RedirectStandardError ([System.IO.Path]::ChangeExtension($bootstrapOut, '.err'))
        if ($bootstrap.ExitCode -ne 0) { throw "Profile bootstrap exited $($bootstrap.ExitCode)." }

        $process = Start-Process -FilePath 'dotnet' -ArgumentList @($cli, 'check', '.', '--output', 'json') -WorkingDirectory $worktree -NoNewWindow -Wait -PassThru -RedirectStandardOutput $out -RedirectStandardError ([System.IO.Path]::ChangeExtension($out, '.err'))
        $processExit = $process.ExitCode
        if ($processExit -ne 0) { throw "CLI exited $processExit." }
        if ((Get-Item $out).Length -eq 0) { throw 'CLI produced no JSON output.' }
        $resultJson = [System.Text.Json.JsonDocument]::Parse([string][System.IO.File]::ReadAllText($out))
        $batchCounts = @{}
        foreach ($batch in $resultJson.RootElement.GetProperty('reviewBatches').EnumerateArray()) {
            $batchCounts[$batch.GetProperty('ruleId').GetString()] = $batch.GetProperty('populationCount').GetInt32()
        }
        $stateJson = [System.Text.Json.JsonDocument]::Parse([string][System.IO.File]::ReadAllText($statePath))
        $projectStates = $stateJson.RootElement.GetProperty('Projects').EnumerateArray()
        $projectCount = 0; $fileCount = 0; $eligibleLoc = 0
        foreach ($project in $projectStates) {
            $projectCount++
            foreach ($file in $project.GetProperty('Sources').EnumerateArray()) { $fileCount++; $eligibleLoc += $file.GetProperty('LineCount').GetInt32() }
        }
        $stateBytes = (Get-Item $statePath).Length
        $summary = [ordered]@{
            id = $source.id
            checkout = if ($source.sensitive) { 'frozen local upstream-branch head' } else { 'local cached default-branch head' }
            exitCode = $processExit
            eligibleProjects = $projectCount
            eligibleSourceFiles = $fileCount
            eligibleSourceLines = $eligibleLoc
            rulePopulations = $batchCounts
            samplingStateBytes = $stateBytes
        }
        if ($source.sensitive) {
            [System.IO.File]::WriteAllText((Join-Path $PrivateEvidenceRoot 'private-eligibility.json'), ($summary | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))
            $results.Add([ordered]@{ id = $source.id; privateEvidence = 'local authorized record'; exitCode = $processExit; eligibleProjects = $projectCount; eligibleSourceFiles = $fileCount; eligibleSourceLines = $eligibleLoc; rulePopulations = $batchCounts; samplingStateBytes = $stateBytes })
        } else {
            $results.Add($summary)
        }
        $resultJson.Dispose(); $stateJson.Dispose()
    } catch {
        $stderrPath = [System.IO.Path]::ChangeExtension($out, '.err')
        $stderr = if (Test-Path $stderrPath) { (Get-Content $stderrPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        if ($source.sensitive) {
            [System.IO.File]::WriteAllText((Join-Path $PrivateEvidenceRoot 'private-eligibility-error.txt'), ($_.Exception.GetType().FullName + ': ' + $_.Exception.Message + "`n" + $stderr), [System.Text.UTF8Encoding]::new($false))
            $results.Add([ordered]@{ id = $source.id; privateEvidence = 'local authorized failure record'; failed = $true })
        } else {
            $results.Add([ordered]@{ id = $source.id; failed = $true; exitCode = $processExit; errorType = $_.Exception.GetType().FullName; errorMessage = $_.Exception.Message; stderr = $stderr })
        }
    } finally {
        if ($worktreeAdded) { git -C $source.path worktree remove --force $worktree | Out-Null }
        if (Test-Path $out) { Remove-Item -LiteralPath $out -Force }
        $err = [System.IO.Path]::ChangeExtension($out, '.err'); if (Test-Path $err) { Remove-Item -LiteralPath $err -Force }
        if (-not [string]::IsNullOrEmpty($bootstrapOut) -and (Test-Path $bootstrapOut)) { Remove-Item -LiteralPath $bootstrapOut -Force }
        if (-not [string]::IsNullOrEmpty($bootstrapOut)) { $bootstrapErr = [System.IO.Path]::ChangeExtension($bootstrapOut, '.err'); if (Test-Path $bootstrapErr) { Remove-Item -LiteralPath $bootstrapErr -Force } }
    }
}

$inventory = [ordered]@{ schemaVersion = 1; measuredAtUtc = [DateTimeOffset]::UtcNow.ToString('o'); implementation = 'current M0012 CLI built from the frozen PR branch'; repositories = @($results) }
$output = Join-Path $RepositoryRoot 'docs/research/ACTIVITY-AGE-ELIGIBILITY.json'
if ($RepositoryId.Count -gt 0 -and (Test-Path $output)) {
    $existing = Get-Content $output -Raw | ConvertFrom-Json
    $merged = [System.Collections.Generic.List[object]]::new()
    foreach ($row in $existing.repositories) { if ($row.id -notin $RepositoryId) { $merged.Add($row) } }
    foreach ($row in $results) { $merged.Add($row) }
    $inventory.repositories = @($merged)
}
[System.IO.File]::WriteAllText($output, ($inventory | ConvertTo-Json -Depth 8), [System.Text.UTF8Encoding]::new($false))
Write-Output "Eligibility aggregate written to $output"
