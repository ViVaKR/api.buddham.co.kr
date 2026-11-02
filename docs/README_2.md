# 1. 이미지 빌드 & import (맥에서 빌드, 서버에서 import)

chmod +x build-postgres.sh
./build-postgres.sh

# 2. 기존 postgres 삭제 (현재 buddham-all.yaml의 것)

sudo kubectl delete statefulset buddham-postgres buddham-redis -n buddham
sudo kubectl delete pvc -n buddham -l app=buddham-postgres

sudo kubectl delete statefulset buddham-postgres buddham-redis -n buddham
sudo kubectl delete pvc -n buddham -l app=buddham-postgres

# 3. 순서대로 apply (secret → configmap → primary → replica → backup)

sudo kubectl apply -f buddham-postgres-secret.yaml
sudo kubectl apply -f buddham-postgres-configmap.yaml
sudo kubectl apply -f buddham-postgres-primary.yaml

# Primary Running 확인 후!

watch kubectl get pods -n buddham

# 4. Replica & 백업

sudo kubectl apply -f buddham-postgres-replica.yaml
sudo kubectl apply -f buddham-postgres-backup.yaml

## application

```bash
 kubectl apply -f buddham-namespace.yaml -f buddham-redis.yaml -f buddham-apps.yaml

 # 단독 실행
 # AI 부대 진격!
kubectl apply -f buddham-ai.yaml

# Chat 특공대 진격!
kubectl apply -f buddham-chat.yaml
```
