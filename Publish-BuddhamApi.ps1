<#
.SYNOPSIS
    구시대적 x86을 배제한 오직 ARM64 시대정신 기반 Buddham API 배포 스크립트
#>

$ErrorActionPreference = "Stop"

$ImageName = "kimbumjun/buddham-api"

$Tag = "v1.1.2"

Write-Host "▶ [1/3] 오직 ARM64 빌드..." -ForegroundColor Cyan

docker buildx build `
  --platform linux/arm64 `
  -t "${ImageName}:${Tag}" `
  -t "${ImageName}:latest" `
  -f Buddham.Api/Dockerfile `
  --push .

Write-Host "✅ Docker Hub Push 완료!" -ForegroundColor Green

Write-Host "▶ [2/3] Talos K8s 네임스페이스(buddham) 배포 갱신..." -ForegroundColor Cyan

kubectl apply -f k8s/buddham-api.yaml -n buddham

Write-Host "▶ [3/3] 롤아웃 재시작 및 상태 모니터링..." -ForegroundColor Cyan

kubectl rollout restart deployment/buddham-api-deploy -n buddham

kubectl rollout status deployment/buddham-api-deploy -n buddham

Write-Host "🎉 시대정신 충만한 Buddham API 프로덕션 배포 완벽 종료!" -ForegroundColor Green
