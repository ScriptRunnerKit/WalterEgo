#!/usr/bin/env bash
# Fails when the pull request title, its description or one of its commit messages matches an internal-reference pattern.
set -euo pipefail

base="${BASE_SHA:?BASE_SHA is required}"
head="${HEAD_SHA:?HEAD_SHA is required}"
patterns_file="$(dirname "$0")/internal-references.patterns"

mapfile -t patterns < <(grep -vE '^[[:space:]]*(#|$)' "$patterns_file")

status=0

check() {
  local source="$1" text="$2" pattern match
  for pattern in "${patterns[@]}"; do
    if match="$(grep -m1 -oP -- "$pattern" <<<"$text")"; then
      echo "::error::$source references internal material: '${match%%$'\n'*}'"
      status=1
    fi
  done
}

check "The pull request title" "${PR_TITLE:-}"
check "The pull request description" "${PR_BODY:-}"

while IFS= read -r commit; do
  check "Commit ${commit:0:12}" "$(git log -1 --format=%B "$commit")"
done < <(git rev-list "$base..$head")

if [[ $status -eq 0 ]]; then
  echo "No internal references in the pull request title, description or commit messages."
fi
exit $status
