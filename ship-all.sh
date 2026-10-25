#!/usr/bin/env zsh
set -e

REMOTE="Ubuntu-server"
REMOTE_PATH="/home/dharma/Buddham/source"

echo "🚚 소스 동기화 중..."
rsync -avz --delete \
  --exclude 'bin/' \
  --exclude 'obj/' \
  --exclude '.git/' \
  --exclude '.idea/' \
  --exclude '.DS_Store' \
  --exclude '**/.DS_Store' \
  -e ssh \
  ./ "$REMOTE:$REMOTE_PATH/"

echo "🔑 실행 권한 부여..."
ssh "$REMOTE" "chmod +x $REMOTE_PATH/build.sh $REMOTE_PATH/build-all.sh"

echo "🔨 원격 병렬 빌드+배포 시작..."
ssh -t "$REMOTE" "cd $REMOTE_PATH && ./build-all.sh"

echo "✅ 전체 배포 완료!"
