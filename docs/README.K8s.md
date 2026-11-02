k# K8s

```bash
# 우분투에서
grep -A2 "Ollama" /home/viva/Buddham/source/k8s/application/buddham-api.yaml

```

## 파드 재시작

```bash
kubectl rollout restart statefulset buddham-ollama-chat -n buddham
kubectl rollout restart statefulset buddham-ollama-embed -n buddham
```
