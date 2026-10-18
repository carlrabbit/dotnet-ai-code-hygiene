$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$selfCheck = & dotnet .\src\DotNetAiCodeHygiene.Cli\bin\Release\net11.0\DotNetAiCodeHygiene.Cli.dll check --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Repository self-host hygiene check failed.' }
$qualityBatch = $selfCheck.reviewBatches | Where-Object ruleId -eq 'docs.summary.quality.review'
$germanBatch = $selfCheck.reviewBatches | Where-Object ruleId -eq 'docs.summary.language.german.review'
if (-not $qualityBatch -or $germanBatch -or -not ($selfCheck.reviewBatches | Where-Object ruleId -eq 'architecture.boringness.review')) { throw 'Repository self-host check did not retain the German disable, quality review, and BORINGness rule behavior.' }
Write-Output 'Repository M0006 self-host behavior passed.'
