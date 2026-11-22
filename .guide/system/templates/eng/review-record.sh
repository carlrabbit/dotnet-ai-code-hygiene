#!/usr/bin/env sh
set -eu

root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
project="$root/src/PROJECT.Engineering"

exec dotnet run \
  --project "$project" \
  -- review record "$@"
