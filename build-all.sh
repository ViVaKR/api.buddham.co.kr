#!/usr/bin/env bash

set -e

# 🔑 SSH 비대화형 실행에서도 ~/.kube/config 를 확실히 바라보도록 명시!
export KUBECONFIG="${HOME}/.kube/config"

TAG=net10

echo "▶ [1/3] 병렬 Docker 빌드 시작..."

# 3개 동시 빌드 (백그라운드)
pids=()
docker build -t buddham-ai:$TAG   -f Buddham.Ai/Dockerfile   . & pids+=($!)
docker build -t buddham-api:$TAG  -f Buddham.Api/Dockerfile  . & pids+=($!)
docker build -t buddham-chat:$TAG -f Buddham.Chat/Dockerfile . & pids+=($!)

for pid in "${pids[@]}"; do wait "$pid"; done   # 하나라도 실패하면 여기서 중단

# 3개 모두 끝날 때까지 대기
wait
echo "✅ 빌드 완료!"

echo "▶ [2/3] k3s import (순차)..."
for APP in buddham-ai buddham-api buddham-chat; do
  docker save $APP:$TAG -o /tmp/$APP.tar
  sudo k3s ctr images import /tmp/$APP.tar
  rm -f /tmp/$APP.tar
  echo "  ✅ $APP imported"
done

echo "▶ [3/3] k8s 배포 (ai → api → chat 순서)..."
kubectl apply -f k8s/buddham-ai.yaml   -n buddham
kubectl apply -f k8s/buddham-api.yaml  -n buddham
kubectl apply -f k8s/buddham-chat.yaml -n buddham

kubectl rollout restart deployment/buddham-ai-deploy   -n buddham
kubectl rollout restart deployment/buddham-api-deploy  -n buddham
kubectl rollout restart deployment/buddham-chat-deploy -n buddham

# 순차 상태 확인
kubectl rollout status deployment/buddham-ai-deploy   -n buddham
kubectl rollout status deployment/buddham-api-deploy  -n buddham
kubectl rollout status deployment/buddham-chat-deploy -n buddham

echo "🎉 전체 배포 완료!"
