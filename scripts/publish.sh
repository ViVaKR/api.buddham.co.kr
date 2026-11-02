#!/usr/bin/env bash

set -e

# 🔑 KUBECONFIG 명시적 설정
export KUBECONFIG="${HOME}/.kube/config"

IMAGE_NAME="kimbumjun/buddham-api"
TAG="v1.1.0" # 혹은 최신 태그 체계

echo "▶ [1/3] Docker Buildx를 통한 멀티아키텍처 빌드 및 푸시 시작..."

# 로컬에 builder가 없다면 생성 후 사용 (Apple Silicon 맥 기준 linux/amd64, linux/arm64 동시 대응 등 필요시 활용)
# docker buildx create --use --name mybuilder 2>/dev/null || docker buildx use mybuilder

# 도커 허브로 곧바로 빌드 & 푸시 (맥에서 빌드해서 허브로 직행!)
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t ${IMAGE_NAME}:${TAG} \
  -t ${IMAGE_NAME}:latest \
  -f Buddham.Api/Dockerfile \
  --push .

echo "✅ Docker Hub Push 완료!"

echo "▶ [2/3] Talos K8s 네임스페이스(buddham) 배포 갱신..."
# k8s 매니페스트 적용 (buddham-api.yaml 내부의 이미지명이 ${IMAGE_NAME}:${TAG}로 되어있어야 함)
kubectl apply -f k8s/buddham-api.yaml -n buddham

echo "▶ [3/3] 롤아웃 재시작 및 상태 모니터링..."
kubectl rollout restart deployment/buddham-api-deploy -n buddham
kubectl rollout status deployment/buddham-api-deploy -n buddham

echo "🎉 Buddham API 프로덕션 배포 완벽 종료!"
