#!/usr/bin/env zsh

NS="buddham"

echo "🧹 1. 성공 후 종료된(Completed) 찌꺼기 파드 청소 중..."
kubectl delete pod --field-selector=status.phase=Completed -n $NS 2>/dev/null

echo "🧹 2. 실패하거나(Failed) 쫓겨난(Evicted) 에러 파드 청소 중..."
kubectl delete pod --field-selector=status.phase=Failed -n $NS 2>/dev/null

# 💡 만약 일반 Web API 파드만 새 기분으로 재시작하고 싶다면 Deployment만 콕 집어서!
# (StatefulSet DB는 절대 건드리지 않음!)
# kubectl rollout restart deployment -n $NS

# 다 건듦
# echo "🔄 StatefulSet Pod들 RESTARTS = 0 으로 갱신 중..."
# kubectl rollout restart statefulset -n $NS

echo "✨ 청소 완료! 안전하게 살아있는 최신 파드 목록:"
kubectl get pods -n $NS
