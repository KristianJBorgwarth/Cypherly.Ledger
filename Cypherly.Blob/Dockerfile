FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cypherly.Blob/Cypherly.Blob.csproj", "Cypherly.Blob/"]
RUN dotnet restore "Cypherly.Blob/Cypherly.Blob.csproj"
COPY . .
WORKDIR "/src/Cypherly.Blob"
RUN dotnet build "./Cypherly.Blob.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cypherly.Blob.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cypherly.Blob.dll"]
