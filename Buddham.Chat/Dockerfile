FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Buddham.Chat/Buddham.Chat.csproj", "Buddham.Chat/"]
COPY ["Buddham.Shared/Buddham.Shared.csproj", "Buddham.Shared/"]
COPY ["Buddham.Core/Buddham.Core.csproj", "Buddham.Core/"]
RUN dotnet restore "Buddham.Chat/Buddham.Chat.csproj"
COPY . .
WORKDIR "/src/Buddham.Chat"
RUN dotnet build "./Buddham.Chat.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Buddham.Chat.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Buddham.Chat.dll"]
