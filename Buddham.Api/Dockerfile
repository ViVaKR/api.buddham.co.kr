FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN sed -i 's|http://archive.ubuntu.com/ubuntu|http://mirror.cloudflare.com/ubuntu|g' /etc/apt/sources.list
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl libfontconfig1 && \
    rm -rf /var/lib/apt/lists/*

COPY ["Buddham.Api/Buddham.Api.csproj", "Buddham.Api/"]
COPY ["Buddham.Shared/Buddham.Shared.csproj", "Buddham.Shared/"]
COPY ["Buddham.Core/Buddham.Core.csproj", "Buddham.Core/"]
COPY ["Buddham.Migrations/Buddham.Migrations.csproj", "Buddham.Migrations/"]
RUN dotnet restore "Buddham.Api/Buddham.Api.csproj"

COPY . .
WORKDIR "/src/Buddham.Api"
RUN dotnet build "./Buddham.Api.csproj" -c Release -o /app/build

# 2단계: 퍼블리시 (최정화 압축)
FROM build AS publish
RUN dotnet publish "./Buddham.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3단계: 최종 실전 가벼운 런타임
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
