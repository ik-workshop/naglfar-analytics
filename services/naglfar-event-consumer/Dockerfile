# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY src/NaglfartEventConsumer/*.csproj ./NaglfartEventConsumer/
RUN dotnet restore "./NaglfartEventConsumer/NaglfartEventConsumer.csproj"

# Copy everything else and build
COPY src/NaglfartEventConsumer/. ./NaglfartEventConsumer/
WORKDIR /src/NaglfartEventConsumer
RUN dotnet build "NaglfartEventConsumer.csproj" -c Release -o /app/build

# Publish stage with optimizations
FROM build AS publish
RUN dotnet publish "NaglfartEventConsumer.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true \
    /p:PublishSingleFile=false \
    /p:EnableCompressionInSingleFile=false

# Runtime stage (using runtime, not aspnet, since this is a worker service)
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS runtime

WORKDIR /app

COPY --from=publish /app/publish .

# Health check (basic process check)
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD pgrep -f "NaglfartEventConsumer" || exit 1

ENTRYPOINT ["dotnet", "NaglfartEventConsumer.dll"]
