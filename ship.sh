#!/usr/bin/env bash

# 사용법: ./ship.sh buddham-api
# 사용법: ./ship.sh buddham-chat
# 사용법: ./ship.sh buddham-ai

set -e

APP=$1
if [ -z "$APP" ]; then
  echo "사용법: ./ship.sh [buddham-api|buddham-ai|buddham-chat]"
  exit 1
fi

REMOTE="Ubuntu-server"
REMOTE_PATH="/home/dharma/Buddham/source"   # ⚠️ 우분투 쪽 솔루션 루트 경로로 수정

echo "🚚 소스 동기화 중 ($REMOTE)..."
rsync -avz --delete \
  --exclude 'bin/' \
  --exclude 'obj/' \
  --exclude '.git/' \
  --exclude '.idea/' \
  -e ssh \
  ./ "$REMOTE:$REMOTE_PATH/"

echo "🔨 원격(우분투, arm64 aarch64)에서 빌드+배포 실행 중..."
ssh -t "$REMOTE" "cd $REMOTE_PATH && ./build.sh $APP"

echo "✅ 이사 완료: $APP"
