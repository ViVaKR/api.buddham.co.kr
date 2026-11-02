# 빌드 배포

```bash
# "DefaultConnection": "Host=buddham-postgres-svc;Port=5432;Database=buddham;Username=buddham;Password=A9037!m8947#;SSL Mode=Prefer;Trust Server Certificate=true;Pooling=true;Maximum Pool Size=50;",

# 포스트그레 서비스 설정 편집창 열기
# kubectl edit svc buddham-postgres-svc -n buddham
# 편집창이 열리면 type: ClusterIP라고 되어 있는 부분을 type: NodePort로 고치고 저장(:wq)
# 그러면 몽고디비처럼 5432:31xxx/TCP 형태로 외부 포트가 똭 열림
# 그 열린 외부 포트와 노드의 실제 사설 IP(예: 192.168.x.x)를 조합해서 appsettings.json에 박아넣고 대포를 쏘면
# 결계고 뭐고 다 뚫고 들어감
# 마이그레이션 성공 후 다시 ClusterIP로 원복!)

# 포트포오딩
kubectl port-forward svc/buddham-postgres-svc 5432:5432 -n buddham

# 솔루션 루트에서 작업
# 1. migrations
dotnet ef migrations add InitialBuddhamMigration \
  -c Buddham.Shared.Data.BuddhamDbContext \
  -p Buddham.Migrations \
  -s Buddham.Api \
  -o Data/Api/Buddham


# 2. update
# 🚀 설계도는 그대로 두고, 배달 주소(127.0.0.1)를 향해 포격 개시!!!
dotnet ef database update \
  -c Buddham.Shared.Data.BuddhamDbContext \
  -p Buddham.Migrations \
  -s Buddham.Api


# [public]
# 1. migrations
dotnet ef migrations add InitialPublicMigration \
  -c Buddham.Shared.Data.PublicDbContext \
  -p Buddham.Migrations \
  -s Buddham.Api \
  -o Data/Api/Public


dotnet ef database update \
  -c Buddham.Shared.Data.PublicDbContext \
  -p Buddham.Migrations \
  -s Buddham.Api


# [log]
# 1. migrations
dotnet ef migrations add InitialLogMigration \
  -c Buddham.Shared.Data.LogDbContext \
  -p Buddham.Migrations \
  -s Buddham.Api \
  -o Data/Api/Log

# 2. update
dotnet ef database update \
  -c Buddham.Shared.Data.LogDbContext \
  -p Buddham.Migrations \
  -s Buddham.Api

```

```bash
kubectl describe pod/buddham-api-deploy-7d98cb8cd7-kcz66 -n buddham
kubectl logs -l app=buddham-api -n buddham --tail=50
kubectl exec -it buddham-api-deploy-5f8cb7f4f9-8kxd6 -n buddham -- /bin/bash
```

```bash
 2813  kubectl rollout restart deployment/buddham-api-deploy -n buddham
 2814  kubectl rollout status deployment/buddham-api-deploy -n buddham
 2816  kubectl exec -it buddham-postgres-0 -n buddham -- psql -U buddham -d buddham -c "CREATE TABLE ai.test_table (id serial primary key); DROP TABLE ai.test_table;"
 2817  kubectl exec -it buddham-postgres-0 -n buddham -- psql -U buddham -d buddham -c "CREATE TABLE public.test_table (id serial primary key); DROP TABLE ai.test_table;"
 2818  kubectl exec -it buddham-postgres-0 -n buddham -- psql -U buddham -d buddham -c "CREATE TABLE public.test_table (id serial primary key); DROP TABLE public.test_table;"

```


```bash
# ① 맥에서 빌드 (솔루션 루트에서)
docker build -t buddham-api:net10 -f Buddham.Api/Dockerfile .

# ② 서버에서 import
docker save buddham-api:net10 | sudo k3s ctr images import -

# ③ Pod 재시작
sudo kubectl rollout restart deployment buddham-api-deploy -n buddham

# ④ 확인
watch kubectl get pods -n buddham

##############

# ✅ 이미지 교체할 때
sudo kubectl rollout restart deployment buddham-api-deploy -n buddham
# → 잠깐 두 벌 보이다가 자동 정리됨. 기다리면 끝!

# ✅ 진행 상황 보기
sudo kubectl rollout status deployment buddham-api-deploy -n buddham
```

## 기타

```bash

# 검색
grep -rniE "검색키워드1|검색키워드2" --include="*.cs" --include="*.ts" --include="*.html" --include="*.json" --include="*.md" .
```
