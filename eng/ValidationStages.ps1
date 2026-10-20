$script:ValidationRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$script:ReceiptRoot = Join-Path $script:ValidationRoot '.execution\validation'
$script:ReceiptVersion = 1

$script:ValidationStages = @(
    [pscustomobject]@{ Id = 'solution'; Command = '[cold] dotnet clean .\DotNetAiCodeHygiene.slnx; dotnet restore .\DotNetAiCodeHygiene.slnx; dotnet build .\DotNetAiCodeHygiene.slnx --configuration Release --no-restore'; DependsOn = @(); Inputs = @('global.json','.editorconfig','Directory.Build.props','DotNetAiCodeHygiene.slnx','src/**','tests/**'); Outputs = @('src/**/bin/Release/**/*.dll','tests/**/bin/Release/**/*') },
    [pscustomobject]@{ Id = 'core-tests'; Command = 'dotnet test .\tests\DotNetAiCodeHygiene.Core.Tests\DotNetAiCodeHygiene.Core.Tests.csproj --configuration Release --no-build --show-slowest-tests 10 [--treenode-filter <filter>]'; DependsOn = @('solution'); Inputs = @('global.json','.editorconfig','Directory.Build.props','tests/DotNetAiCodeHygiene.Core.Tests/**','src/DotNetAiCodeHygiene.Core/**'); Outputs = @('TestResults/DotNetAiCodeHygiene.Core.Tests-*-report.html') },
    [pscustomobject]@{ Id = 'cli-tests'; Command = 'dotnet test .\tests\DotNetAiCodeHygiene.Cli.Tests\DotNetAiCodeHygiene.Cli.Tests.csproj --configuration Release --no-build --show-slowest-tests 10 [--treenode-filter <filter>]'; DependsOn = @('solution'); Inputs = @('global.json','.editorconfig','Directory.Build.props','tests/DotNetAiCodeHygiene.Cli.Tests/**','src/**'); Outputs = @('TestResults/DotNetAiCodeHygiene.Cli.Tests-*-report.html') },
    [pscustomobject]@{ Id = 'readme-policy'; Command = 'Check README lifecycle headings and required installed-tool examples'; DependsOn = @(); Inputs = @('README.md'); Outputs = @() },
    [pscustomobject]@{ Id = 'pack-current'; Command = 'dotnet pack .\src\DotNetAiCodeHygiene.Cli\DotNetAiCodeHygiene.Cli.csproj --configuration Release --no-build'; DependsOn = @('solution'); Inputs = @('global.json','.editorconfig','Directory.Build.props','src/**'); Outputs = @('artifacts/packages/DotNetAiCodeHygiene.Tool.0.11.0.nupkg') },
    [pscustomobject]@{ Id = 'installed-consumers'; Command = '& .\eng\validate-installed-consumers.ps1'; DependsOn = @('pack-current'); Inputs = @('global.json','.editorconfig','Directory.Build.props','src/**','eng/validate-installed-consumers.ps1'); Outputs = @('.execution/validation/consumer-provenance.json') },
    [pscustomobject]@{ Id = 'self-host'; Command = '& .\eng\validate-self-host.ps1'; DependsOn = @('solution'); Inputs = @('global.json','.editorconfig','.gitignore','AGENTS.md','Directory.Build.props','Directory.Packages.props','DotNetAiCodeHygiene.slnx','README.md','.hygiene/**','docs/**','src/**','tests/**','eng/validate-self-host.ps1'); Outputs = @() }
)

function Get-StageReceiptPath([string] $Id) { Join-Path $script:ReceiptRoot ($Id + '.json') }

