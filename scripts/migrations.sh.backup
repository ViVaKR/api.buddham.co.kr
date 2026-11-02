#!/usr/bin/env zsh

# 로컬 네트워크에서 K8s ClusterIP 직접 접속! (port-forward 불필요!)
# 💡 사용법: ./migrate.sh [마이그레이션이름] [DbContext이름]
#    예시) ./migrate.sh InitialCreate AuthDbContext
#    이름을 안 주면 오늘 날짜, DbContext를 안 주면 기본 AuthDbContext로 실행됨!

MIG_NAME=${1:-"Migration_$(date +%Y%m%d_%H%M%S)"}
CONTEXT_NAME=${2:-"BuddhamDbContext"}

# DbContext 이름에서 "DbContext"를 뗀 순수 스키마 이름 추출 (폴더 정리용 - 예: Auth, Log, Ai)
SCHEMA_NAME="${CONTEXT_NAME%DbContext}"

echo "🚀 [${CONTEXT_NAME}] Migration 생성 시작..."
echo "📂 저장 위치: Buddham.Migrations/${SCHEMA_NAME}/"

# 🌟 핵심 튜닝 포인트: -p(대상 프로젝트)를 Buddham.Migrations로 변경!
dotnet ef migrations add "${MIG_NAME}" \
  -c "Buddham.Shared.Data.${CONTEXT_NAME}" \
  -p Buddham.Migrations \
  -s Buddham.Api \
  -o "${SCHEMA_NAME}"

if [ $? -eq 0 ]; then
  echo "📦 [${CONTEXT_NAME}] Database Update 실행..."
  dotnet ef database update \
    -c "Buddham.Shared.Data.${CONTEXT_NAME}" \
    -p Buddham.Migrations \
    -s Buddham.Api
    
  echo "✅ ${CONTEXT_NAME} 마이그레이션 및 DB 업데이트 완료!"
else
  echo "❌ 마이그레이션 생성 실패! 코드를 확인해 주게나 친구야."
fi
