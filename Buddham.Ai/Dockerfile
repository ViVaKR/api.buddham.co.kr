# 1단계: 빌드 기지
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Buddham.Ai/Buddham.Ai.csproj", "Buddham.Ai/"]
COPY ["Buddham.Shared/Buddham.Shared.csproj", "Buddham.Shared/"]
COPY ["Buddham.Core/Buddham.Core.csproj", "Buddham.Core/"]
RUN dotnet restore "Buddham.Ai/Buddham.Ai.csproj"

COPY . .
WORKDIR "/src/Buddham.Ai"
RUN dotnet build "Buddham.Ai.csproj" -c Release -o /app/build

# 2단계: 퍼블리시
FROM build AS publish
RUN dotnet publish "Buddham.Ai.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3단계: 런타임 출격 (gRPC 포트인 50051 개방)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 50051
ENTRYPOINT ["dotnet", "Buddham.Ai.dll"]
