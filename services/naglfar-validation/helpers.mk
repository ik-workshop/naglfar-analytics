# Determine the directory of this helper makefile
NAGLFAR_VALIDATION_DIR := services/naglfar-validation

docker-build-naglfar: ## Build Docker image for naglfar-analytics service
	@echo "Building Docker image..."
	docker build -t naglfar-analytics:latest $(NAGLFAR_VALIDATION_DIR)

docker-run-naglfar: ## Run Docker container for naglfar-analytics service
	@echo "Running Docker container..."
	@echo "Application will be available at: http://localhost:8080"
	@echo "Health check: http://localhost:8080/healthz"
	@echo "Readiness check: http://localhost:8080/readyz"
	docker run -d --name naglfar-analytics -p 8080:8080 -p 8081:8081 \
		-e ASPNETCORE_ENVIRONMENT=Production \
		-e ASPNETCORE_URLS=http://+:8080 \
		naglfar-analytics:latest

docker-stop-naglfar: ## Stop and remove Docker container for naglfar-analytics service
	@echo "Stopping Docker container..."
	docker stop naglfar-analytics || true
	docker rm naglfar-analytics || true

docker-clean-naglfar: docker-stop ## Remove Docker image for naglfar-analytics service
	@echo "Removing Docker image..."
	docker rmi naglfar-analytics:latest || true
