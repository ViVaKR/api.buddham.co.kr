#

```powershell
Start-BuddhamDeploy -WhatIf        # 단계 미리 보기
Start-BuddhamDeploy -App chat

# 롤백
kubectl rollout undo deployment/buddham-chat-deploy -n buddham

```
