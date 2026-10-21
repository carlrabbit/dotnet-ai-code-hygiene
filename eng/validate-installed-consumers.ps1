$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$package = Get-ChildItem .\artifacts\packages\DotNetAiCodeHygiene.Tool.0.11.0.nupkg
# Tier 4: install the exact package just packed into an isolated consumer tool path.
$package = Get-ChildItem .\artifacts\packages\DotNetAiCodeHygiene.Tool.0.11.0.nupkg
$tierRoot = Join-Path ([IO.Path]::GetTempPath()) ('hygiene-tier4-' + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $tierRoot 'feed'; $toolPath = Join-Path $tierRoot 'tools'; $upgradeToolPath = Join-Path $tierRoot 'upgrade-tools'; $consumer = Join-Path $tierRoot 'consumer'; $autonomousConsumer = Join-Path $tierRoot 'autonomous'; $nugetCache = Join-Path $tierRoot 'nuget-cache'
$nugetConfig = Join-Path $tierRoot 'NuGet.config'
New-Item -ItemType Directory -Force $feed, $toolPath, $upgradeToolPath, $consumer, $autonomousConsumer, $nugetCache | Out-Null
Copy-Item -LiteralPath $package.FullName -Destination $feed
if ((Get-FileHash -LiteralPath (Join-Path $feed $package.Name) -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash) { throw 'Tier-4 local feed package does not match the package just packed.' }
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="CurrentRun" value="$feed" />
    <add key="NuGetOfficial" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="CurrentRun">
      <package pattern="DotNetAiCodeHygiene.Tool" />
    </packageSource>
    <packageSource key="NuGetOfficial">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfig -Encoding utf8
$previousPath = $env:PATH
$env:PATH = $toolPath + [IO.Path]::PathSeparator + $env:PATH
$previousNugetPackages = $env:NUGET_PACKAGES
$env:NUGET_PACKAGES = $nugetCache
dotnet tool install DotNetAiCodeHygiene.Tool --tool-path $toolPath --configfile $nugetConfig --version 0.11.0
if ($LASTEXITCODE -ne 0) { throw "Tier-4 tool install failed with exit code $LASTEXITCODE." }
$hygiene = Join-Path $toolPath 'dotnet-hygiene.exe'
if (-not (Test-Path -LiteralPath $hygiene)) { throw 'Tier-4 installed hygiene command was not created.' }
$installedShimSha256 = (Get-FileHash -LiteralPath $hygiene -Algorithm SHA256).Hash.ToLowerInvariant()
$pathBeforeUpgrade = $env:PATH
$priorPackage = Join-Path $feed 'DotNetAiCodeHygiene.Tool.0.9.0.nupkg'
$priorSource = Join-Path $tierRoot 'previous-version-source'
$priorFixtureCommit = '2e17b293be78480dcad9817f43c9bcc079a81ea8'
$currentPackageSha256 = (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$fixtureDisposition = 'built'
$fixturePackageSha256 = ''
git cat-file -e "$priorFixtureCommit`^{commit}"
if ($LASTEXITCODE -ne 0) { throw "Deterministic previous-version fixture commit $priorFixtureCommit is unavailable; EC-19c cannot pass." }
$priorTree = (& git rev-parse "$priorFixtureCommit`^{tree}").Trim()
if ($LASTEXITCODE -ne 0 -or -not $priorTree) { throw 'Could not identify the pinned previous-version source tree.' }
$sdkIdentity = (& dotnet --version).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Could not identify the SDK for the previous-version fixture.' }
$fixtureIdentity = "$priorFixtureCommit|$priorTree|$sdkIdentity|DotNetAiCodeHygiene.Tool|0.9.0"
$fixtureKey = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($fixtureIdentity))).ToLowerInvariant()
$fixtureCache = Join-Path $env:LOCALAPPDATA 'DotNetAiCodeHygiene\ValidationCache'
$cachedPackage = Join-Path $fixtureCache ($fixtureKey + '.nupkg')
$cachedReceipt = Join-Path $fixtureCache ($fixtureKey + '.json')
$allowFixtureCacheRead = $env:DOTNET_HYGIENE_VALIDATION_COLD -ne '1'
$cacheValid = $false
if ($allowFixtureCacheRead -and (Test-Path -LiteralPath $cachedPackage) -and (Test-Path -LiteralPath $cachedReceipt)) {
    try {
        $cacheReceipt = Get-Content -LiteralPath $cachedReceipt -Raw | ConvertFrom-Json
        $cacheHash = (Get-FileHash -LiteralPath $cachedPackage -Algorithm SHA256).Hash.ToLowerInvariant()
        $cacheValid = $cacheReceipt.schemaVersion -eq 1 -and $cacheReceipt.identity -ceq $fixtureIdentity -and $cacheReceipt.sourceCommit -ceq $priorFixtureCommit -and $cacheReceipt.sourceTree -ceq $priorTree -and $cacheReceipt.sdk -ceq $sdkIdentity -and $cacheReceipt.sha256 -ceq $cacheHash
    }
    catch { $cacheValid = $false }
}
if ($cacheValid) {
    Copy-Item -LiteralPath $cachedPackage -Destination $priorPackage
    $fixtureDisposition = 'reused'
    $fixturePackageSha256 = $cacheHash
    Write-Output ("Previous-version fixture: reused verified immutable cache {0} (SHA-256 {1})." -f $fixtureKey,$cacheHash)
}
else {
    Write-Output ("Previous-version fixture: building pinned source {0} with SDK {1}." -f $priorFixtureCommit,$sdkIdentity)
    git worktree add --detach $priorSource $priorFixtureCommit | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not prepare the deterministic previous-version source fixture.' }
    try {
        $priorProject = Join-Path $priorSource 'src\DotNetAiCodeHygiene.Cli\DotNetAiCodeHygiene.Cli.csproj'
        $priorProjectText = Get-Content -LiteralPath $priorProject -Raw
        if ($priorProjectText -notmatch '<Version>0\.9\.0</Version>' -or $priorProjectText -notmatch '<ToolCommandName>hygiene</ToolCommandName>') { throw 'Pinned previous-version source no longer declares the expected 0.9.0 legacy-tool fixture.' }
        dotnet pack $priorProject --configuration Release --output $feed | Out-Null
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $priorPackage)) { throw 'Deterministic 0.9.0 package fixture could not be built; EC-19c is not established.' }
    }
    finally { git worktree remove --force $priorSource | Out-Null }
    $fixturePackageSha256 = (Get-FileHash -LiteralPath $priorPackage -Algorithm SHA256).Hash.ToLowerInvariant()
    New-Item -ItemType Directory -Force -Path $fixtureCache | Out-Null
    $cachedPackageTemp = $cachedPackage + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    Copy-Item -LiteralPath $priorPackage -Destination $cachedPackageTemp
    Move-Item -LiteralPath $cachedPackageTemp -Destination $cachedPackage -Force
    $cacheHash = (Get-FileHash -LiteralPath $cachedPackage -Algorithm SHA256).Hash.ToLowerInvariant()
    $cacheReceiptTemp = $cachedReceipt + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    [pscustomobject]@{ schemaVersion=1; identity=$fixtureIdentity; sha256=$cacheHash; sourceCommit=$priorFixtureCommit; sourceTree=$priorTree; sdk=$sdkIdentity; createdUtc=[DateTimeOffset]::UtcNow.ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath $cacheReceiptTemp -Encoding utf8
    Move-Item -LiteralPath $cacheReceiptTemp -Destination $cachedReceipt -Force
    Write-Output ("Previous-version fixture: cached immutable package {0} (SHA-256 {1})." -f $fixtureKey,$cacheHash)
}
$provenancePath = Join-Path $PSScriptRoot '..\.execution\validation\consumer-provenance.json'
$provenanceTemp = $provenancePath + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
[pscustomobject]@{ schemaVersion=1; currentPackage='DotNetAiCodeHygiene.Tool.0.11.0'; currentPackageSha256=$currentPackageSha256; currentPackagePath=[IO.Path]::GetRelativePath((Resolve-Path (Join-Path $PSScriptRoot '..')).Path,$package.FullName).Replace('\','/'); installedCommand='dotnet-hygiene'; installedVersion='0.11.0'; installedShimSha256=$installedShimSha256; previousFixtureCommit=$priorFixtureCommit; previousFixtureTree=$priorTree; previousFixtureSdk=$sdkIdentity; previousFixtureDisposition=$fixtureDisposition; previousFixtureSha256=$fixturePackageSha256; previousFixtureKey=$fixtureKey; recordedUtc=[DateTimeOffset]::UtcNow.ToString('o') } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $provenanceTemp -Encoding utf8
Move-Item -LiteralPath $provenanceTemp -Destination $provenancePath -Force
dotnet tool install DotNetAiCodeHygiene.Tool --tool-path $upgradeToolPath --configfile $nugetConfig --version 0.9.0
if ($LASTEXITCODE -ne 0) { throw 'Tier-4 previous-version tool install failed.' }
$priorInstalled = dotnet tool list --tool-path $upgradeToolPath --format json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or @($priorInstalled.data | Where-Object { $_.packageId -ieq 'DotNetAiCodeHygiene.Tool' -and $_.version -eq '0.9.0' }).Count -ne 1) { throw 'Previous-version fixture install did not resolve the verified 0.9.0 package.' }
dotnet tool update DotNetAiCodeHygiene.Tool --tool-path $upgradeToolPath --configfile $nugetConfig --version 0.11.0
if ($LASTEXITCODE -ne 0) { throw 'Tier-4 trusted local-feed tool update failed.' }
$updatedShim = Join-Path $upgradeToolPath 'dotnet-hygiene.exe'
if (-not (Test-Path -LiteralPath $updatedShim) -or (Test-Path -LiteralPath (Join-Path $upgradeToolPath 'hygiene.exe'))) { throw 'Tier-4 update did not replace the legacy shim with the canonical dotnet-hygiene shim.' }
$env:PATH = $upgradeToolPath + [IO.Path]::PathSeparator + $env:PATH
$updatedVersion = dotnet hygiene --version
if ($LASTEXITCODE -ne 0 -or ($updatedVersion -join '').Trim() -ne '0.11.0') { throw 'Tier-4 updated dotnet hygiene command did not restart and report version 0.11.0.' }
$env:PATH = $pathBeforeUpgrade

# A newer stable fixture proves autonomous discovery. Keep the required 0.11.0
# consumer above exact; 0.11.1 exists only in this disposable feed.
 $releaseFeed = Join-Path $tierRoot 'release-feed'
New-Item -ItemType Directory -Force $releaseFeed | Out-Null
Copy-Item -LiteralPath $package.FullName -Destination $releaseFeed
$releaseBuildOutput = (Join-Path $tierRoot 'release-build-output') + [IO.Path]::DirectorySeparatorChar
dotnet pack .\src\DotNetAiCodeHygiene.Cli\DotNetAiCodeHygiene.Cli.csproj --configuration Release --no-restore --output $releaseFeed "-p:Version=0.11.1" "-p:BaseOutputPath=$releaseBuildOutput"
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $releaseFeed 'DotNetAiCodeHygiene.Tool.0.11.1.nupkg'))) { throw 'Tier-4 release-update fixture package failed to pack.' }
$releaseToolPath = Join-Path $tierRoot 'release-tools'; $releaseConsumer = Join-Path $tierRoot 'release-consumer'; $releaseCache = Join-Path $tierRoot 'release-cache'; $releaseNugetConfig = Join-Path $tierRoot 'release-consumer\NuGet.config'
New-Item -ItemType Directory -Force $releaseToolPath, $releaseConsumer | Out-Null
$releaseConfigText = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear/><add key="ReleaseFixture" value="$releaseFeed"/><add key="NuGetOfficial" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="ReleaseFixture"><package pattern="DotNetAiCodeHygiene.Tool"/></packageSource><packageSource key="NuGetOfficial"><package pattern="*"/></packageSource></packageSourceMapping></configuration>
"@
Set-Content -LiteralPath $releaseNugetConfig -Value $releaseConfigText -Encoding utf8
$env:NUGET_PACKAGES = $releaseCache
dotnet tool install DotNetAiCodeHygiene.Tool --tool-path $releaseToolPath --configfile $releaseNugetConfig --version 0.11.0
if ($LASTEXITCODE -ne 0) { throw 'Tier-4 autonomous-upgrade fixture failed to install exact 0.11.0.' }
$releaseShim = Join-Path $releaseToolPath 'dotnet-hygiene.exe'
Push-Location $releaseConsumer
try {
    git init -q
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 release consumer git init failed.' }
    Set-Content -LiteralPath ReleaseFixture.csproj -NoNewline -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
    Set-Content -LiteralPath Release.cs -NoNewline -Value 'using System.Collections.Generic; public class ReleaseFixture { }'
    dotnet restore .\ReleaseFixture.csproj | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 release fixture restore failed.' }
    $env:PATH = $releaseToolPath + [IO.Path]::PathSeparator + $pathBeforeUpgrade
    $releaseReport = dotnet hygiene --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $releaseReport.releaseUpdate.status -ne 'updated-and-revalidated' -or $releaseReport.releaseUpdate.previousVersion -ne '0.11.0' -or $releaseReport.releaseUpdate.installedVersion -ne '0.11.1' -or $releaseReport.releaseUpdate.revalidatedStatus -ne 'approval-required') { throw 'Autonomous trusted-feed update did not restart and revalidate the installed command.' }
    if ((Get-Content -LiteralPath Release.cs -Raw) -notmatch 'using System\.Collections\.Generic;') { throw 'Autonomous style formatting applied non-allowlisted IDE0005 and removed an unused import.' }
    $releaseVersion = dotnet hygiene --version
    if ($LASTEXITCODE -ne 0 -or ($releaseVersion -join '').Trim() -ne '0.11.1') { throw 'Autonomous trusted-feed update did not replace the installed shim.' }
}
finally { Pop-Location; $env:PATH = $pathBeforeUpgrade }

