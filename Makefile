.PHONY: help
#? help: Get more info on available commands
help: Makefile
	@sed -n 's/^#?//p' $< | column -t -s ':' |  sort | sed -e 's/^/ /'

# Local build without Docker
#? restore: Restore .NET dependencies
restore:
	@echo "Restoring .NET dependencies..."
	dotnet restore src/NaglfartAnalytics/NaglfartAnalytics.csproj
	dotnet restore tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj

#? build: Build the application
build: restore
	@echo "Building the application..."
	dotnet build src/NaglfartAnalytics/NaglfartAnalytics.csproj -c Release
	dotnet build tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj -c Release

#? run: Run the application locally
run:
	@echo "Running the application..."
	@echo "Application will be available at: http://localhost:5000"
	@echo "Health check: http://localhost:5000/healthz"
	@echo "Readiness check: http://localhost:5000/readyz"
	dotnet run --project src/NaglfartAnalytics/NaglfartAnalytics.csproj --urls "http://localhost:8080"

#? test: Run all tests
test:
	@echo "Running tests..."
	dotnet test tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj

#? test-watch: Run tests in watch mode
test-watch:
	@echo "Running tests in watch mode..."
	@echo "Press Ctrl+C to stop"
	dotnet watch test --project tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj

#? test-coverage: Run tests with code coverage
test-coverage:
	@echo "Running tests with code coverage..."
	dotnet test tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj \
		--collect:"XPlat Code Coverage" \
		--results-directory ./coverage

#? test-verbose: Run tests with verbose output
test-verbose:
	@echo "Running tests with verbose output..."
	dotnet test tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj \
		--verbosity detailed

#? clean: Clean build artifacts
clean:
	@echo "Cleaning build artifacts..."
	dotnet clean src/NaglfartAnalytics/NaglfartAnalytics.csproj
	dotnet clean tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj
	rm -rf src/NaglfartAnalytics/bin src/NaglfartAnalytics/obj
	rm -rf tests/NaglfartAnalytics.Tests/bin tests/NaglfartAnalytics.Tests/obj
	rm -rf coverage

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

#? apigw-restart: rebuild and restart traefik
apigw-restart:
	@docker compose -f docker-compose.yml up -d --build api-gateway

# Diagram commands
DIAGRAMS_DIR := docs/assets/diagrams
DIAGRAMS_SRC := $(wildcard $(DIAGRAMS_DIR)/*.mmd)
DIAGRAMS_SVG := $(DIAGRAMS_SRC:.mmd=.svg)
MERMAID_CLI_VERSION := 11.12.0
MERMAID_CLI_IMAGE := minlag/mermaid-cli:$(MERMAID_CLI_VERSION)

#? diagrams: Generate SVG images from Mermaid diagrams
diagrams: $(DIAGRAMS_SVG)
	@echo "✓ All diagrams generated successfully!"
	@echo "  Generated $(words $(DIAGRAMS_SVG)) SVG files in $(DIAGRAMS_DIR)/"

$(DIAGRAMS_DIR)/%.svg: $(DIAGRAMS_DIR)/%.mmd
	@echo "Generating $@..."
	@docker run --rm -v $(PWD):/data $(MERMAID_CLI_IMAGE) \
		-i /data/$< -o /data/$@ -b white -t neutral

#? diagrams-validate: Validate Mermaid diagrams by checking for syntax errors
diagrams-validate:
	@echo "Validating Mermaid diagrams..."
	@failed=0; \
	for file in $(DIAGRAMS_SRC); do \
		echo "  Validating $$(basename $$file)..."; \
		output=$$(docker run --rm -v $(PWD):/data $(MERMAID_CLI_IMAGE) \
			-i /data/$$file -o - -b transparent -t neutral 2>&1); \
		if echo "$$output" | grep -qi "syntax error\|error in graph\|parse error"; then \
			echo "    ✗ Syntax error detected"; \
			echo "$$output" | grep -i "error" | head -3; \
			failed=1; \
		else \
			echo "    ✓ Valid"; \
		fi; \
	done; \
	if [ $$failed -eq 0 ]; then \
		echo "✓ All $(words $(DIAGRAMS_SRC)) diagrams are valid!"; \
	else \
		echo "✗ Some diagrams have syntax errors"; \
		exit 1; \
	fi

#? diagrams-clean: Remove generated SVG files
diagrams-clean:
	@echo "Cleaning generated SVG files..."
	@rm -f $(DIAGRAMS_DIR)/*.svg
	@echo "✓ Cleaned $(DIAGRAMS_DIR)/"
