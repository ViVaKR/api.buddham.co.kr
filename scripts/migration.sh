#!/usr/bin/env zsh
# ═══════════════════════════════════════════════════════
# 🪷 Buddham 마이그레이션 스크립트
# 사용법:
#   ./migrate.sh add       # 전체 마이그레이션 생성
#   ./migrate.sh add Auth  # 특정 스키마만 생성
#   ./migrate.sh update    # 전체 DB 반영
#   ./migrate.sh update Log # 특정 스키마만 반영
# ═══════════════════════════════════════════════════════

set -e

STARTUP="Buddham.Api"
MIGRATIONS="Buddham.Migrations"

# 스키마 → Context 매핑 (Context명은 스키마명DbContext 규칙)
typeset -A CONTEXTS
CONTEXTS=(
  Auth      "BuddhamDbContext"
  Ai        "AiDbContext"
  Archive   "ArchiveDbContext"
  Canon     "CanonDbContext"
  Chat      "ChatDbContext"
  Comment   "CommentDbContext"
  Dharma    "DharmaDbContext"
  Log       "LogDbContext"
  Public    "PublicDbContext"
)

# 출력 디렉토리 규칙: Data/스키마명
output_dir() {
  echo "Data/$1"
}

# 마이그레이션 이름 규칙: Init스키마명 (예: InitAuth, InitLog)
migration_name() {
  echo "Init$1"
}

# ───────────────────────────────────────────────────────
do_add() {
  local schema=$1
  local context=$CONTEXTS[$schema]
  local name=$(migration_name $schema)
  local out=$(output_dir $schema)

  echo "🔨 [$schema] 마이그레이션 생성 → $name"
  dotnet ef migrations add "$name" \
    --project "$MIGRATIONS" \
    --startup-project "$STARTUP" \
    --context "$context" \
    --output-dir "$out"
  echo "✅ [$schema] 완료"
}

do_update() {
  local schema=$1
  local context=$CONTEXTS[$schema]

  echo "🚀 [$schema] DB 반영 중..."
  dotnet ef database update \
    --project "$MIGRATIONS" \
    --startup-project "$STARTUP" \
    --context "$context"
  echo "✅ [$schema] 반영 완료"
}

# ───────────────────────────────────────────────────────
CMD=$1
TARGET=$2

case $CMD in
  add)
    if [ -n "$TARGET" ]; then
      # 특정 스키마만
      if [ -z "$CONTEXTS[$TARGET]" ]; then
        echo "❌ 알 수 없는 스키마: $TARGET"
        echo "사용 가능: ${(k)CONTEXTS}"
        exit 1
      fi
      do_add "$TARGET"
    else
      # 전체
      echo "🪷 전체 마이그레이션 생성 시작..."
      for schema in ${(k)CONTEXTS}; do
        do_add "$schema"
      done
      echo "🎉 전체 마이그레이션 생성 완료!"
    fi
    ;;

  update)
    if [ -n "$TARGET" ]; then
      if [ -z "$CONTEXTS[$TARGET]" ]; then
        echo "❌ 알 수 없는 스키마: $TARGET"
        echo "사용 가능: ${(k)CONTEXTS}"
        exit 1
      fi
      do_update "$TARGET"
    else
      echo "🪷 전체 DB 반영 시작..."
      for schema in ${(k)CONTEXTS}; do
        do_update "$schema"
      done
      echo "🎉 전체 DB 반영 완료!"
    fi
    ;;

  *)
    echo "사용법:"
    echo "  ./migrate.sh add              # 전체 마이그레이션 생성"
    echo "  ./migrate.sh add Auth         # Auth 스키마만 생성"
    echo "  ./migrate.sh update           # 전체 DB 반영"
    echo "  ./migrate.sh update Log       # Log 스키마만 반영"
    echo ""
    echo "사용 가능한 스키마: ${(k)CONTEXTS}"
    exit 1
    ;;
esac
