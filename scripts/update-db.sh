#!/bin/bash

# 💡 실행할 때 파라미터가 들어왔는지 확인 (예: ./update-db.sh LogDbContext)
CONTEXT_NAME=$1

if [ -z "$CONTEXT_NAME" ]; then
    echo "❌ [오류] DbContext 이름을 입력하지 않았다네 친구야!"
    echo "💡 사용법: ./update-db.sh [DbContext이름]"
    echo "📌 예시: ./update-db.sh LogDbContext"
    exit 1
fi

echo "🧹 기존 포트 포워딩 정리 중..."
kill $(lsof -t -i:5432) 2>/dev/null

echo "🚀 쿠버네티스 포트 포워딩 시작 (5432:5432)..."
kubectl port-forward svc/buddham-postgres-svc 5432:5432 -n buddham > /dev/null 2>&1 &
sleep 2

# 🔐 로컬 user-secrets에서 연결 문자열 추출 (Api 프로젝트 기준)
REAL_CONN=$(dotnet user-secrets get "ConnectionStrings:DefaultConnection" --project ../Buddham.Api | grep -o "Host=.*")

if [ -z "$REAL_CONN" ]; then
    echo "❌ user-secrets에서 연결 문자열을 읽어오지 못했다네."
    kill $(lsof -t -i:5432) 2>/dev/null
    exit 1
fi

# 포트 포워딩 경로로 주소 치환 후 환경 변수 등록
export ConnectionStrings__DefaultConnection=$(echo $REAL_CONN | sed 's/Host=[^;]*/Host=127.0.0.1/')

# 🔥 입력받은 컨텍스트 이름으로 다이내믹하게 업데이트!
echo "⚡ EF Core 데이터베이스 업데이트 실행 중... 🎯 대상: [$CONTEXT_NAME]"
dotnet ef database update \
  -c "$CONTEXT_NAME" \
  -p Buddham.Migrations \
  -s Buddham.Api

# 🛑 포트 포워딩 종료 및 정리
echo "🛑 포트 포워딩 종료 및 정리 중..."
kill $(lsof -t -i:5432) 2>/dev/null

echo "🎉 [$CONTEXT_NAME] 업데이트 완료! 순결하게 성공했다네 친구야! 크하하하!"

# --> (예) 로그 컨텍스트를 업데이트할 때
# ./update-db.sh LogDbContext
