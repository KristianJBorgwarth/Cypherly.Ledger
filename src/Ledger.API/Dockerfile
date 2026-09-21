FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Ledger.API/Ledger.API.csproj", "src/Ledger.API/"]
COPY ["src/Ledger.Application/Ledger.Application.csproj", "src/Ledger.Application/"]
COPY ["src/Ledger.Domain/Ledger.Domain.csproj", "src/Ledger.Domain/"]
COPY ["src/Ledger.Infrastructure/Ledger.Infrastructure.csproj", "src/Ledger.Infrastructure/"]
RUN dotnet restore "src/Ledger.API/Ledger.API.csproj"
COPY . .
WORKDIR "/src/src/Ledger.API"
RUN dotnet build "./Ledger.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ledger.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ledger.API.dll"]
