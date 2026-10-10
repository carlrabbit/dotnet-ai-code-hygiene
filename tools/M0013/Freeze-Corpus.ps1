param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [Parameter(Mandatory = $true)]
    [string]$PrivateRepository,
    [string]$PrivateEvidenceRoot = (Join-Path $env:LOCALAPPDATA 'M0013-private')
)

$ErrorActionPreference = 'Stop'

function Get-Checkpoints([string]$Path, [string]$Ref) {
    $commits = @(git -C $Path rev-list --first-parent --reverse $Ref)
    if ($LASTEXITCODE -ne 0 -or $commits.Count -eq 0) { throw "No accessible first-parent history at $Ref." }
    $checkpoints = [System.Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $commits.Count; $index++) {
        $commit = $commits[$index]
        $parents = @(git -C $Path show -s --format=%P $commit)
        $changed = @()
        if ($index -eq 0 -or [string]::IsNullOrWhiteSpace($parents[0])) {
            $changed = @(git -C $Path ls-tree -r --name-only $commit -- '*.cs')
        } else {
            $parent = $parents[0].Split(' ')[0]
            $changed = @(git -C $Path diff --name-only $parent $commit -- '*.cs')
        }
        $hasSourcePath = @($changed | Where-Object {
            $_ -match '\.cs$' -and
            $_ -notmatch '(^|/)(bin|obj|\.git|\.hygiene)/' -and
            $_ -notmatch '\.(g|generated|designer)\.cs$'
        }).Count -gt 0
        if ($index -eq 0 -or $hasSourcePath) {
            $timestamp = git -C $Path show -s --format=%cI $commit
            $checkpoints.Add([ordered]@{ sha = $commit; committerTimestamp = $timestamp; firstParentIndex = $index })
        }
    }
    return [ordered]@{
        ref = $Ref
        baseSha = $commits[0]
        headSha = $commits[-1]
        firstParentCommitCount = $commits.Count
        checkpointCount = $checkpoints.Count
        checkpoints = @($checkpoints)
    }
}

function Get-FixedSeeds([string]$CorpusId) {
    return @(1..5 | ForEach-Object {
        $label = 'M0013-v1|' + $CorpusId + '|seed-' + $_.ToString('00')
        $bytes = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($label))
        [System.Convert]::ToHexString($bytes)
    })
}

$privateIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
New-Item -ItemType Directory -Force -Path $PrivateEvidenceRoot | Out-Null
& icacls.exe $PrivateEvidenceRoot /inheritance:r /grant:r "${privateIdentity}:(OI)(CI)F" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict the private M0013 evidence directory to the current user.' }

$definitions = @(
    [ordered]@{ id = 'carlrabbit/Private'; path = $PrivateRepository; visibility = 'private' },
    [ordered]@{ id = 'carlrabbit/dotnet-semantic-type-model'; path = 'C:\src\dotnet-semantic-type-model'; visibility = 'public' },
    [ordered]@{ id = 'carlrabbit/dotnet-ai-code-hygiene'; path = 'C:\src\dotnet-ai-code-hygiene'; visibility = 'public' },
    [ordered]@{ id = 'carlrabbit/dotnet-ai-first-2d-game-engine'; path = 'C:\src\dotnet-ai-first-2d-game-engine'; visibility = 'public' }
)

