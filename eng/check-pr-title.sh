#!/usr/bin/env bash
# Squash merges turn the pull request title into the commit message, so it must be a Conventional Commit.
set -euo pipefail

title="${PR_TITLE:?PR_TITLE is required}"
pattern='^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\([a-z0-9][a-z0-9.-]*\))?!?: [^ ].*$'

if [[ ! "$title" =~ $pattern ]]; then
  echo "::error::The pull request title is not a Conventional Commit: '$title'"
  echo "Expected '<type>(<optional scope>): <description>' with one of: build, chore, ci, docs, feat, fix, perf, refactor, revert, style, test."
  exit 1
fi

echo "The pull request title is a valid Conventional Commit."
