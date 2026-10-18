[CmdletBinding(DefaultParameterSetName = 'Aggregate')]
param(
    [Parameter(ParameterSetName = 'Plan')][switch] $PlanJson,
    [Parameter(ParameterSetName = 'Shard', Mandatory)][string] $Shard,
    [Parameter(ParameterSetName = 'Verify', Mandatory)][switch] $Verify,
    [Parameter(ParameterSetName = 'Aggregate')][switch] $Fresh,
    [Parameter(ParameterSetName = 'Shard')][string] $TestFilter
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ValidationStages.ps1')
$previousCold = $env:DOTNET_HYGIENE_VALIDATION_COLD
if ($Fresh) { $env:DOTNET_HYGIENE_VALIDATION_COLD = '1' }
try { Invoke-ValidationStages -PlanJson:$PlanJson -Shard $Shard -Verify:$Verify -Fresh:$Fresh -TestFilter $TestFilter }
finally {
    if ($null -eq $previousCold) { Remove-Item Env:\DOTNET_HYGIENE_VALIDATION_COLD -ErrorAction SilentlyContinue }
    else { $env:DOTNET_HYGIENE_VALIDATION_COLD = $previousCold }
}
