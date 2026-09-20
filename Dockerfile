# ==========================================
# Stage 1: Build React Frontend
# ==========================================
FROM node:20-alpine AS client-build
WORKDIR /client

COPY src/Client/package*.json ./
RUN npm ci

COPY src/Client/ ./
RUN npm run build

# ==========================================
# Stage 2: Build ASP.NET Core Backend
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS server-build
WORKDIR /src

COPY src/Server/FootballBoardGame.Server.csproj ./Server/
RUN dotnet restore ./Server/FootballBoardGame.Server.csproj

COPY src/Server/ ./Server/

# フロントエンドのビルド成果物を wwwroot に配置
COPY --from=client-build /client/../Server/wwwroot ./Server/wwwroot/

WORKDIR /src/Server
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Stage 3: Runtime for Google Cloud Run
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Google Cloud Run のポート (デフォルト 8080)
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=server-build /app/publish .

ENTRYPOINT ["dotnet", "FootballBoardGame.Server.dll"]
