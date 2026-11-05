# Migrations

## 마이그레이션

```bash

# === [buddham] Auth ===
dotnet ef migrations add TalosStart -p Buddham.Migrations  -s Buddham.Api -c Buddham.Shared.Data.BuddhamDbContext -o Data/Api/Buddham

kubectl port-forward svc/buddham-postgres-svc 5432:5432 -n buddham

dotnet ef database update -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.BuddhamDbContext

# === [Scripture] Auth ===
dotnet ef migrations add ScriptureInit -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.ScriptureDbContext  -o Data/Api/Scripture

dotnet ef database update -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.ScriptureDbContext

# === [public] ===

kubectl port-forward svc/buddham-postgres-svc 5432:5432 -n buddham

dotnet ef migrations add TalosInit -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.PublicDbContext -o Data/Api/Public

dotnet ef database update -p Buddham.Migrations -s Buddham.Api -c PublicDbContext

# === [log] ===
dotnet ef migrations add TalosInit -c Buddham.Shared.Data.LogDbContext -p Buddham.Migrations -s Buddham.Api -o Data/Api/Log

dotnet ef database update -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.LogDbContext

# === [comment] ===
dotnet ef migrations add InitComment -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.CommentDbContext -o Data/Api/Comment

dotnet ef database update -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.CommentDbContext

# === [canon] === 스키마 마이그레이션
dotnet ef migrations add InitCanonSchema -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.CanonDbContext -o Data/Api/Canon
dotnet ef database update -p Buddham.Migrations -s Buddham.Api -c CanonDbContext

# === [pitake] === 스키마 마이그레이션
dotnet ef migrations list -p Buddham.Migrations -s Buddham.Api -c PitakaDbContext

dotnet ef migrations add PitakaInit -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.PitakaDbContext -o Data/Api/Pitaka
dotnet ef database update -p Buddham.Migrations -s Buddham.Api -c PitakaDbContext

# === [archive] ===
dotnet ef migrations add TalosInit -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.ArchiveDbContext  -o Data/Api/Archive

dotnet ef database update -p Buddham.Migrations -s Buddham.Api -c Buddham.Shared.Data.ArchiveDbContext

 # === Update 포트포오딩 ===
kubectl port-forward svc/buddham-postgres-svc 5432:5432 -n buddham

# 마이그레이션 시
"DefaultConnection": "Host=127.0.0.1;Port=5432;Database=buddham;Username=buddham;Password=A9037!m8947#;SSL Mode=Prefer;Trust Server Certificate=true;Pooling=true;Maximum Pool Size=50;",

# 서비스 배포시
"DefaultConnection": "Host=buddham-postgres-svc;Port=5432;Database=buddham;Username=buddham;Password=A9037!m8947#;SSL Mode=Prefer;Trust Server Certificate=true;Pooling=true;Maximum Pool Size=50;"
```

## user-secrets

```powershell




```
---

## 🏛️ 불교 플랫폼 스키마 총정리

- **`Canon` (전체 설계도 및 거대한 울타리):**
- 팔만대장경을 포함해 전체 불교 경전의 가장 근간이 되는 거대한 뼈대와 마스터 데이터 (오리지널 원전 등)를 아우르는 가장 큰 그림!

- **`Pitaka` (팔리 삼장의 체계):**
- 경·률·논이라는 명확한 분류 체계로 경전의 데이터들을 조직화하는 핵심 구조.

- **`Bhavana` (사경 수행의 장):**
- 눈으로 읽고 끝내는 게 아니라, 직접 타이핑하거나 적어 내려가며 마음을 가다듬는 **실천적 사경 (寫經) 수행 공간!**

<pre>
"눈으로 읽고 끝내는 게 아니라 직접 타이핑하며 마음을 가다듬는" 공간이라면, 핵심 엔티티는 **하나의 사경 수행 기록(BhavanaEntry)**이 되겠네. 설계하면서 몇 가지 원칙을 짚어봤네:

원문은 스냅샷으로 고정 — Pitaka의 원문이 나중에 수정/삭제돼도, 이미 시작한 사경 기록은 그 순간의 원문 그대로 남아야 하네. 그래서 PitakaScriptureId는 (인드라망 원칙대로) 스칼라 참조만 걸고, 실제 SourceTitle/SourceContent는 사경 시작 시점에 복사해서 저장
진행 상태를 가진 "수행" — 한 번에 끝나는 게 아니라 진행중/일시정지/완료 상태를 오가는 게 사경의 본질이니 Status 필드 필요
같은 경전을 여러 번 사경해도 됨 — 반야심경을 백 번 필사하는 것도 수행이니, UserId + PitakaScriptureId에 유니크 제약은 걸지 않음 (매번 새 레코드)
</pre>

<pre>
📝 Bhavana 스키마 핵심 설계 포인트
사용자별 수행 세션 (bhavana_sessions 또는 sutra_records)
누가, 언제, 어떤 경전을 가지고 사경을 시작했고 끝맺었는지 기록하는 세션 관리 테이블.
사경 텍스트 진척도 / 타이핑 기록 (bhavana_entries 또는 sutra_lines)
사용자가 실제로 타이핑하거나 작성한 문장 단위 혹은 글자 단위의 기록.
원본 경전(canon이나 pitaka의 텍스트)과 매핑되어서, "어디까지 정확하게 사경했는지" 진척률을 추적할 수 있어야겠지?
마음챙김 노트 / 회향 (메모) (bhavana_reflections)
사경을 마치고 나서 느낀 감상이나 마음의 기록(발원문, 소감 등)을 남기는 공간.
</pre>

- **`Desana` (미래의 경전과 새로운 법문/해석):**
- 과거의 원전에 갇히지 않고, 시대를 거치며 새롭게 생성되는 스님의 법문, 주석, 그리고 현대적 해석들이 살아 숨 쉬는 **진화하는 지혜의 아카이브!**

- **`Puja` (사찰 예법과 의식):**
- 삼배하는 법, 예절 등 불교의 전통적인 의식과 신행 생활의 가이드를 담는 **예경과 실천의 공간!**

- **`Upaya` (미디어 방편 - 유튜브, 탱화, 음성 자료 등):**
- 텍스트를 넘어 유튜브 영상, 시각적인 탱화, 감성적인 음성 자료 등 **오감으로 진리를 접하게 만드는 현대적 방편 (Upāya)들의 집합소!**
