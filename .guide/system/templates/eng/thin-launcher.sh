#!/usr/bin/env sh
set -eu

root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"

exec dotnet run   --project "$root/src/Project.Engineering"   -- "$@"
