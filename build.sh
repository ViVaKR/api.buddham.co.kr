#!/usr/bin/env bash
APP=$1  # buddham-ai, buddham-api, buddham-chat
TAG=net10

case $APP in
  buddham-api)  PROJECT_DIR="Buddham.Api" YAML="k8s/application/buddham-api.yaml" ;;
  buddham-ai)   PROJECT_DIR="Buddham.Ai" YAML="k8s/application/buddham-ai.yaml" ;;
  buddham-chat) PROJECT_DIR="Buddham.Chat" YAML="k8s/application/buddham-chat.yaml" ;;
  *) echo "❌ 알 수 없는 앱: $APP"; exit 1 ;;
esac

echo "▶ Building $APP:$TAG ..."
docker build -t $APP:$TAG -f $PROJECT_DIR/Dockerfile .

echo "▶ Importing to k3s containerd ..."
#docker save $APP:$TAG | sudo k3s ctr images import -
# 파이프 대신 임시파일 방식
docker save $APP:$TAG -o /tmp/$APP.tar
sudo k3s ctr images import /tmp/$APP.tar
rm -f /tmp/$APP.tar


# ✅ yaml 변경사항 반영 (변경 없으면 무시됨)
echo "▶ Applying k8s manifests ..."
sudo kubectl apply -f $YAML -n buddham

echo "▶ Restarting $APP-deploy ..."
sudo kubectl rollout restart deployment/$APP-deploy -n buddham
sudo kubectl rollout status deployment/$APP-deploy -n buddham

echo "✅ Done: $APP:$TAG"

# 사용법 (반드시 Buddham.slnx가 있는 솔루션 루트에서 실행)
#./build.sh buddham-api
#./build.sh buddham-chat
#./build.sh buddham-chat
