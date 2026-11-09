# 로그 옵션 모음


```bash

# 최근 50줄부터 시작해서 계속 따라가기 (처음에 전체 로그가 쏟아지는 걸 방지)
kubectl logs -f --tail=50 deploy/buddham-api-deploy -n buddham

# 최근 10분 로그만
kubectl logs --since=10m deploy/buddham-api-deploy -n buddham

# 시간 표시 붙이기
kubectl logs -f --timestamps deploy/buddham-api-deploy -n buddham

# Remote IP 줄만 필터링
kubectl logs -f deploy/buddham-api-deploy -n buddham | grep "Remote IP"

# 직전에 죽은 컨테이너 로그 (CrashLoop일 때)
kubectl logs --previous deploy/buddham-api-deploy -n buddham

```