function Get-StageFiles($Stage) {
    $found = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($pattern in @($Stage.Inputs) + @('eng/validate.ps1','eng/ValidationStages.ps1')) {
        if ($pattern.Contains('**')) {
            $prefix = $pattern.Split('/')[0]
            $base = Join-Path $script:ValidationRoot $prefix
            if (Test-Path -LiteralPath $base) {
                Get-ChildItem -LiteralPath $base -File -Recurse -Force | Where-Object { $_.FullName -notmatch '[\\/](bin|obj|TestResults|artifacts)[\\/]' -and $_.FullName -notmatch '[\\/]\.hygiene[\\/]\.state[\\/]' } | ForEach-Object { [void]$found.Add($_.FullName) }
            }
        }
        else {
            $path = Join-Path $script:ValidationRoot ($pattern -replace '/', '\')
            if (Test-Path -LiteralPath $path -PathType Leaf) { [void]$found.Add((Resolve-Path -LiteralPath $path).Path) }
        }
    }
    @($found | Sort-Object)
}

function Get-InputDigest($Stage) {
    $items = @(
    foreach ($file in Get-StageFiles $Stage) {
        $relative = [IO.Path]::GetRelativePath($script:ValidationRoot, $file).Replace('\','/')
        $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
        "$relative`0$hash"
    })
    if ($Stage.Id -eq 'self-host') { $items += "`$git-diff`0$(Get-RepositoryDeltaDigest)" }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($items -join "`n"))
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-RepositoryDeltaDigest {
    $base = (& git merge-base HEAD origin/main 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or -not $base) { throw 'Could not identify origin/main merge base for repository working-tree validation.' }
    $delta = (& git diff --binary --no-ext-diff $base -- 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0) { throw 'Could not fingerprint the repository change set.' }
    $parts = [System.Collections.Generic.List[string]]::new()
    $parts.Add("base`0$base")
    $parts.Add("diff`0$delta")
    $untracked = @(& git ls-files --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate untracked repository inputs.' }
    foreach ($relative in $untracked | Sort-Object -Unique) {
        $path = Join-Path $script:ValidationRoot $relative
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
            $parts.Add("untracked`0$($relative.Replace('\','/'))`0$hash")
        }
    }
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($parts -join "`n")))).ToLowerInvariant()
}

