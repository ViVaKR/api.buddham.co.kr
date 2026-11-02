# ============================================================
# Buddham.Api (ARM64 / Talos 배포용)
# 빌드 컨텍스트는 솔루션 루트:
#   docker buildx build -f Buddham.Api/Dockerfile .
# ============================================================

# ---------- 1단계: 빌드 + restore ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN sed -i 's|http://archive.ubuntu.com/ubuntu|http://mirror.cloudflare.com/ubuntu|g' /etc/apt/sources.list
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl libfontconfig1 && \
    rm -rf /var/lib/apt/lists/*

# 공통 속성 / 중앙 패키지 관리 파일은 반드시 csproj 보다 먼저 복사해야 합니다.
# (restore 가 TargetFramework 와 PackageVersion 을 이 파일들에서 읽습니다)
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]

# csproj 만 먼저 복사해서 restore 레이어를 캐시합니다.
COPY ["Buddham.Api/Buddham.Api.csproj", "Buddham.Api/"]
COPY ["Buddham.Shared/Buddham.Shared.csproj", "Buddham.Shared/"]
COPY ["Buddham.Core/Buddham.Core.csproj", "Buddham.Core/"]
COPY ["Buddham.Migrations/Buddham.Migrations.csproj", "Buddham.Migrations/"]
RUN dotnet restore "Buddham.Api/Buddham.Api.csproj"

# 나머지 소스 복사 (.dockerignore 가 bin / obj / artifacts 를 걸러냅니다)
COPY . .

# ---------- 2단계: 퍼블리시 ----------
# publish 가 빌드를 포함하므로 별도의 dotnet build 단계는 두지 않습니다.
FROM build AS publish
WORKDIR /src/Buddham.Api
RUN dotnet publish "./Buddham.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- 3단계: 최종 실행 이미지 ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# SkiaSharp 네이티브 라이브러리 의존성 설치
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    libpng16-16 \
    libicu-dev \
    libgl1 \
    libglib2.0-0 \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Buddham.Api.dll"]
