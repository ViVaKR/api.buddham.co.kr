#!/usr/bin/env zsh

# 로컬 네트워크에서 K8s ClusterIP 직접 접속!
# port-forward 불필요!

echo "🚀 Migration 실행..."
dotnet ef migrations add ${1:-"Migration_$(date +%Y%m%d)"} \
  -c Buddham.Shared.Data.BuddhamDbContext \
  -p Buddham.Shared \
  -s Buddham.Api \
  -o Data/Migrations/Buddham

echo "📦 Database Update..."
dotnet ef database update \
  -c Buddham.Shared.Data.BuddhamDbContext \
  -p Buddham.Shared \
  -s Buddham.Api

echo "✅ Migration 완료!"

# buddham-apps.yaml 이미 완벽하게 되어있어!
#- name: ConnectionStrings__DefaultConnection
#  valueFrom:
#    secretKeyRef:
#      name: buddham-postgres-secret
#      key: DefaultConnection  # ← Secret 에서 직접 주입!
#

#export ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=buddham;Username=buddham;Password=A9037!m8947#;SSL Mode=Prefer;Trust Server Certificate=true;"
#cd Buddham.Api
#dotnet ef database update
