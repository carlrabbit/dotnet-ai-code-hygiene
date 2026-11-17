$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$selfCheck = & dotnet .\src\DotNetAiCodeHygiene.Cli\bin\Release\net11.0\DotNetAiCodeHygiene.Cli.dll check --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Repository self-host hygiene check failed.' }
$portfolioIds = @('architecture.abstraction.necessity','architecture.indirection.value','architecture.responsibility.cohesion','architecture.boundary.ownership','architecture.authority.uniqueness','architecture.lifecycle.binding','architecture.mechanism.explicitness','behavior.error.contract','behavior.validation.boundary','behavior.state.consistency','behavior.resource.ownership','tests.assertion.effectiveness','tests.change-contract.coverage','tests.isolation.coupling','docs.summary.quality.review','docs.summary.language.german.review','comments.intent.review')
$catalog = & dotnet .\src\DotNetAiCodeHygiene.Cli\bin\Release\net11.0\DotNetAiCodeHygiene.Cli.dll rules --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Repository self-host rule catalog could not be loaded.' }
$portfolioRules = @($catalog.rules | Where-Object { $_.id -in $portfolioIds })
$germanRule = $portfolioRules | Where-Object id -eq 'docs.summary.language.german.review'
$portfolioBatches = @($selfCheck.reviewBatches | Where-Object { $_.ruleId -in $portfolioIds })
if ($portfolioRules.Count -ne 17 -or $germanRule.enabled -ne $false -or (@($portfolioBatches | ForEach-Object { $_.sampleCount } | Measure-Object -Sum).Sum -gt 10)) { throw 'Repository self-host check did not expose the 17-rule portfolio, disabled German policy, and shared ten-obligation ceiling.' }
if (@($selfCheck.reviewBatches | Where-Object { $_.ruleId -in @('architecture.boringness.review','readability.type-responsibility.review','tests.quality.review') }).Count -ne 0) { throw 'Repository self-host check exposed retired semantic rule identities.' }
Write-Output 'Repository M0025 self-host behavior passed.'
$diffBase = (& git merge-base HEAD origin/main | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or -not $diffBase) { throw 'Could not identify origin/main merge base for the repository diff check.' }
git diff --check $diffBase --
if ($LASTEXITCODE -ne 0) { throw 'Repository diff whitespace check failed.' }