# A parent local-tool manifest with a lowercase package key must update itself,
# while a competing PATH tool-path install remains untouched.
$localManifestRoot = Join-Path $tierRoot 'local-manifest-root'; $localConsumer = Join-Path $localManifestRoot 'nested\consumer'; $localCache = Join-Path $tierRoot 'local-package-cache'; $localCliHome = Join-Path $tierRoot 'local-cli-home'
New-Item -ItemType Directory -Force (Join-Path $localManifestRoot '.config'), $localConsumer, $localCache, $localCliHome | Out-Null
Copy-Item -LiteralPath $releaseNugetConfig -Destination (Join-Path $localManifestRoot 'NuGet.config')
$previousCliHome = $env:DOTNET_CLI_HOME
$env:DOTNET_CLI_HOME = $localCliHome
$env:NUGET_PACKAGES = $localCache
$manifestPath = Join-Path $localManifestRoot '.config\dotnet-tools.json'
Push-Location $localManifestRoot
try {
    [IO.File]::WriteAllText($manifestPath, '{"version":1,"isRoot":true,"tools":{}}', [Text.UTF8Encoding]::new($false))
    dotnet tool install DotNetAiCodeHygiene.Tool --local --tool-manifest $manifestPath --configfile (Join-Path $localManifestRoot 'NuGet.config') --version 0.11.0 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Local-manifest 0.11.0 consumer install failed.' }
    $manifestJson = [IO.File]::ReadAllText($manifestPath).Replace('"DotNetAiCodeHygiene.Tool"', '"dotnetaicodehygiene.tool"')
    [IO.File]::WriteAllText($manifestPath, $manifestJson)
}
finally { Pop-Location }
Set-Content -LiteralPath (Join-Path $localConsumer 'NuGet.config') -Value (Get-Content -LiteralPath (Join-Path $localManifestRoot 'NuGet.config') -Raw)
New-Item -ItemType Directory -Force (Join-Path $localConsumer '.config') | Out-Null
$nestedManifestPath = Join-Path $localConsumer '.config\dotnet-tools.json'
Push-Location $localConsumer
try {
    [IO.File]::WriteAllText($nestedManifestPath, '{"version":1,"isRoot":true,"tools":{}}', [Text.UTF8Encoding]::new($false))
    dotnet tool install DotNetAiCodeHygiene.Tool --local --tool-manifest $nestedManifestPath --configfile (Join-Path $localConsumer 'NuGet.config') --version 0.11.0 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Nested local-manifest 0.11.0 consumer install failed.' }
    $nestedJson = [IO.File]::ReadAllText($nestedManifestPath).Replace('"DotNetAiCodeHygiene.Tool"', '"DOTNETAICODEHYGIENE.TOOL"')
    [IO.File]::WriteAllText($nestedManifestPath, $nestedJson)
}
finally { Pop-Location }
Set-Content -LiteralPath (Join-Path $localConsumer 'Local.csproj') -NoNewline -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
Set-Content -LiteralPath (Join-Path $localConsumer 'Local.cs') -NoNewline -Value 'public class Local { }'
dotnet restore (Join-Path $localConsumer 'Local.csproj') | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Local-manifest consumer restore failed.' }
$env:PATH = $toolPath + [IO.Path]::PathSeparator + $pathBeforeUpgrade
Push-Location $localConsumer
try {
    git init -q -b main
    if ($LASTEXITCODE -ne 0) { throw 'Local-manifest consumer git init failed.' }
    $localRaw = dotnet hygiene --output json
    $localExitCode = $LASTEXITCODE
    $localReport = ($localRaw -join "`n") | ConvertFrom-Json
    if ($localExitCode -ne 0 -or $localReport.releaseUpdate.status -ne 'updated-and-revalidated' -or $localReport.releaseUpdate.previousVersion -ne '0.11.0' -or $localReport.releaseUpdate.installedVersion -ne '0.11.1') { throw "Ancestor local manifest did not update and restart its exact installation. Exit=$localExitCode Output=$($localRaw -join ' ') Report: $($localReport | ConvertTo-Json -Depth 16 -Compress)" }
    $updatedNested = [IO.File]::ReadAllText($nestedManifestPath) | ConvertFrom-Json
    $unchangedOuter = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
    if ($updatedNested.tools.PSObject.Properties['DOTNETAICODEHYGIENE.TOOL'].Value.version -ne '0.11.1' -or $unchangedOuter.tools.PSObject.Properties['dotnetaicodehygiene.tool'].Value.version -ne '0.11.0') { throw 'Nested local invocation did not update only the closest matching manifest scope.' }
    $competingVersion = & (Join-Path $toolPath 'dotnet-hygiene.exe') --version
    if ($LASTEXITCODE -ne 0 -or ($competingVersion -join '').Trim() -ne '0.11.0') { throw 'Local-manifest update changed a competing PATH tool-path installation.' }
}
finally { Pop-Location; $env:PATH = $pathBeforeUpgrade; $env:NUGET_PACKAGES = $releaseCache; if ($null -eq $previousCliHome) { Remove-Item Env:\DOTNET_CLI_HOME -ErrorAction SilentlyContinue } else { $env:DOTNET_CLI_HOME = $previousCliHome } }

