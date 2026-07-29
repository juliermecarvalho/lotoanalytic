FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props NuGet.config ./
COPY apps/api/src/LotoAnalytics.Api/LotoAnalytics.Api.csproj apps/api/src/LotoAnalytics.Api/
RUN dotnet restore apps/api/src/LotoAnalytics.Api/LotoAnalytics.Api.csproj

COPY apps/api/src apps/api/src
RUN dotnet publish apps/api/src/LotoAnalytics.Api/LotoAnalytics.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LotoAnalytics.Api.dll"]
