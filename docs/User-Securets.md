#

```bash
# 1) 새 키 생성 (64바이트 → base64)
$key = [Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }))

# 2) 로컬 user-secrets 갱신
dotnet user-secrets set "JwtConfig:Key" $key

# 3) 클러스터 시크릿 갱신 (같은 키)
kubectl create secret generic buddham-app-secret -n buddham `
  --from-literal=JwtConfig__Key=$key `
  --dry-run=client -o yaml | kubectl apply -f -

# 4) 파드 재시작 (시크릿은 시작 시점에 주입됨)
kubectl rollout restart deployment/buddham-api-deploy -n buddham

```