# Unavailable/offline sources must be reported while the compatible binary
# continues; incompatible profile state must remain untouched and actionable.
$offlineConsumer = Join-Path $tierRoot 'offline-consumer'; $incompatibleConsumer = Join-Path $tierRoot 'incompatible-consumer'
New-Item -ItemType Directory -Force $offlineConsumer, $incompatibleConsumer, (Join-Path $incompatibleConsumer '.hygiene') | Out-Null
@'
<configuration><packageSources><clear /></packageSources></configuration>
'@ | Set-Content -LiteralPath (Join-Path $offlineConsumer 'NuGet.config') -Encoding utf8
$env:PATH = $releaseToolPath + [IO.Path]::PathSeparator + $pathBeforeUpgrade
Push-Location $offlineConsumer
try {
    git init -q
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 offline consumer git init failed.' }
    $offlineReport = dotnet hygiene --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $offlineReport.releaseUpdate.status -ne 'unavailable' -or $offlineReport.status -ne 'approval-required') { throw 'Offline release discovery was not reported while the compatible tool continued bootstrap.' }
}
finally { Pop-Location }
Push-Location $incompatibleConsumer
try {
    git init -q
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 incompatible-profile consumer git init failed.' }
    $unsupportedProfile = '{"schemaVersion":1,"profile":"dotnet-11","version":99}'
    Set-Content -LiteralPath .hygiene\profile.json -NoNewline -Value $unsupportedProfile
    $incompatibleReport = dotnet hygiene --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $incompatibleReport.status -ne 'incomplete' -or $incompatibleReport.lifecycle -ne 'profile-update' -or [string]::IsNullOrWhiteSpace($incompatibleReport.compatibilityError) -or (Get-Content -LiteralPath .hygiene\profile.json -Raw) -cne $unsupportedProfile) { throw 'Incompatible profile was not reported safely or its user data changed.' }
}
finally { Pop-Location; $env:PATH = $pathBeforeUpgrade }

