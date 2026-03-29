FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/MealPlanner.Api/ src/MealPlanner.Api/
RUN dotnet restore src/MealPlanner.Api/MealPlanner.Api.csproj \
    && dotnet publish src/MealPlanner.Api/MealPlanner.Api.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MealPlanner.Api.dll"]