function Get-ToolIdentity {
    $dotnet = (& dotnet --version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Could not identify the active .NET SDK.' }
    $git = (& git --version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Could not identify the active Git installation.' }
    [pscustomobject]@{ dotnet = $dotnet; git = $git; powershell = $PSVersionTable.PSVersion.ToString(); os = [Runtime.InteropServices.RuntimeInformation]::OSDescription }
}

function Get-OutputInventory($Stage) {
    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($pattern in $Stage.Outputs) {
        $path = Join-Path $script:ValidationRoot ($pattern -replace '/', '\')
        if ($pattern -eq 'src/**/bin/Release/**/*.dll') {
            $prefix = $pattern.Split('/')[0]
            $base = Join-Path $script:ValidationRoot $prefix
            if (Test-Path -LiteralPath $base) {
                Get-ChildItem -LiteralPath $base -File -Recurse -Filter '*.dll' | Where-Object { $_.FullName -match '[\\/]bin[\\/]Release[\\/]' -and $_.FullName -notmatch '[\\/]publish[\\/]' } | ForEach-Object {
                    $rows.Add([pscustomobject]@{ path = [IO.Path]::GetRelativePath($script:ValidationRoot, $_.FullName).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() })
                }
            }
        }
        elseif ($pattern -eq 'tests/**/bin/Release/**/*') {
            $base = Join-Path $script:ValidationRoot 'tests'
            if (Test-Path -LiteralPath $base) {
                Get-ChildItem -LiteralPath $base -File -Recurse -Force | Where-Object { $_.FullName -match '[\\/]bin[\\/]Release[\\/]' -and $_.FullName -notmatch '[\\/]publish[\\/]' -and $_.Extension -in @('.dll','.exe','.pdb','.json') } | ForEach-Object {
                    $rows.Add([pscustomobject]@{ path = [IO.Path]::GetRelativePath($script:ValidationRoot, $_.FullName).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() })
                }
            }
        }
        elseif ($pattern.Contains('*')) {
            $separator = $pattern.LastIndexOf('/')
            $directory = Join-Path $script:ValidationRoot ($pattern.Substring(0,$separator) -replace '/', '\')
            $filePattern = $pattern.Substring($separator + 1)
            if (Test-Path -LiteralPath $directory) {
                Get-ChildItem -Path (Join-Path $directory $filePattern) -File -ErrorAction SilentlyContinue | ForEach-Object {
                    $rows.Add([pscustomobject]@{ path = [IO.Path]::GetRelativePath($script:ValidationRoot, $_.FullName).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() })
                }
            }
        }
        elseif (Test-Path -LiteralPath $path -PathType Leaf) {
            $rows.Add([pscustomobject]@{ path = $pattern; sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() })
        }
    }
    @($rows | Sort-Object path)
}

function Get-ExpectedFingerprint($Stage) {
    $dependencyReceipts = foreach ($dependency in $Stage.DependsOn) {
        $dep = Get-Content -LiteralPath (Get-StageReceiptPath $dependency) -Raw | ConvertFrom-Json
        [pscustomobject]@{ id = $dependency; fingerprint = $dep.fingerprint; outputs = @($dep.outputs) }
    }
    $payload = [ordered]@{
        schemaVersion = $script:ReceiptVersion
        id = $Stage.Id
        command = $Stage.Command
        inputDigest = Get-InputDigest $Stage
        toolchain = Get-ToolIdentity
        dependencies = @($dependencyReceipts)
    }
    $json = $payload | ConvertTo-Json -Depth 16 -Compress
    [pscustomobject]@{ hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($json))).ToLowerInvariant(); payload = $payload }
}

function Test-StageReceipt($Stage, [switch] $Quiet) {
    $receiptPath = Get-StageReceiptPath $Stage.Id
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { return $false }
    try {
        $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
        if ($receipt.schemaVersion -ne $script:ReceiptVersion -or $receipt.status -ne 'passed') { return $false }
        foreach ($dependency in $Stage.DependsOn) { if (-not (Test-StageReceipt ($script:ValidationStages | Where-Object Id -eq $dependency) -Quiet)) { return $false } }
        $expected = Get-ExpectedFingerprint $Stage
        if ($receipt.fingerprint -ne $expected.hash) { return $false }
        $outputs = @(Get-OutputInventory $Stage)
        if ((@($receipt.outputs | ConvertTo-Json -Depth 8 -Compress)) -cne (@($outputs | ConvertTo-Json -Depth 8 -Compress))) { return $false }
        return $true
    }
    catch { return $false }
}

function Assert-Prerequisites($Stage) {
    foreach ($dependency in $Stage.DependsOn) {
        $depStage = $script:ValidationStages | Where-Object Id -eq $dependency
        if (-not (Test-StageReceipt $depStage)) { throw "Stage '$($Stage.Id)' requires current successful '$dependency' evidence. Run -Shard $dependency first." }
    }
}

function Invoke-StageCommand($Stage, [string] $Filter) {
    switch ($Stage.Id) {
        'solution' {
            if ($env:DOTNET_HYGIENE_VALIDATION_COLD -eq '1') {
                dotnet clean .\DotNetAiCodeHygiene.slnx --configuration Release
                if ($LASTEXITCODE -ne 0) { throw "dotnet clean failed with exit code $LASTEXITCODE." }
            }
            dotnet restore .\DotNetAiCodeHygiene.slnx
            if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }
            dotnet build .\DotNetAiCodeHygiene.slnx --configuration Release --no-restore
            if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE." }
        }
        'core-tests' {
            $args = @('test','.\tests\DotNetAiCodeHygiene.Core.Tests\DotNetAiCodeHygiene.Core.Tests.csproj','--configuration','Release','--no-build','--show-slowest-tests','10')
            if ($Filter) { $args += @('--treenode-filter',$Filter) }
            & dotnet @args
            if ($LASTEXITCODE -ne 0) { throw "Core test stage failed with exit code $LASTEXITCODE." }
        }
        'cli-tests' {
            $args = @('test','.\tests\DotNetAiCodeHygiene.Cli.Tests\DotNetAiCodeHygiene.Cli.Tests.csproj','--configuration','Release','--no-build','--show-slowest-tests','10')
            if ($Filter) { $args += @('--treenode-filter',$Filter) }
            & dotnet @args
            if ($LASTEXITCODE -ne 0) { throw "CLI test stage failed with exit code $LASTEXITCODE." }
        }
        'readme-policy' {
            $readmeText = Get-Content -LiteralPath .\README.md -Raw
            foreach ($heading in @('## First Bootstrap','## Run on Gates','## Manual Inspection and Debugging','## Exhaustive read-only audit','## Updating to a New Release')) { if (-not $readmeText.Contains($heading)) { throw "README is missing required lifecycle section '$heading'." } }
            foreach ($example in @('dotnet hygiene --version','dotnet hygiene check --base origin/main','dotnet hygiene review handoff','dotnet hygiene baseline accept --approver','--report-sha256','--yolo-all --attest-explicit-task','dotnet hygiene audit next --run','dotnet hygiene audit record --run','dotnet hygiene audit finish --run')) { if (-not $readmeText.Contains($example)) { throw "README is missing required lifecycle example '$example'." } }
        }
        'pack-current' {
            dotnet pack .\src\DotNetAiCodeHygiene.Cli\DotNetAiCodeHygiene.Cli.csproj --configuration Release --no-build
            if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed with exit code $LASTEXITCODE." }
            $package = Get-Item .\artifacts\packages\DotNetAiCodeHygiene.Tool.0.11.0.nupkg
            Write-Output ("Current package SHA-256: " + (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash)
        }
        'installed-consumers' { & .\eng\validate-installed-consumers.ps1; if ($LASTEXITCODE -ne 0) { throw "Installed-consumer validation failed with exit code $LASTEXITCODE." } }
        'self-host' { & .\eng\validate-self-host.ps1; if ($LASTEXITCODE -ne 0) { throw "Self-host validation failed with exit code $LASTEXITCODE." } }
        default { throw "Unknown validation stage '$($Stage.Id)'." }
    }
}

function Write-StageReceipt($Stage, [DateTimeOffset] $Started, [double] $ElapsedSeconds, [string] $Filter, $Before) {
    $expected = Get-ExpectedFingerprint $Stage
    if ($expected.hash -ne $Before.hash) { throw "Inputs, prerequisites, or toolchain changed while stage '$($Stage.Id)' was running; no success receipt was written." }
    $endInputDigest = $expected.payload.inputDigest
    if ($Filter) {
        Write-Output ("PASS {0} focused filter '{1}': {2:N2}s; focused evidence does not satisfy the complete stage receipt." -f $Stage.Id,$Filter,$ElapsedSeconds)
        return
    }
    $outputInventory = @(Get-OutputInventory $Stage)
    if ($Stage.Id -in @('solution','core-tests','cli-tests','pack-current','installed-consumers') -and $outputInventory.Count -eq 0) { throw "Stage '$($Stage.Id)' produced none of its required output artifacts; no success receipt was written." }
    $receipt = [ordered]@{
        schemaVersion = $script:ReceiptVersion; id = $Stage.Id; status = 'passed'; fingerprint = $expected.hash
        inputDigest = $endInputDigest; command = $Stage.Command; dependencies = @($Stage.DependsOn)
        startedUtc = $Started.ToString('o'); elapsedSeconds = [Math]::Round($ElapsedSeconds,3); execution = 'fresh'; filter = $Filter
        toolchain = $expected.payload.toolchain; prerequisites = @($expected.payload.dependencies); outputs = $outputInventory
    }
    $path = Get-StageReceiptPath $Stage.Id
    $temporary = $path + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    $receipt | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $temporary -Encoding utf8
    Move-Item -LiteralPath $temporary -Destination $path -Force
    Write-Output ("PASS {0}: {1:N2}s (fresh); receipt {2}" -f $Stage.Id,$ElapsedSeconds,$path)
}

function Assert-CompleteValidation {
    $invalid = @($script:ValidationStages | Where-Object { -not (Test-StageReceipt $_) } | ForEach-Object Id)
    if ($invalid.Count) { throw ('Validation evidence is incomplete or stale: ' + ($invalid -join ', ')) }
    foreach ($stage in $script:ValidationStages) {
        $receipt = Get-Content -LiteralPath (Get-StageReceiptPath $stage.Id) -Raw | ConvertFrom-Json
        [pscustomobject]@{ id=$stage.Id; status='reused'; elapsedSeconds=$receipt.elapsedSeconds; receipt=(Get-StageReceiptPath $stage.Id) }
    }
}

function Invoke-ValidationStages {
    param([switch] $PlanJson,[string] $Shard,[switch] $Verify,[switch] $Fresh,[string] $TestFilter)
    Set-Location $script:ValidationRoot
    $plan = @($script:ValidationStages | ForEach-Object { [pscustomobject]@{ id=$_.Id; command=$_.Command; dependencies=@($_.DependsOn); expectedReceipt=(Get-StageReceiptPath $_.Id); expectedOutputs=@($_.Outputs) } })
    if ($PlanJson) { [pscustomobject]@{ schemaVersion=1; sequentialOnly=$true; stages=$plan } | ConvertTo-Json -Depth 12; return }
    if ($Shard -and -not ($script:ValidationStages.Id -contains $Shard)) { throw "Unknown validation shard '$Shard'. Use -PlanJson to list stable stage IDs." }
    if ($TestFilter -and $Shard -notin @('core-tests','cli-tests')) { throw '-TestFilter is available only with -Shard core-tests or -Shard cli-tests.' }
    New-Item -ItemType Directory -Force -Path $script:ReceiptRoot | Out-Null
    $lockPath = Join-Path $script:ReceiptRoot 'execution.lock'
    $lock = $null
    try { $lock = [IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None) }
    catch { throw 'Another validation stage/aggregate is active. Overlapping execution is prohibited; retry after it exits.' }
    try {
        if ($Fresh) { Get-ChildItem -LiteralPath $script:ReceiptRoot -Filter '*.json' -File | Remove-Item -Force }
        if ($Verify) {
            $rows = @(Assert-CompleteValidation)
            [pscustomobject]@{status='verified';stages=$rows;executed=0;reused=$rows.Count} | ConvertTo-Json -Depth 8
            return
        }
        if ($Shard) {
            $stage = $script:ValidationStages | Where-Object Id -eq $Shard
            Assert-Prerequisites $stage
            if ((Test-StageReceipt $stage) -and -not $Fresh -and -not $TestFilter) { Write-Output ("REUSED {0}: current receipt and dependencies match." -f $stage.Id); return }
            if (-not $TestFilter) { Remove-Item -LiteralPath (Get-StageReceiptPath $stage.Id) -Force -ErrorAction SilentlyContinue }
            $before=Get-ExpectedFingerprint $stage; $started = [DateTimeOffset]::UtcNow; $timer=[Diagnostics.Stopwatch]::StartNew()
            try { Invoke-StageCommand $stage $TestFilter; $timer.Stop(); Write-StageReceipt $stage $started $timer.Elapsed.TotalSeconds $TestFilter $before }
            catch { $timer.Stop(); Remove-Item -LiteralPath (Get-StageReceiptPath $stage.Id) -Force -ErrorAction SilentlyContinue; Write-Output ("FAIL {0}: {1:N2}s; no valid receipt retained." -f $stage.Id,$timer.Elapsed.TotalSeconds); throw }
            return
        }
        $runs = [System.Collections.Generic.List[object]]::new()
        foreach ($stage in $script:ValidationStages) {
            Assert-Prerequisites $stage
            if ((Test-StageReceipt $stage) -and -not $Fresh) { $receipt=Get-Content -LiteralPath (Get-StageReceiptPath $stage.Id) -Raw | ConvertFrom-Json; Write-Output ("REUSED {0}: prior execution {1:N2}s." -f $stage.Id,$receipt.elapsedSeconds); $runs.Add([pscustomobject]@{id=$stage.Id;execution='reused';seconds=$receipt.elapsedSeconds}); continue }
            Remove-Item -LiteralPath (Get-StageReceiptPath $stage.Id) -Force -ErrorAction SilentlyContinue
            $before=Get-ExpectedFingerprint $stage; $started=[DateTimeOffset]::UtcNow; $timer=[Diagnostics.Stopwatch]::StartNew()
            try { Invoke-StageCommand $stage ''; $timer.Stop(); Write-StageReceipt $stage $started $timer.Elapsed.TotalSeconds '' $before; $runs.Add([pscustomobject]@{id=$stage.Id;execution='fresh';seconds=$timer.Elapsed.TotalSeconds}) }
            catch { $timer.Stop(); Remove-Item -LiteralPath (Get-StageReceiptPath $stage.Id) -Force -ErrorAction SilentlyContinue; Write-Output ("FAIL {0}: {1:N2}s; no valid receipt retained." -f $stage.Id,$timer.Elapsed.TotalSeconds); throw }
        }
        $finalReceipts = @(Assert-CompleteValidation)
        $total=($runs | Measure-Object -Property seconds -Sum).Sum
        $slowest=$runs | Sort-Object seconds -Descending | Select-Object -First 3
        Write-Output ("Aggregate verified complete: {0:N2}s of stage execution; fresh={1}, reused={2}, verified={3}. Slowest: {4}" -f $total,(@($runs | Where-Object execution -eq 'fresh').Count),(@($runs | Where-Object execution -eq 'reused').Count),$finalReceipts.Count,(($slowest | ForEach-Object { '{0} {1:N2}s {2}' -f $_.id,$_.seconds,$_.execution }) -join '; '))
    }
    finally { if ($null -ne $lock) { $lock.Dispose() } }
}