# Inject a late style failure after the first project was formatted. The real
# installed tool must restore sources and managed bootstrap files on failure.
$rollbackConsumer = Join-Path $tierRoot 'rollback-consumer'; $rollbackWrapperProject = Join-Path $tierRoot 'dotnet-wrapper'; $rollbackCounter = Join-Path $tierRoot 'style-call-count.txt'
$rollbackWrapperBin = Join-Path $rollbackWrapperProject 'bin\Release\net11.0'
$dotnetExecutable = (Get-Command dotnet).Source
New-Item -ItemType Directory -Force $rollbackConsumer, (Join-Path $rollbackConsumer 'A'), (Join-Path $rollbackConsumer 'B') | Out-Null
dotnet new console --framework net11.0 --no-restore --output $rollbackWrapperProject | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Tier-4 formatter wrapper project creation failed.' }
$wrapperProjectFile = Get-ChildItem -LiteralPath $rollbackWrapperProject -Filter *.csproj | Select-Object -First 1
(Get-Content -LiteralPath $wrapperProjectFile.FullName -Raw).Replace('<PropertyGroup>', '<PropertyGroup><AssemblyName>dotnet</AssemblyName>') | Set-Content -LiteralPath $wrapperProjectFile.FullName -NoNewline
$wrapperSource = @"
using System.Diagnostics;
string[] cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
string joined = string.Join(" ", cliArgs);
if (joined.Contains("dotnet-format.dll", StringComparison.OrdinalIgnoreCase) && joined.Contains(" style ", StringComparison.OrdinalIgnoreCase))
{
    string counter = Environment.GetEnvironmentVariable("M0018_STYLE_COUNT")!;
    int count = File.Exists(counter) ? int.Parse(File.ReadAllText(counter)) : 0;
    count++;
    File.WriteAllText(counter, count.ToString());
    if (count >= 2) { Console.Error.WriteLine("Injected second-project style failure for rollback validation."); return 42; }
}
var start = new ProcessStartInfo(@"$dotnetExecutable") { UseShellExecute = false };
foreach (string argument in cliArgs) { start.ArgumentList.Add(argument); }
using Process process = Process.Start(start)!;
process.WaitForExit();
return process.ExitCode;
"@
Set-Content -LiteralPath (Join-Path $rollbackWrapperProject 'Program.cs') -Value $wrapperSource
& $dotnetExecutable build $wrapperProjectFile.FullName --configuration Release | Out-Null
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $rollbackWrapperBin 'dotnet.exe'))) { throw 'Tier-4 formatter wrapper build failed.' }
$rollbackSourceA = 'public class A{public void Run(){if(true){System.Console.WriteLine("a");}}}'
$rollbackSourceB = 'public class B{public void Run(){if(true){System.Console.WriteLine("b");}}}'
Set-Content -LiteralPath (Join-Path $rollbackConsumer 'A\A.csproj') -NoNewline -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
Set-Content -LiteralPath (Join-Path $rollbackConsumer 'B\B.csproj') -NoNewline -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
Set-Content -LiteralPath (Join-Path $rollbackConsumer 'A\A.cs') -NoNewline -Value $rollbackSourceA
Set-Content -LiteralPath (Join-Path $rollbackConsumer 'B\B.cs') -NoNewline -Value $rollbackSourceB
Push-Location $rollbackConsumer
try {
    git init -q
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 rollback fixture git init failed.' }
    & $dotnetExecutable restore .\A\A.csproj | Out-Null
    & $dotnetExecutable restore .\B\B.csproj | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 rollback fixture restore failed.' }
    $env:M0018_STYLE_COUNT = $rollbackCounter
    $env:PATH = $rollbackWrapperBin + [IO.Path]::PathSeparator + $releaseToolPath + [IO.Path]::PathSeparator + $pathBeforeUpgrade
    $rollbackReport = & $releaseShim --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $rollbackReport.status -ne 'incomplete' -or $rollbackReport.lifecycle -ne 'bootstrap' -or -not $rollbackReport.rolledBack -or (Get-Content -LiteralPath A\A.cs -Raw).Trim() -cne $rollbackSourceA -or (Get-Content -LiteralPath B\B.cs -Raw).Trim() -cne $rollbackSourceB -or (Test-Path -LiteralPath .hygiene\profile.json) -or (Test-Path -LiteralPath .editorconfig) -or (Test-Path -LiteralPath Directory.Build.props)) { throw 'Tier-4 late formatter failure did not roll back source and managed profile changes.' }
}
finally { Pop-Location; $env:PATH = $pathBeforeUpgrade; Remove-Item Env:\M0018_STYLE_COUNT -ErrorAction SilentlyContinue }

