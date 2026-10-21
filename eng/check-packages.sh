#!/usr/bin/env bash
# Checks the packed output: the expected number of packages and symbol packages, all sharing one version.
set -euo pipefail

directory="${1:?usage: check-packages.sh <directory> <expected-count> [version-regex]}"
expected="${2:?usage: check-packages.sh <directory> <expected-count> [version-regex]}"
version_pattern="${3:-}"

shopt -s nullglob
packages=("$directory"/*.nupkg)
symbols=("$directory"/*.snupkg)

if [[ ${#packages[@]} -ne $expected ]]; then
  echo "::error::Expected $expected packages, found ${#packages[@]}."
  exit 1
fi

if [[ ${#symbols[@]} -ne $expected ]]; then
  echo "::error::Expected $expected symbol packages, found ${#symbols[@]}."
  exit 1
fi

mapfile -t versions < <(
  for package in "${packages[@]}"; do
    basename "$package" .nupkg | sed -E 's/^WalterEgo(\.[A-Za-z]+)*\.([0-9].*)$/\2/'
  done | sort -u
)

if [[ ${#versions[@]} -ne 1 ]]; then
  echo "::error::The packages do not share one version: ${versions[*]}"
  exit 1
fi

if [[ -n "$version_pattern" && ! "${versions[0]}" =~ $version_pattern ]]; then
  echo "::error::Version ${versions[0]} does not match $version_pattern."
  exit 1
fi

echo "$expected packages and $expected symbol packages, version ${versions[0]}."
