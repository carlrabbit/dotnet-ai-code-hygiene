$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/PROJECT.Engineering'

dotnet run --project $project -- review request @args
exit $LASTEXITCODE