Push-Location $autonomousConsumer
try {
    git init -q -b main
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 autonomous fixture git init failed.' }
    git config user.email hygiene-autonomous@example.invalid
    git config user.name 'Hygiene Autonomous Fixture'
    Set-Content -LiteralPath (Join-Path $autonomousConsumer 'Autonomous.csproj') -NoNewline -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><ItemGroup><Compile Remove="Eligible/**/*.cs;Legacy/**/*.cs" /></ItemGroup></Project>'
@'
/// <summary>A sample type coordinates one operation.</summary>
public class Sample {
    // Keep this note close to the operation it explains.
    public string UserEdit() { return "retained"; }
    public void Run() { if (true) { System.Console.WriteLine("x"); } }
}
'@ | Set-Content -LiteralPath (Join-Path $autonomousConsumer 'Sample.cs')
@'
/// <summary>A second sample type.</summary>
public class Other {
    // This comment explains the second declaration.
    public void Run() { }
}
'@ | Set-Content -LiteralPath (Join-Path $autonomousConsumer 'Other.cs')
New-Item -ItemType Directory -Force (Join-Path $autonomousConsumer 'Eligible'), (Join-Path $autonomousConsumer 'Legacy') | Out-Null
Set-Content -LiteralPath (Join-Path $autonomousConsumer 'Eligible\Eligible.csproj') -NoNewline -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
Set-Content -LiteralPath (Join-Path $autonomousConsumer 'Eligible\Eligible.cs') -NoNewline -Value 'public class Eligible{public int Value()=>1;}'
Set-Content -LiteralPath (Join-Path $autonomousConsumer 'Legacy\Legacy.csproj') -NoNewline -Value '<Project><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" /></Project>'
$legacySource = 'public class Legacy{public int Value()=>1;}'
Set-Content -LiteralPath (Join-Path $autonomousConsumer 'Legacy\Legacy.cs') -NoNewline -Value $legacySource
    dotnet restore (Join-Path $autonomousConsumer 'Eligible\Eligible.csproj') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 eligible SDK project restore failed.' }
    dotnet restore (Join-Path $autonomousConsumer 'Autonomous.csproj') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 autonomous fixture restore failed.' }
    git add Autonomous.csproj Sample.cs Other.cs Eligible/Eligible.csproj Eligible/Eligible.cs Legacy/Legacy.csproj Legacy/Legacy.cs
    git commit -m 'autonomous fixture baseline' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 autonomous fixture baseline commit failed.' }
    $autonomousBase = (git rev-parse HEAD).Trim()
    git update-ref refs/remotes/origin/main $autonomousBase
    git symbolic-ref refs/remotes/origin/HEAD refs/remotes/origin/main
    Set-Content -LiteralPath staged.txt -NoNewline -Value 'staged value'
    git add staged.txt
    Set-Content -LiteralPath staged.txt -NoNewline -Value 'unstaged value'
    Set-Content -LiteralPath user-untracked.txt -NoNewline -Value 'preserve this untracked user file'
    Set-Content -LiteralPath Sample.cs -NoNewline -Value 'public class Sample{public string UserEdit(){return "retained";}}'
@'
/// <summary>An updated second sample type.</summary>
public class Other {
    // This comment explains the second declaration after a user edit.
    public void Run() { if (true) { System.Console.WriteLine("other"); } }
}
'@ | Set-Content -LiteralPath Other.cs
    $autonomousBootstrap = dotnet hygiene --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $autonomousBootstrap.status -ne 'approval-required' -or $autonomousBootstrap.lifecycle -ne 'bootstrap') { throw 'Installed no-argument dotnet hygiene did not bootstrap into an approval-required baseline.' }
    if ($autonomousBootstrap.preHygieneFormatting[0].status -ne 'drift' -or $autonomousBootstrap.changedPaths -notcontains 'Sample.cs') { throw 'Installed bootstrap did not assess original formatting or report the post-hygiene source change.' }
    if (@($autonomousBootstrap.preHygieneFormatting | Where-Object project -like 'Legacy/*').Count -gt 0 -or (Get-Content -LiteralPath (Join-Path $autonomousConsumer 'Legacy\Legacy.cs') -Raw) -cne $legacySource) { throw 'Installed bootstrap assessed or changed the unsupported legacy project.' }
    if ((Get-Content -LiteralPath (Join-Path $autonomousConsumer 'Eligible\Eligible.cs') -Raw) -match 'Eligible\{public') { throw 'Installed bootstrap failed to format the eligible nested SDK-style project.' }
    if (-not (Test-Path -LiteralPath $autonomousBootstrap.reportPath)) { throw 'Installed autonomous bootstrap did not persist its baseline report.' }
    $cachedPaths = @(git diff --cached --name-only)
    $worktreePaths = @(git diff --name-only)
    if ($LASTEXITCODE -ne 0 -or $cachedPaths -notcontains 'staged.txt' -or $cachedPaths -contains 'Sample.cs' -or $worktreePaths -notcontains 'staged.txt' -or $worktreePaths -notcontains 'Sample.cs' -or (Get-Content -LiteralPath staged.txt -Raw) -cne 'unstaged value' -or (Get-Content -LiteralPath user-untracked.txt -Raw) -cne 'preserve this untracked user file' -or (Get-Content -LiteralPath Sample.cs -Raw) -notmatch 'retained') { throw 'Successful bootstrap failed to preserve staged, unstaged, and untracked user work.' }
    $stillPending = dotnet hygiene --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $stillPending.status -ne 'approval-required') { throw 'Installed autonomous entrypoint bypassed its explicit baseline approval boundary.' }
    dotnet hygiene baseline accept --approver 'Isolated Tier-4 fixture approver' --report-sha256 $autonomousBootstrap.reportSha256 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed baseline approval command failed for the isolated authorized fixture.' }
    $autonomousGate = dotnet hygiene --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $autonomousGate.lifecycle -ne 'gate' -or @($autonomousGate.workOrder).Count -eq 0) { throw 'Installed autonomous gate omitted its selected grouped semantic work order.' }
    $groupedFiles = @($autonomousGate.workOrder)
    $multiRuleGroup = @($groupedFiles | Where-Object { @($_.items | Select-Object -ExpandProperty ruleId -Unique).Count -gt 1 })
    $zeroWorkBatches = @($autonomousGate.batches | Where-Object { $_.sampleCount -eq 0 })
    $workBatchHandles = @($groupedFiles | ForEach-Object { $_.items } | Select-Object -ExpandProperty batchHandle -Unique)
    if ($groupedFiles.Count -lt 2 -or $multiRuleGroup.Count -eq 0 -or $zeroWorkBatches.Count -eq 0 -or @($zeroWorkBatches | Where-Object { $workBatchHandles -contains $_.handle }).Count -gt 0) { throw 'Installed work order did not preserve multi-file grouping, independent same-file rules, and zero-work batch identity without phantom items.' }
    foreach ($fileWork in $autonomousGate.workOrder) {
        if ($fileWork.CurrentContentSha256.Length -ne 64 -or [string]::IsNullOrWhiteSpace($fileWork.CurrentContent) -or @($fileWork.items).Count -eq 0) { throw 'Installed work order omitted current source identity/context or selected items.' }
    }
}
finally { Pop-Location }
$env:PATH = $toolPath + [IO.Path]::PathSeparator + $pathBeforeUpgrade
Push-Location $consumer
try {
    git init -q
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 fixture git init failed.' }
    git config user.email hygiene-tier4@example.invalid
    git config user.name 'Hygiene Tier 4'
    Set-Content -NoNewline -Path Consumer.csproj -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
    Set-Content -NoNewline -Path Sample.cs -Value 'public class Sample{private int value;public int Value(){return this.value;}}'
    dotnet hygiene --version
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene --version failed.' }
    $version = dotnet hygiene --version
    if ($LASTEXITCODE -ne 0 -or ($version -join '').Trim() -ne '0.11.0') { throw 'Installed hygiene version does not match 0.11.0.' }
    dotnet hygiene help --agent | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene help --agent failed.' }
    $bootstrap = dotnet hygiene bootstrap --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $bootstrap.status -ne 'approval-required' -or $bootstrap.lifecycle -ne 'bootstrap' -or @($bootstrap.remainingProfileFindings).Count -ne 0 -or [string]::IsNullOrWhiteSpace($bootstrap.reportSha256)) { throw "Installed bootstrap did not prepare a clean report-bound approval request. Output: $($bootstrap | ConvertTo-Json -Depth 12 -Compress)" }
    dotnet hygiene rules disable style.braces.required | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed braces rule could not be disabled.' }
    $editorAfterDisable = Get-Content -Raw .editorconfig
    if ($editorAfterDisable -match '# hygiene rule: style\.braces\.required') { throw 'Disabling braces retained its hygiene-owned projection.' }
    dotnet hygiene rules enable style.braces.required | Out-Null
    if ($LASTEXITCODE -ne 0 -or (Get-Content -Raw .editorconfig) -notmatch '# hygiene rule: style\.braces\.required') { throw 'Enabling braces did not restore its projection.' }
    dotnet hygiene rules disable format.csharp.roslyn | Out-Null
    $disabledFormat = dotnet hygiene format --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $disabledFormat.changedCount -ne 0 -or $disabledFormat.selectedRuleIds.Count -ne 0) { throw 'Disabled format rule still participated in format.' }
    dotnet hygiene rules enable format.csharp.roslyn | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed format rule could not be enabled.' }
    # Bootstrap now performs authoritative formatting itself; inject a fresh
    # unformatted source state to verify the manual diagnostic command too.
    Set-Content -NoNewline -Path Sample.cs -Value 'public class Sample{private int value;public int Value(){return this.value;}}'
    $formatBefore = Get-Content -Raw Sample.cs
    $formatCheck = dotnet hygiene format --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $formatCheck.changedCount -lt 1 -or ($formatCheck.selectedRuleIds -join ',') -ne 'format.csharp.roslyn') { throw 'Installed hygiene format --check did not report pending changes and selected rule.' }
    if ((Get-Content -Raw Sample.cs) -cne $formatBefore) { throw 'Installed format --check modified source.' }
    dotnet hygiene format | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene format mutation failed.' }
    $formatted = Get-Content -Raw Sample.cs
    if ($formatted -ceq $formatBefore) { throw 'Installed format mutation did not change source.' }
    $formatClean = dotnet hygiene format --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $formatClean.changedCount -ne 0) { throw 'Installed format was not idempotent.' }
    $normalizeBefore = Get-Content -Raw Sample.cs
    dotnet hygiene rules disable style.qualification.this.unnecessary | Out-Null
    $thisDisabled = dotnet hygiene normalize --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or ($thisDisabled.selectedRuleIds -contains 'style.qualification.this.unnecessary')) { throw 'Disabled this qualification rule still participated in normalize.' }
    dotnet hygiene rules enable style.qualification.this.unnecessary | Out-Null
    $normalizeCheck = dotnet hygiene normalize --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $normalizeCheck.changedCount -lt 1) { throw 'Installed hygiene normalize --check did not report pending changes.' }
    if ((Get-Content -Raw Sample.cs) -cne $normalizeBefore) { throw 'Installed normalize --check modified source.' }
    dotnet hygiene normalize | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene normalize mutation failed.' }
    $normalized = Get-Content -Raw Sample.cs
    if ($normalized -ceq $normalizeBefore) { throw 'Installed normalize mutation did not change source.' }
    $normalizeClean = dotnet hygiene normalize --check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $normalizeClean.changedCount -ne 0) { throw 'Installed normalize was not idempotent.' }
    $profilePath = Join-Path $consumer '.hygiene/profile.json'
    Set-Content -LiteralPath .editorconfig -Encoding utf8 -Value "root = true`n`n# consumer-owned text`n[*.md]`ntrim_trailing_whitespace = true`n`n<!-- hygiene profile:begin -->`n[*.cs]`ncsharp_prefer_braces = true`ndotnet_diagnostic.IDE0011.severity = error`ndotnet_style_require_accessibility_modifiers = always`ndotnet_diagnostic.IDE0040.severity = error`n<!-- hygiene profile:end -->"
    Set-Content -LiteralPath $profilePath -Encoding utf8 -Value "{`n  `"schemaVersion`": 1,`n  `"profile`": `"dotnet-11`",`n  `"version`": 1`n}"
    $update = dotnet hygiene update --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $update.findingCount -ne 0 -or $update.changedPaths.Count -eq 0) { throw 'Installed update did not migrate the v1 profile.' }
    if ((Get-Content -Raw $profilePath) -notmatch '"version": 2') { throw 'Installed profile update did not persist v2.' }
    $migratedEditor = Get-Content -Raw .editorconfig
    if ($migratedEditor -notmatch '# hygiene rule: style\.braces\.required' -or $migratedEditor -notmatch '# hygiene rule: style\.accessibility\.explicit') { throw 'Installed profile migration did not regenerate rule-owned EditorConfig contributions.' }
    if ($migratedEditor -notmatch '# consumer-owned text\r?\n\[\*\.md\]\r?\ntrim_trailing_whitespace = true') { throw 'Installed profile migration did not preserve user EditorConfig content.' }
    $updateAgain = dotnet hygiene update --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $updateAgain.findingCount -ne 0 -or $updateAgain.changedPaths.Count -ne 0) { throw 'Installed update was not idempotent after migration.' }
    $rules = dotnet hygiene rules --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or -not (($rules.rules | Where-Object id -eq 'profile.dotnet.analysis.required').configurable -eq $false)) { throw 'Installed mandatory profile rule is missing or configurable.' }
    $capabilitiesValid = $rules.rules.Count -eq 24 `
        -and ($rules.rules | Where-Object id -eq 'format.csharp.roslyn').capabilities.format `
        -and ($rules.rules | Where-Object id -eq 'style.braces.required').capabilities.normalize `
        -and ($rules.rules | Where-Object id -eq 'style.accessibility.explicit').capabilities.normalize `
        -and ($rules.rules | Where-Object id -eq 'style.object-creation.target-typed').capabilities.normalize `
        -and ($rules.rules | Where-Object id -eq 'style.default.literal').capabilities.normalize `
        -and ($rules.rules | Where-Object id -eq 'style.cast.redundant').capabilities.normalize `
        -and ($rules.rules | Where-Object id -eq 'style.interpolation.redundant').capabilities.normalize `
        -and ($rules.rules | Where-Object id -eq 'style.names.inferred').capabilities.normalize `
        -and ($rules.rules | Where-Object id -eq 'style.usings.organize').capabilities.normalize
    if (-not $capabilitiesValid) { throw 'Installed rule capabilities are incomplete.' }
    if (-not ($rules.rules | Where-Object id -eq 'docs.summary.quality.review')) { throw 'Installed rule catalog omitted summary quality review.' }
    if (-not ($rules.rules | Where-Object id -eq 'architecture.boringness.review')) { throw 'Installed rule catalog omitted BORINGness review.' }
    $newRuleIds = @('readability.type-responsibility.review', 'tests.quality.review', 'comments.intent.review')
    if (@($rules.rules | Where-Object { $_.id -in $newRuleIds }).Count -ne 3) { throw 'Installed rule catalog omitted an M0017 review rule.' }
    $visualRule = $rules.rules | Where-Object id -eq 'readability.control-flow.visual-block'
    if ($visualRule.version -ne 2 -or $visualRule.classification -ne 'review-candidate') { throw 'Installed visual-block rule did not expose its v2 candidate contract.' }
    $reviewSource = @('// Copyright 2026 Example. All rights reserved.', '// MIT License', '/// <summary>Sample API</summary>', 'public class Sample { public void Run() { int value = 1; if (value > 0) { System.Console.WriteLine(value); } } }') + @(0..31 | ForEach-Object { "/// <summary>A useful API description for sample $_.</summary>`npublic class Reviewed$_ { }" })
    Set-Content -NoNewline -Path Sample.cs -Value ($reviewSource -join "`n")
    dotnet hygiene format | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed formatter failed on the visual-block fixture.' }
    dotnet hygiene normalize | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed normalizer failed on the visual-block fixture.' }
    $visualCheck = dotnet hygiene check --review-all --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or -not ($visualCheck.findings | Where-Object { $_.ruleId -eq 'readability.control-flow.visual-block' -and $_.classification -eq 'review-candidate' })) { throw 'Installed visual-block candidate was not retained after format/normalize.' }
    git add Sample.cs
    git commit -m 'review fixture baseline' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Tier-4 baseline commit failed.' }
    $baseCommit = (& git rev-parse HEAD).Trim()
    Add-Content -Path Sample.cs -Value @'

