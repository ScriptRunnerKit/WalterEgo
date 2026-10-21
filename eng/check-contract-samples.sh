#!/usr/bin/env bash
# A pull request that changes a contract path must also change samples/, unless a maintainer applied no-sample-impact.
set -euo pipefail

base="${BASE_SHA:?BASE_SHA is required}"
head="${HEAD_SHA:?HEAD_SHA is required}"
accepted="${HAS_NO_SAMPLE_IMPACT_LABEL:-false}"
paths_file="$(dirname "$0")/contract-paths.txt"

mapfile -t patterns < <(grep -vE '^[[:space:]]*(#|$)' "$paths_file")
mapfile -t changed < <(git diff --name-only "$base...$head")

contract_changes=()
samples_changed=false
for file in "${changed[@]}"; do
  if [[ "$file" == samples/* ]]; then
    samples_changed=true
  fi
  for pattern in "${patterns[@]}"; do
    # Unquoted on purpose: the right-hand side is a glob, and * also matches /.
    if [[ "$file" == $pattern ]]; then
      contract_changes+=("$file")
      break
    fi
  done
done

if [[ ${#contract_changes[@]} -eq 0 ]]; then
  echo "No contract change."
  exit 0
fi

echo "Contract changes:"
printf '  %s\n' "${contract_changes[@]}"

if [[ "$samples_changed" == true ]]; then
  echo "samples/ was updated in the same pull request."
  exit 0
fi

if [[ "$accepted" == true ]]; then
  echo "Accepted without sample changes: the no-sample-impact label is applied."
  exit 0
fi

echo "::error::The contract changed but samples/ did not. Update the affected samples, or a maintainer can apply the no-sample-impact label."
exit 1
