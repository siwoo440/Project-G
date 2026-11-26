#!/usr/bin/env bash
set -euo pipefail
story_repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$story_repo_root"
story_dotnet="${PROJECT_G_DOTNET:-dotnet}"
for story_suite in StoryCombat StoryMission StoryPersistence StoryCatalog; do
  "$story_dotnet" run --project "tests/${story_suite}.Tests/${story_suite}.Tests.csproj"
done
if [[ -n "${PROJECT_G_STORY_SOURCE:-}" ]]; then
  python -m unittest discover -s tests/StoryCatalog.Tests -p test_export.py
else
  echo 'Source snapshot oracle: NOT RUN (set PROJECT_G_STORY_SOURCE to the approved snapshots directory).'
fi
git diff --check
