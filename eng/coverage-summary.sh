#!/usr/bin/env bash
# Writes the line and branch coverage of each Cobertura report to the job summary.
set -euo pipefail

directory="${1:?usage: coverage-summary.sh <results-directory>}"
summary="${GITHUB_STEP_SUMMARY:-/dev/stdout}"

shopt -s nullglob globstar
reports=("$directory"/**/*.cobertura.xml)

if [[ ${#reports[@]} -eq 0 ]]; then
  echo "::error::No Cobertura report found in $directory."
  exit 1
fi

percent() {
  awk -v rate="$1" 'BEGIN { printf "%.1f%%", rate * 100 }'
}

{
  echo "### Coverage"
  echo
  echo "| Packages | Lines | Branches |"
  echo "|---|---|---|"
  for report in "${reports[@]}"; do
    root="$(grep -m1 -oE '<coverage [^>]*>' "$report")"
    line_rate="$(sed -E 's/.*line-rate="([0-9.]+)".*/\1/' <<<"$root")"
    branch_rate="$(sed -E 's/.*branch-rate="([0-9.]+)".*/\1/' <<<"$root")"
    packages="$(grep -oE '<package [^>]*name="[^"]+"' "$report" | sed -E 's/.*name="([^"]+)"/\1/' | sort -u | paste -sd ',' - || true)"
    echo "| ${packages:-(no instrumented code)} | $(percent "$line_rate") | $(percent "$branch_rate") |"
  done
} >>"$summary"
