[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('M0024')][string] $Milestone
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$reviewPath = Join-Path $repositoryRoot ".review\REV-$Milestone-COMPLETION.md"
if (-not (Test-Path -LiteralPath $reviewPath -PathType Leaf)) {
    Write-Error "Required independent review record is missing: $reviewPath"
    exit 1
}

$content = Get-Content -LiteralPath $reviewPath -Raw
function Read-ReviewField([string] $Name) {
    $match = [regex]::Match($content, "(?im)^\s*-\s*$([regex]::Escape($Name))\s*:\s*(.+?)\s*$")
    if ($match.Success) { return $match.Groups[1].Value.Trim() }
    return ''
}

$decision = Read-ReviewField 'Decision'
$reviewer = Read-ReviewField 'Reviewer identity'
$reviewDate = Read-ReviewField 'Decision date'
if ($decision -ne 'approved' -or [string]::IsNullOrWhiteSpace($reviewer) -or $reviewer -match '^(pending|unassigned|unknown)$' -or
    [string]::IsNullOrWhiteSpace($reviewDate) -or $reviewDate -match '^(pending|unknown)$') {
    Write-Error "REV-$Milestone-COMPLETION is pending or incomplete; independent human approval has not been recorded."
    exit 1
}

if (-not [regex]::IsMatch($reviewDate, '^\d{4}-\d{2}-\d{2}$') -or
    -not [regex]::IsMatch($content, '(?im)^## Reviewer rationale\s*$') -or
    -not [regex]::IsMatch($content, '(?im)^## Decision evidence\s*$')) {
    Write-Error "REV-$Milestone-COMPLETION is missing a dated reviewer rationale or evidence section."
    exit 1
}

$rationale = [regex]::Match($content, '(?ims)^## Reviewer rationale\s*\r?\n(?<body>.*?)(?=^## |\z)').Groups['body'].Value.Trim()
$evidence = [regex]::Match($content, '(?ims)^## Decision evidence\s*\r?\n(?<body>.*?)(?=^## |\z)').Groups['body'].Value.Trim()
if ($rationale.Length -lt 30 -or $evidence.Length -lt 30) {
    Write-Error "REV-$Milestone-COMPLETION does not contain substantive reviewer rationale and evidence."
    exit 1
}

Write-Output "PASS REV-$Milestone-COMPLETION approved by $reviewer on $reviewDate."
