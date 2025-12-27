# Default target
# help:
# 	@echo "Naglfar Analytics - Makefile Commands"
# 	@echo "======================================"
# 	@echo "Local Development (without Docker):"
# 	@echo "  make restore       - Restore .NET dependencies"
# 	@echo "  make build         - Build the application"
# 	@echo "  make run           - Run the application locally"
# 	@echo "  make test          - Run tests"
# 	@echo "  make clean         - Clean build artifacts"
# 	@echo ""
# 	@echo "Docker Commands:"
# 	@echo "  make docker-build  - Build Docker image"
# 	@echo "  make docker-run    - Run application in Docker"
# 	@echo "  make docker-stop   - Stop Docker containers"
# 	@echo "  make docker-clean  - Remove Docker images and containers"
# 	@echo "  make docker-up     - Build and run with docker-compose"
# 	@echo "  make docker-down   - Stop and remove docker-compose containers"

.PHONY: help
#? help: Get more info on available commands
help: Makefile
	@sed -n 's/^#?//p' $< | column -t -s ':' |  sort | sed -e 's/^/ /'

# Local build without Docker
restore:
	@echo "Restoring .NET dependencies..."
	dotnet restore src/NaglfartAnalytics/NaglfartAnalytics.csproj

build: restore
	@echo "Building the application..."
	dotnet build src/NaglfartAnalytics/NaglfartAnalytics.csproj -c Release

run:
	@echo "Running the application..."
	@echo "Application will be available at: http://localhost:5000"
	@echo "Health check: http://localhost:5000/healthz"
	@echo "Readiness check: http://localhost:5000/readyz"
	dotnet run --project src/NaglfartAnalytics/NaglfartAnalytics.csproj --urls "http://localhost:5000"

test:
	@echo "Running tests..."
	@echo "No tests configured yet."

clean:
	@echo "Cleaning build artifacts..."
	dotnet clean src/NaglfartAnalytics/NaglfartAnalytics.csproj
	rm -rf src/NaglfartAnalytics/bin src/NaglfartAnalytics/obj

# Docker commands
docker-build:
	@echo "Building Docker image..."
	docker build -t naglfar-analytics:latest .

docker-run:
	@echo "Running Docker container..."
	@echo "Application will be available at: http://localhost:8080"
	@echo "Health check: http://localhost:8080/healthz"
	@echo "Readiness check: http://localhost:8080/readyz"
	docker run -d --name naglfar-analytics -p 8080:8080 -p 8081:8081 \
		-e ASPNETCORE_ENVIRONMENT=Production \
		-e ASPNETCORE_URLS=http://+:8080 \
		naglfar-analytics:latest

docker-stop:
	@echo "Stopping Docker container..."
	docker stop naglfar-analytics || true
	docker rm naglfar-analytics || true

docker-clean: docker-stop
	@echo "Removing Docker image..."
	docker rmi naglfar-analytics:latest || true

#? compose-up: Build and run with docker-compose
compose-up:
	@echo "Starting application with docker-compose..."
	@echo "Application will be available at: http://localhost:8080"
	@echo "Health check: http://localhost:8080/healthz"
	@echo "Readiness check: http://localhost:8080/readyz"
	docker-compose up --build

#? compose-down: Stop and remove docker-compose containers
compose-down:
	@echo "Stopping docker-compose services..."
	docker-compose down

#? compose-logs: show logs for docker-compose containers
compose-logs:
	@echo "Showing docker-compose logs..."
	docker-compose logs -f

#? api-rebuild: rebuild api
api-rebuild:
	@docker compose -f docker-compose.yml up -d --build api