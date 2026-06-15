FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-api
WORKDIR /src
COPY . .
RUN dotnet restore "src/WorldEventAlerts.Api/WorldEventAlerts.Api.csproj"
RUN dotnet publish "src/WorldEventAlerts.Api/WorldEventAlerts.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build-api /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WorldEventAlerts.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-web
WORKDIR /src
COPY . .
RUN dotnet restore "src/WorldEventAlerts.Web/WorldEventAlerts.Web.csproj"
RUN dotnet publish "src/WorldEventAlerts.Web/WorldEventAlerts.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS web
WORKDIR /app
COPY --from=build-web /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WorldEventAlerts.Web.dll"]