namespace Xunit { [System.AttributeUsage(System.AttributeTargets.Method)] public sealed class FactAttribute : System.Attribute { } }
/// <summary>An additional API description for the PR gate.</summary>
public class ReviewedAdditional
{
    // Keep this guard aligned with the request contract.
    public void Run()
    {
        var key = 1; // Keep the source statement and trailing review separate.
        // Avoid the guarded operation when the key is empty.
        if (key > 0) { System.Console.WriteLine(key); }
    }
    [Xunit.Fact]
    public void ChangedBehaviorHasRegressionTest() { System.Console.WriteLine("observable"); }
}
'@
    $bareCheck = dotnet hygiene check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or @($bareCheck.reviewBatches | Where-Object sampleCount -ne 0).Count -ne 0) { throw 'Bare installed check must schedule zero semantic review.' }
    $check = dotnet hygiene check --base $baseCommit --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Installed hygiene check failed.' }
    if (-not ($check.findings | Where-Object ruleId -eq 'docs.summary.required')) { throw 'Installed check did not report a missing API summary.' }
    if (-not ($check.findings | Where-Object ruleId -eq 'docs.text.sentence')) { throw 'Installed check did not report missing summary punctuation.' }
    $quality = $check.reviewBatches | Where-Object ruleId -eq 'docs.summary.quality.review'
    $german = $check.reviewBatches | Where-Object ruleId -eq 'docs.summary.language.german.review'
    if (-not $quality -or $quality.ruleVersion -ne 6 -or $quality.sampleCount -lt 1 -or $quality.sampleCount -gt 5 -or -not $german -or $german.ruleVersion -ne 4) { throw 'Installed PR-base check did not emit bounded independent v6 quality and v4 German review batches.' }
    if (-not ($check.reviewBatches | Where-Object ruleId -eq 'architecture.boringness.review')) { throw 'Installed check omitted BORINGness review.' }
    if (@($check.reviewBatches | Where-Object { $_.ruleId -in $newRuleIds }).Count -ne 3) { throw 'Installed PR-base check omitted an M0017 review batch.' }
    $typeReview = $check.reviewBatches | Where-Object ruleId -eq 'readability.type-responsibility.review'
    $testReview = $check.reviewBatches | Where-Object ruleId -eq 'tests.quality.review'
    $commentReview = $check.reviewBatches | Where-Object ruleId -eq 'comments.intent.review'
    if ($typeReview.sampleCount -lt 1 -or $testReview.sampleCount -lt 1 -or $commentReview.sampleCount -lt 1) { throw 'Installed M0017 rules did not select changed type, test, and comment subjects.' }
    if (($testReview.questions.Count -ne 3) -or $testReview.items[0].summary -ne '') { throw 'Installed test-quality review did not retain its fixed questions and empty non-applicable summary.' }
    $newAccepted = dotnet hygiene review accept $testReview.handle $testReview.items[0].id --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $newAccepted.acceptedCount -ne 1 -or $newAccepted.ruleId -ne 'tests.quality.review') { throw 'Installed test-quality review acceptance failed.' }
    $commentExpanded = dotnet hygiene review expand $commentReview.handle --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $commentExpanded.reviewBatch.ruleId -ne 'comments.intent.review' -or $commentExpanded.reviewBatch.items.Count -lt 4) { throw 'Installed comment-intent expansion failed or omitted comment groups.' }
    $commentHandoff = dotnet hygiene review handoff $commentReview.handle
    $commentHandoffText = [string]::Join([Environment]::NewLine, @($commentHandoff))
    if ($LASTEXITCODE -ne 0 -or $commentHandoffText -notmatch 'Review handoff written: ') { throw 'Installed comment-intent handoff failed.' }
    $commentRequestPath = [regex]::Match($commentHandoffText, '(?m)^Review handoff written: (.+)$').Groups[1].Value.Trim()
    $commentRequest = Get-Content -LiteralPath $commentRequestPath -Raw | ConvertFrom-Json
    $commentSource = $commentRequest.sources | Where-Object path -eq 'Sample.cs'
    if (-not $commentSource -or $commentSource.content -notmatch 'Copyright 2026' -or $commentSource.content -notmatch 'MIT License' -or $commentSource.content -notmatch 'trailing review separate' -or $commentSource.content -notmatch 'Avoid the guarded operation') { throw 'Installed comment-intent expansion did not preserve current source context or ordinary legal comments.' }
    $typeHandoff = dotnet hygiene review handoff $typeReview.handle
    $typeHandoffText = [string]::Join([Environment]::NewLine, @($typeHandoff))
    if ($LASTEXITCODE -ne 0 -or $typeHandoffText -notmatch 'Review handoff written: ') { throw 'Installed type-responsibility handoff failed.' }
    $typeRequestPath = [regex]::Match($typeHandoffText, '(?m)^Review handoff written: (.+)$').Groups[1].Value.Trim()
    $typeRequest = Get-Content -LiteralPath $typeRequestPath -Raw | ConvertFrom-Json
    if ($typeRequest.reviewerClass -ne 'planner' -or $typeRequest.rule.id -ne 'readability.type-responsibility.review') { throw 'Installed type-responsibility handoff contract is invalid.' }
    $internalFields = 'samplingTicket|stateEpoch|repositorySeed|hazard|threshold|evaluationFingerprint|aggregateEvidence'
    if (($check | ConvertTo-Json -Depth 32) -match $internalFields) { throw 'Installed check exposed internal sampling metadata.' }
    $firstQualityItem = $quality.items[0].id
    $accepted = dotnet hygiene review accept $quality.handle $firstQualityItem --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $accepted.acceptedCount -ne 1 -or $accepted.ruleId -ne 'docs.summary.quality.review') { throw 'Installed explicit review accept failed.' }
    $checkAgain = dotnet hygiene check --base $baseCommit --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Installed follow-up hygiene check failed.' }
    $qualityAgain = $checkAgain.reviewBatches | Where-Object ruleId -eq 'docs.summary.quality.review'
    $germanAgain = $checkAgain.reviewBatches | Where-Object ruleId -eq 'docs.summary.language.german.review'
    $boringness = $checkAgain.reviewBatches | Where-Object ruleId -eq 'architecture.boringness.review'
    $acceptedAll = dotnet hygiene review accept $qualityAgain.handle --all --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $acceptedAll.acceptedCount -ne $qualityAgain.sampleCount) { throw 'Installed review accept --all did not consume exactly the current normal sample.' }
    if (-not ($german.questions[0].text -match 'natural, comprehensible German')) { throw 'Installed German review rubric is incorrect.' }
    $expanded = dotnet hygiene review expand $qualityAgain.handle --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $expanded.reviewBatch.mode -ne 'expanded' -or $expanded.reviewBatch.ruleId -ne 'docs.summary.quality.review') { throw 'Installed summary review expansion failed or selected the wrong batch.' }
    $boringExpanded = dotnet hygiene review expand $boringness.handle --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $boringExpanded.reviewBatch.items.Count -lt 32) { throw 'Installed BORINGness expansion did not retain the complete type population.' }
    $handoff = dotnet hygiene review handoff $germanAgain.handle
    $handoffText = [string]::Join([Environment]::NewLine, @($handoff))
    if ($LASTEXITCODE -ne 0 -or $handoffText -notmatch 'Review handoff written: ') { throw 'Installed German review handoff failed.' }
    $plannerHandoff = dotnet hygiene review handoff $boringness.handle
    $plannerHandoffText = [string]::Join([Environment]::NewLine, @($plannerHandoff))
    if ($LASTEXITCODE -ne 0 -or $plannerHandoffText -notmatch 'Review handoff written: ') { throw 'Installed BORINGness planner handoff failed.' }
    $plannerPath = [regex]::Match($plannerHandoffText, '(?m)^Review handoff written: (.+)$').Groups[1].Value.Trim()
    $plannerRequest = Get-Content -LiteralPath $plannerPath -Raw | ConvertFrom-Json
    if ($plannerRequest.reviewerClass -ne 'planner' -or $plannerRequest.populationCount -lt 32 -or $plannerRequest.PSObject.Properties.Name -match 'result|ticket|hazard') { throw 'Installed BORINGness planner handoff contract is invalid.' }
    dotnet hygiene rules disable docs.summary.language.german.review | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Installed German rule could not be disabled.' }
    $qualityOnly = dotnet hygiene check --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $qualityOnly.reviewBatches.Count -ne 5 -or $qualityOnly.reviewBatches.ruleId -contains 'docs.summary.language.german.review' -or -not ($qualityOnly.reviewBatches | Where-Object ruleId -eq 'docs.summary.quality.review') -or -not ($qualityOnly.reviewBatches | Where-Object ruleId -eq 'architecture.boringness.review') -or -not ($qualityOnly.reviewBatches | Where-Object ruleId -eq 'comments.intent.review')) { throw 'Disabling the installed German rule changed or removed other semantic-review rules.' }

    # Prove the complete lifecycle terminal outcome from a clean, committed main-branch consumer.
    $completeConsumer = Join-Path $tierRoot 'complete-consumer'
    New-Item -ItemType Directory -Force $completeConsumer | Out-Null
    $previousSkipUpdate = $env:DOTNET_HYGIENE_SKIP_UPDATE
    $env:DOTNET_HYGIENE_SKIP_UPDATE = '1'
    Push-Location $completeConsumer
    try {
        git init -q -b main
        if ($LASTEXITCODE -ne 0) { throw 'Tier-4 complete consumer git init failed.' }
        git config user.email hygiene-complete@example.invalid
        git config user.name 'Hygiene Complete Fixture'
        Set-Content -LiteralPath Empty.csproj -NoNewline -Value '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
        Set-Content -LiteralPath Empty.cs -NoNewline -Value 'public class Empty { public int Value => 1; }'
        dotnet restore .\Empty.csproj | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Tier-4 complete consumer restore failed.' }
        $pendingBaseline = dotnet hygiene --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $pendingBaseline.status -ne 'approval-required') { throw 'Clean consumer bootstrap did not require explicit approval.' }
        Set-Content -LiteralPath Empty.cs -NoNewline -Value 'public class Empty { public int Value => 2; }'
        dotnet hygiene baseline accept --approver 'Isolated completion fixture approver' --report-sha256 $pendingBaseline.reportSha256 | Out-Null
        if ($LASTEXITCODE -eq 0) { throw 'Installed tool accepted a pending baseline after relevant source bytes changed.' }
        $pendingBaseline = dotnet hygiene bootstrap --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $pendingBaseline.status -ne 'approval-required') { throw 'Explicit bootstrap did not refresh stale installed-consumer baseline evidence.' }
        Remove-Item -LiteralPath .hygiene\.state\baseline-approval.json
        $markerOnly = dotnet hygiene --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $markerOnly.status -ne 'incomplete' -or $markerOnly.lifecycle -ne 'baseline') { throw 'A marker without an accepted baseline record entered the installed-consumer gate.' }
        $pendingBaseline = dotnet hygiene bootstrap --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $pendingBaseline.status -ne 'approval-required') { throw 'Installed explicit bootstrap did not restore a report-bound approval record.' }
        git add -A
        git commit -m 'complete consumer baseline fixture' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Tier-4 complete consumer commit failed.' }
        $completeBase = (git rev-parse HEAD).Trim()
        git update-ref refs/remotes/origin/main $completeBase
        git symbolic-ref refs/remotes/origin/HEAD refs/remotes/origin/main
        dotnet hygiene baseline accept --approver 'Isolated completion fixture approver' --report-sha256 $pendingBaseline.reportSha256 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Clean consumer baseline approval failed.' }
        $completeReport = dotnet hygiene --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $completeReport.status -ne 'complete' -or $completeReport.lifecycle -ne 'gate' -or [string]::IsNullOrWhiteSpace($completeReport.gate) -or [string]::IsNullOrWhiteSpace($completeReport.runId)) { throw 'Clean installed consumer did not return a truthful complete gate report.' }
        Set-Content -LiteralPath Empty.cs -NoNewline -Value 'public class Empty { public int Value => 3; }'
        $ordinaryEdit = dotnet hygiene --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $ordinaryEdit.lifecycle -ne 'gate') { throw 'Accepted baseline incorrectly froze ordinary source edits.' }
        Set-Content -LiteralPath .hygiene\profile.json -NoNewline -Value '{"schemaVersion":1,"profile":"dotnet-11","version":1}'
        $profileDrift = dotnet hygiene --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $profileDrift.status -ne 'approval-required' -or $profileDrift.lifecycle -ne 'profile-update' -or @($profileDrift.changedPaths).Count -eq 0) { throw 'Autonomous profile drift did not prepare a materially changed approval-required baseline.' }
        $profileDriftPending = dotnet hygiene --output json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $profileDriftPending.status -ne 'approval-required') { throw 'A materially changed profile baseline was reported complete before external approval.' }
    }
    finally {
        Pop-Location
        if ($null -eq $previousSkipUpdate) { Remove-Item Env:\DOTNET_HYGIENE_SKIP_UPDATE -ErrorAction SilentlyContinue } else { $env:DOTNET_HYGIENE_SKIP_UPDATE = $previousSkipUpdate }
    }
    if ($env:M0020_VALIDATION_INJECT_CONSUMER_FAILURE -eq '1') { throw 'Injected M0020 late consumer-stage failure after all installed scenarios for receipt-recovery validation.' }
}
finally {
    Pop-Location
    $env:NUGET_PACKAGES = $previousNugetPackages
    $env:PATH = $previousPath
    Remove-Item -LiteralPath $tierRoot -Recurse -Force
}
