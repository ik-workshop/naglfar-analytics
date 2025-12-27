# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY src/NaglfartAnalytics/*.csproj ./NaglfartAnalytics/
RUN dotnet restore "./NaglfartAnalytics/NaglfartAnalytics.csproj"

# Copy everything else and build
COPY src/NaglfartAnalytics/. ./NaglfartAnalytics/
WORKDIR /src/NaglfartAnalytics
RUN dotnet build "NaglfartAnalytics.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "NaglfartAnalytics.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080;http://+:8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "NaglfartAnalytics.dll"]
