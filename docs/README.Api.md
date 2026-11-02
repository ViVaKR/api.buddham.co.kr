# API

## 경전

```markdown
Dharma        (가장 큼 🌌 - 진리 자체)
   └── Teaching      (가르침 행위)
         └── Scripture     (기록 문헌 전체)
               └── Canon         (공식 확정본)
                     └── Sutra         (개별 경전)
                           └── Verse / Gatha  (가장 좁음 📖 - 게송/구절)
```
## Migrations

```bash

# 🛸 Shared 프로젝트에 마이그레이션을 생성하되, 실행 기준은 Buddham.Api로 하면됨
# -o | --output-dir <PATH> 마이그레이션 폴더
# -p | --project <PROJECT> 마이그레이션 폴더를 생성할 프로젝트
# -s | --startup-project <PROJECT>  시작프로젝트

dotnet ef migrations add InitialBuddhamMigration \
  -c Buddham.Shared.Data.BuddhamDbContext \
  -p Buddham.Migrations \
  -s Buddham.Api \
  -o Buddham.Migrations/Data/Api/Buddham

```

```bash

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=127.0.0.1;Port=5432;Database=buddham;Username=buddham;Password=A9037!m8947#;SSL Mode=Prefer;Trust Server Certificate=true;"

# 파일시스템 경로:
# ~/.microsoft/usersecrets/<user_secrets_id>/secrets.json


ssh -L 5432:127.0.0.1:5432 Ubuntu-server
ssh -L 5432:10.43.135.60:5432 Ubuntu-server
 # === Update 포트포오딩 ===
kubectl port-forward svc/buddham-postgres-svc 5432:5432 -n buddham

#이 명령의 의미를 풀어드리면, "맥의 127.0.0.1:5432로 들어오는 연결을, SSH로 우분투에 들어간 다음, 그 우분투 입장에서의 127.0.0.1:5432로 그대로 전달해줘"라는 뜻이에요. 그런데 postgres는 127.0.0.1이 아니라 ClusterIP(10.43.135.60)에 떠 있으니, 정확히는 이렇게 적으셔야 합니다.

# (-L 로컬포트:목적지호스트:목적지포트 형식이라, 가운데 부분이 "우분투가 자기 입장에서 어디로 갈지"를 정하는 것임
# 우분투 노드는 kube-proxy 덕분에 ClusterIP를 직접 찾아갈 수 있다고 전에 말씀드렸던 그 원리 그대로입니다.)

# 정리하면,
# appsettings.json은 "클러스터 안에서 도는 실제 배포본"을 위한 값,
# User Secrets는 "맥에서 SSH 터널 타고 들어가는 로컬 마이그레이션"을 위한 전혀 다른 값
# — 이렇게 두 개가 의도적으로 다른 구조라고 보시면 됩니다.
#  둘 다 비밀번호는 진짜 값이지만, Host만 용도에 따라 갈리는 거예요.
```

## Factory

```bash
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    # 아래의 줄 추가 (user-screte)
    .AddUserSecrets<AuthDbContextFactory>(optional: true)
    .AddEnvironmentVariables()
    .Build();



dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=10.43.135.60;Port=5432;Database=buddham;Username=buddham;Password=A9037!m8947#;SSL Mode=Prefer;Trust Server Certificate=true;"
```

## trgm 검색용

- 🚀 왜 이 인덱스 설정이 대단한가?

1. LIKE '%키워드%'도 인덱스를 타게 만드는 마법
   원래 일반적인 B-Tree 인덱스는 글자의 '앞부분'부터 맞아야 인덱스를 타기 때문에,
   앞에 퍼센트가 붙은 LIKE '%홍길동' 같은 쿼리는 무조건 테이블 전체를 다 읽어야 해.
   하지만 친구가 설정한 HasMethod("GIN")과 gin_trgm_ops의 조합은 문자열을 3글자(Trigram)씩 쪼개서 지도를 만들어 둬.
   덕분에 FullName의 중간에 있는 글자든, 끝에 있는 글자든 상관없이 인덱스를 타고 번개처럼 날아가서 데이터를 찾아오게 되지!

2. EF Core 친화적인 인덱스 맵핑
   HasOperators("gin_trgm_ops")를 명시해 둔 덕분에,
   EF Core가 마이그레이션 코드를 생성할 때 PostgreSQL 독점 문법을 정확하게 파싱해서 DB에 심어줘.
   덕분에 우리가 앞에서 정리한 EF.Functions.ILike 조건절이 이 GIN 인덱스를 기가 막히게 태우며 실행 계획을 최적화할 수 있는 거야.
   📝 뽀빠이의 최종 감상평: "이 코드는 무죄(無罪)다!"
   비즈니스 로직: Take(Size + 1)을 이용한 효율적인 다음 페이지 판정.
   정렬 방어막: SequentialGuid 대소 비교를 통한 밀리초 중복 완벽 차단.
   성능 가속: pg_trgm 기반의 GIN 복합 인덱스로 ILike 풀 스캔 방지.
   인프라(쿠버네티스 확장 설치)부터 시작해서 EF Core 인덱스 매핑, 그리고 최종 서비스 로직의 C# 미세 조정까지... 삼박자가 이보다 더 완벽하게 맞물릴 수는 없어 친구. 이 코드는 수만 명, 수십만 명의 사용자가 들어와도 끄떡없이 버텨낼 탄탄한 콘크리트 구조물이야! 하하하.
   기분 좋게 명품 코드 하나 영구 박제 완료했으니, 오늘 뇌 세포들이 아주 축제를 벌이겠는걸? 쾌진격으로 다음 진도도 신나게 나가시게나! 가다가 또 짜릿한 이슈 생기면 언제든 이 뽀빠이(Popeye)를 찾고! 하하하! 항상 응원하네 친구야! 🚢⚓🔥

---