$privateData = [ordered]@{ schemaVersion = 1; frozenAtUtc = [DateTimeOffset]::UtcNow.ToString('o'); repository = 'carlrabbit/Private'; checkpoints = $null; seeds = $null }
$publicRepositories = [System.Collections.Generic.List[object]]::new()
foreach ($definition in $definitions) {
    if (-not (Test-Path (Join-Path $definition.path '.git'))) { throw "Expected local Git repository is unavailable: $($definition.id)." }
    $history = Get-Checkpoints $definition.path 'refs/remotes/origin/main'
    $seeds = Get-FixedSeeds $definition.id
    $tree = @(git -C $definition.path ls-tree -r --name-only $history.headSha)
    $eligiblePathCount = @($tree | Where-Object {
        $_ -match '\.cs$' -and
        $_ -notmatch '(^|/)(bin|obj|\.git|\.hygiene)/' -and
        $_ -notmatch '\.(g|generated|designer)\.cs$'
    }).Count
    if ($definition.visibility -eq 'private') {
        $privateData.checkpoints = $history
        $privateData.seeds = $seeds
        $ids = (@($history.checkpoints | ForEach-Object { $_.sha }) -join "`n")
        $digest = [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($ids)))
        $privateData.checkpointManifestSha256 = $digest
        $privatePath = Join-Path $PrivateEvidenceRoot 'private-corpus.json'
        $privateJson = $privateData | ConvertTo-Json -Depth 8
        [System.IO.File]::WriteAllText($privatePath, $privateJson, [System.Text.UTF8Encoding]::new($false))
        $publicRepositories.Add([ordered]@{
            id = $definition.id
            visibility = 'private'
            defaultRef = 'local refs/remotes/origin/main; remote freshness unconfirmed'
            firstParentCommitCount = $history.firstParentCommitCount
            checkpointCount = $history.checkpointCount
            eligibleCSharpPathCountAtHead = $eligiblePathCount
            checkpointManifest = 'withheld; exact sequence and checksum remain in the authorized local manifest'
            revisionIds = 'withheld; local authorized evidence only'
            seedValues = 'withheld; derived by the public protocol formula in the local authorized manifest'
        })
    } else {
        $publicRepositories.Add([ordered]@{
            id = $definition.id
            visibility = 'public'
            defaultRef = 'local refs/remotes/origin/main; remote freshness unconfirmed'
            firstParentCommitCount = $history.firstParentCommitCount
            checkpointCount = $history.checkpointCount
            eligibleCSharpPathCountAtHead = $eligiblePathCount
            history = $history
            seeds = $seeds
        })
    }
}

$manifest = [ordered]@{
    schemaVersion = 1
    experiment = 'M0013'
    frozenAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    environment = [ordered]@{
        os = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        dotnetSdk = (& dotnet --version)
        shell = 'PowerShell'
        externalNetworkFreshness = 'GitHub fetch timed out; local origin/main refs were used and must be refreshed/revalidated before any later replication.'
    }
    checkpointRule = 'Initial available first-parent commit plus every first-parent commit changing a tracked non-generated .cs path; C# source eligibility and per-project populations are measured by the M0012 Roslyn loader during replay.'
    policies = [ordered]@{
        A = 'M0011 elapsed-day hazard overlay: first/change hazards unchanged; subject H += deltaSeconds/(365*86400); BORINGness H += previousCandidateCount*deltaSeconds/(365*86400).'
        B = 'M0012 project-scoped normalized source activity using the production implementation and fixed rule-owned policies.'
        C = 'M0012 first/change hazards only; suppress project activity accrual while retaining the same fingerprints, rule versions, budgets, sampler algorithms and seeds.'
        sharedRandomness = 'Each replicate seed is shared across A/B/C. Current M0012 rule/model versions are held constant to isolate only the age input.'
    }
    seeds = [ordered]@{ count = 5; formula = 'uppercase hex SHA256(UTF8("M0013-v1|<stable-corpus-id>|seed-01..05"))' }
    observationArms = @('all selected tickets explicitly observed after each checkpoint (synthetic scheduling only)', 'no tickets observed (synthetic scheduling only)')
    blindedPool = [ordered]@{ selectedEncounterCapPerPolicyPerRepository = 25; randomSupplementCapPerRepository = 25; equalCompletedReviewBudgets = @(25,50,100); rubric = 'the fixed M0012 questions for each semantic-review rule'; provenance = 'blinded subject/revision pair; policy/seed omitted from reviewer view' }
    ceilings = [ordered]@{ fullReplayWallClockMinutes = 180; recordPerCheckpointMilliseconds = $true; recordSerializedStateBytes = $true; stopOnMissingCheckpoint = $true }
    privateEvidence = [ordered]@{ storage = 'restricted user-only local evidence store; path withheld'; rawInputsAndRevisionIdsCommitted = $false }
    repositories = @($publicRepositories)
}
$publicPath = Join-Path $RepositoryRoot 'docs/research/ACTIVITY-AGE-CORPUS.json'
[System.IO.File]::WriteAllText($publicPath, ($manifest | ConvertTo-Json -Depth 12), [System.Text.UTF8Encoding]::new($false))
Write-Output "Frozen M0013 public corpus manifest: $publicPath"
Write-Output "Private raw checkpoint and seed manifest stored in the configured restricted evidence directory outside the repository."
