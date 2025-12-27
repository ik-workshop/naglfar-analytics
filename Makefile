
MAKEFLAGS += --warn-undefined-variables
MAKEFLAGS += --no-builtin-rules

DIAGRAMS_DIR := docs/assets/diagrams
DIAGRAMS_SRC := $(wildcard $(DIAGRAMS_DIR)/*.mmd)
DIAGRAMS_SVG := $(DIAGRAMS_SRC:.mmd=.svg)
MERMAID_CLI_VERSION := 11.12.0
MERMAID_CLI_IMAGE := minlag/mermaid-cli:$(MERMAID_CLI_VERSION)

.PHONY: help
help:
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)

-include services/book-store/helpers.mk
-include services/naglfar-validation/helpers.mk

restore: ## Restore .NET dependencies for all services
	@echo "Restoring .NET dependencies..."
	dotnet restore services/naglfar-validation/src/NaglfartAnalytics/NaglfartAnalytics.csproj
	dotnet restore services/naglfar-validation/tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj

#? build: Build all .NET services
build: restore
	@echo "Building all services..."
	dotnet build services/naglfar-validation/src/NaglfartAnalytics/NaglfartAnalytics.csproj -c Release
	dotnet build services/naglfar-validation/tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj -c Release

#? run: Run naglfar-validation service locally
run:
	@echo "Running naglfar-validation service..."
	@echo "Application will be available at: http://localhost:8000"
	@echo "Health check: http://localhost:8000/healthz"
	@echo "Readiness check: http://localhost:8000/readyz"
	dotnet run --project services/naglfar-validation/src/NaglfartAnalytics/NaglfartAnalytics.csproj --urls "http://localhost:8000"

#? test: Run all tests
test:
	@echo "Running tests for all services..."
	dotnet test services/naglfar-validation/tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj

#? test-watch: Run tests in watch mode
test-watch:
	@echo "Running tests in watch mode..."
	@echo "Press Ctrl+C to stop"
	dotnet watch test --project services/naglfar-validation/tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj

#? test-coverage: Run tests with code coverage
test-coverage:
	@echo "Running tests with code coverage..."
	dotnet test services/naglfar-validation/tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj \
		--collect:"XPlat Code Coverage" \
		--results-directory ./coverage

#? test-verbose: Run tests with verbose output
test-verbose:
	@echo "Running tests with verbose output..."
	dotnet test services/naglfar-validation/tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj \
		--verbosity detailed

clean: ## Clean build artifacts
	@echo "Cleaning build artifacts..."
	dotnet clean services/naglfar-validation/src/NaglfartAnalytics/NaglfartAnalytics.csproj
	dotnet clean services/naglfar-validation/tests/NaglfartAnalytics.Tests/NaglfartAnalytics.Tests.csproj
	rm -rf services/naglfar-validation/src/NaglfartAnalytics/bin services/naglfar-validation/src/NaglfartAnalytics/obj
	rm -rf services/naglfar-validation/tests/NaglfartAnalytics.Tests/bin services/naglfar-validation/tests/NaglfartAnalytics.Tests/obj
	rm -rf coverage

#? compose-up: Build and run with docker-compose
compose-up:
	@echo "Starting application with docker-compose..."
	@echo "Application will be available at: http://localhost:8080"
	@echo "Health check: http://localhost:8080/healthz"
	@echo "Readiness check: http://localhost:8080/readyz"
	docker-compose -f infrastructure/docker-compose.yml up --build

#? compose-down: Stop and remove docker-compose containers
compose-down:
	@echo "Stopping docker-compose services..."
	docker-compose -f infrastructure/docker-compose.yml down

#? compose-logs: show logs for docker-compose containers
compose-logs:
	@echo "Showing docker-compose logs..."
	docker-compose -f infrastructure/docker-compose.yml logs -f

#? validation-rebuild: rebuild naglfar-validation service
validation-rebuild:
	@docker compose -f infrastructure/docker-compose.yml up -d --build naglfar-validation

#? apigw-restart: rebuild and restart traefik
apigw-restart:
	@docker compose -f infrastructure/docker-compose.yml up -d --build api-gateway

diagrams: $(DIAGRAMS_SVG) ## Generate SVG images from Mermaid diagrams
	@echo "✓ All diagrams generated successfully!"
	@echo "  Generated $(words $(DIAGRAMS_SVG)) SVG files in $(DIAGRAMS_DIR)/"

$(DIAGRAMS_DIR)/%.svg: $(DIAGRAMS_DIR)/%.mmd
	@echo "Generating $@..."
	@docker run --rm -v $(PWD):/data $(MERMAID_CLI_IMAGE) \
		-i /data/$< -o /data/$@ -b white -t neutral

diagrams-validate: ## Validate Mermaid diagrams by checking for syntax errors
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

diagrams-clean: ## Remove generated SVG files
	@echo "Cleaning generated SVG files..."
	@rm -f $(DIAGRAMS_DIR)/*.svg
	@echo "✓ Cleaned $(DIAGRAMS_DIR)/"
