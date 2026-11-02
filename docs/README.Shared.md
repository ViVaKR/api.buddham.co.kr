# Migration

```bash
sudo chown -R www-data:www-data /home/viva/Kubs/Buddham/
sudo usermod -aG www-data viva
newgrp www-data

stat ~/Kubs/Buddham/Storage
#  파일: /home/viva/Kubs/Buddham/Storage
#  크기: 4096      	블록: 8          입출력 블록: 4096   디렉터리
#장치: 8,2	아이노드: 31593569    링크: 3
#접근: (0755/drwxr-xr-x)  UID: (   33/www-data)   GID: (   33/www-data)
#접근: 2026-06-24 23:14:01.290515969 +0900
#수정: 2026-06-24 20:02:07.190780171 +0900
#변경: 2026-06-24 23:16:15.219688203 +0900
#생성: 2026-06-24 19:34:23.828998195 +0900

kubectl apply -f [사령관님의-배포파일].yaml
```

```bash
# 1. AuthDbContext 기본 마이그레이션 (이름은 오늘날짜로 자동생성)
./migrate.sh

# 2. AuthDbContext에 원하는 이름으로 마이그레이션
./migrate.sh InitialAuth AuthDbContext

# 3. 나중에 LogDbContext 마이그레이션 할 때도 한 줄로 끝!
./migrate.sh InitialLog LogDbContext

```

```bash
# ---> Kubs PostgreSQL (AUTH)
dotnet ef migrations add Modified_UserRole_RemvoeWarn -c Buddham.API.Data.Kubs.AuthDbContext -o Migrations/Kubs/Auth

dotnet ef migrations remove -c Buddham.API.Data.Kubs.AuthDbContext

dotnet ef database update -c Buddham.API.Data.Kubs.AuthDbContext


dotnet ef migrations add Init_20260511 -c Buddham.API.Data.Kubs.ApplicationDbContext -o Migrations/Kubs/Auth

dotnet ef migrations remove -c Buddham.API.Data.Kubs.ApplicationDbContext

dotnet ef database update -c Buddham.API.Data.Kubs.ApplicationDbContext

# ──> (Public) ───────────────────

dotnet ef migrations add Modified_Many_11 -c Buddham.API.Data.Kubs.PublicDbContext -o Migrations/Kubs/Public

dotnet ef migrations remove -c Buddham.API.Data.Kubs.PublicDbContext

dotnet ef database update -c Buddham.API.Data.Kubs.PublicDbContext

# ──> (Ai) ───────────────────

dotnet ef migrations add Init_AiDharma -c Buddham.API.Data.Kubs.AiDharmaDbContext -o Migrations/Kubs/Ai

dotnet ef migrations remove -c Buddham.API.Data.Kubs.AiDharmaDbContext

dotnet ef database update -c Buddham.API.Data.Kubs.AiDharmaDbContext

# ──> (Log) ───────────────────

dotnet ef migrations add Init_AuditLog -c Buddham.API.Data.Kubs.LogDbContext -o Migrations/Kubs/Log

dotnet ef migrations remove -c Buddham.API.Data.Kubs.LogDbContext

dotnet ef database update -c Buddham.API.Data.Kubs.LogDbContext

# ──> (Archive) ───────────────────
dotnet ef migrations add Modified_Archive_Schema_1 -c Buddham.API.Data.Kubs.ArchiveDbContext -o Migrations/Kubs/Archive

dotnet ef migrations remove -c Buddham.API.Data.Kubs.ArchiveDbContext

dotnet ef database update -c Buddham.API.Data.Kubs.ArchiveDbContext

# ──> (Demo) ───────────────────
dotnet ef migrations add Modified_Demo_Schema_1 -c Buddham.API.Data.Kubs.DemoDbContext -o Migrations/Kubs/Demo

dotnet ef migrations remove -c Buddham.API.Data.Kubs.DemoDbContext

dotnet ef database update -c Buddham.API.Data.Kubs.DemoDbContext

# ──> (Comment) ───────────────────
dotnet ef migrations add ModifiedGuideComment_RemoveLikeCount -c Buddham.API.Data.Kubs.CommentDbContext -o Migrations/Kubs/Comment
dotnet ef migrations remove -c Buddham.API.Data.Kubs.CommentDbContext
dotnet ef database update -c Buddham.API.Data.Kubs.CommentDbContext

```

```bash
# 통합 불상(Buddham) 마이그레이션 치트키!
buddham-ef() {
    local action=$1  # add, remove, update
    local ctx=$2     # auth, public, ai, log, archive, demo, comment
    local name=$3    # 마이그레이션 이름 (add 일 때만 필요)

    # 1. DbContext 명칭 매핑
    local context_class=""
    local out_dir=""

    case $ctx in
        auth)    context_class="Buddham.Shared.Data.Kubs.AuthDbContext"; out_dir="Data/Migrations/Kubs/Auth" ;;
        public)  context_class="Buddham.Shared.Data.Kubs.PublicDbContext"; out_dir="Data/Migrations/Kubs/Public" ;;
        ai)      context_class="Buddham.Shared.Data.Kubs.AiDharmaDbContext"; out_dir="Data/Migrations/Kubs/Ai" ;;
        log)     context_class="Buddham.Shared.Data.Kubs.LogDbContext"; out_dir="Data/Migrations/Kubs/Log" ;;
        archive) context_class="Buddham.Shared.Data.Kubs.ArchiveDbContext"; out_dir="Data/Migrations/Kubs/Archive" ;;
        demo)    context_class="Buddham.Shared.Data.Kubs.DemoDbContext"; out_dir="Data/Migrations/Kubs/Demo" ;;
        comment) context_class="Buddham.Shared.Data.Kubs.CommentDbContext"; out_dir="Data/Migrations/Kubs/Comment" ;;
        *) echo "어허, ctx 이름을 잘 쓰게나! (auth, public, ai, log...)"; return 1 ;;
    esac

    # 2. 액션별 명령어 실행
    case $action weigh
        add)
            if [ -z "$name" ]; then echo "마이그레이션 이름을 적어주게!"; return 1; fi
            dotnet ef migrations add "$name" -c "$context_class" -p Buddham.Shared -s Buddham.Api -o "$out_dir"
            ;;
        remove)
            dotnet ef migrations remove -c "$context_class" -p Buddham.Shared -s Buddham.Api
            ;;
        update)
            dotnet ef database update -c "$context_class" -p Buddham.Shared -s Buddham.Api
            ;;
        *) echo "액션은 add, remove, update 중 하나만 가능하네!"; return 1 ;;
    esac
}

# buddham-ef add auth Modified_UserRole

# buddham-ef remove ai

# buddham-ef update comment

```
