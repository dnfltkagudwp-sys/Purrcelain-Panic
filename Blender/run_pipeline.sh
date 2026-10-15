#!/usr/bin/env bash
# 원본 GLB에서 작업 파일을 처음부터 다시 만든다 (각 단계는 한 번만 실행).
# 사용법: bash Blender/run_pipeline.sh [마지막 단계 번호, 기본 04]
set -euo pipefail
cd "$(dirname "$0")/.."
B="${BLENDER:-/c/Program Files/Blender Foundation/Blender 5.2/blender.exe}"
S=Blender/scripts
LAST="${1:-04}"

run() {
  echo "=== $1"
  "$B" --background "${@:2}" --python "$S/$1.py" 2>&1 | grep -E "\[PP\]|Error|Traceback|line [0-9]" || true
}

run 00_import --factory-startup
for step in 01_cat 02_vase 03_table_style 03b_vase_variants 04_export; do
  [[ "${step:0:2}" > "$LAST" ]] && break
  run "$step" Blender/Purrcelain_Work.blend
done
