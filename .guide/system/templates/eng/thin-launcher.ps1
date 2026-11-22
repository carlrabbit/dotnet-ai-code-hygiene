$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/Project.Engineering'

dotnet run --project $project -- @args
exit $LASTEXITCODE
