#!/bin/bash
# Mobile Panel dist/ → ASP.NET Server wwwroot/ 동기화
# 사용법: 어디서든 실행 가능 (스크립트 기준 경로 사용)

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DIST="$ROOT/Mobile-Panel/dist"
WWWROOT="$ROOT/Server-AspNet/ExhibitionServer/wwwroot"

if [ ! -d "$DIST" ]; then
  echo "[ERROR] $DIST 없음. 먼저 'npm run build' 실행 필요"
  exit 1
fi

echo "[copy-panel] $DIST → $WWWROOT 복사 중..."
rm -rf "$WWWROOT"/*
cp -r "$DIST"/. "$WWWROOT"/
echo "[copy-panel] 완료"
