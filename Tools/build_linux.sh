#!/usr/bin/env bash
# Sestaví scénu a Linux přehrávač do Build/Linux/Kostka.x86_64
set -euo pipefail
cd "$(dirname "$0")/.."
UNITY="${UNITY:-/workspace/unity/2022.3.62f3/Editor/Unity}"
ulimit -n 65536 || true
"$UNITY" -batchmode -nographics -quit -projectPath "$(pwd)" \
  -executeMethod TetrisBatch.BuildSceneAndLinux -logFile /tmp/tetris_linux.log
echo "Hotovo: Build/Linux/Kostka.x86_64"
