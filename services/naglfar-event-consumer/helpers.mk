# Determine the directory of this helper makefile
EVENT_CONSUMER_DIR := services/naglfar-event-consumer

compose-rebuild-event-consumer: ## Rebuild and restart naglfar-event-consumer service
	@docker compose -f infrastructure/docker-compose.yml up -d --build naglfar-event-consumer

docker-build-event-consumer: ## Build Docker image for naglfar-event-consumer service
	@echo "Building Docker image for event consumer..."
	docker build -t naglfar-event-consumer:latest $(EVENT_CONSUMER_DIR)

docker-run-event-consumer: ## Run Docker container for naglfar-event-consumer service
	@echo "Running Docker container for event consumer..."
	@echo "Event consumer will connect to Redis and subscribe to naglfar-events channel"
	docker run -d --name naglfar-event-consumer \
		-e DOTNET_ENVIRONMENT=Development \
		-e Redis__ConnectionString=host.docker.internal:6379 \
		-e Redis__Channel=naglfar-events \
		naglfar-event-consumer:latest

docker-stop-event-consumer: ## Stop and remove Docker container for naglfar-event-consumer service
	@echo "Stopping Docker container for event consumer..."
	docker stop naglfar-event-consumer || true
	docker rm naglfar-event-consumer || true

docker-clean-event-consumer: docker-stop-event-consumer ## Remove Docker image for naglfar-event-consumer service
	@echo "Removing Docker image for event consumer..."
	docker rmi naglfar-event-consumer:latest || true

test-event-consumer: ## Run tests for naglfar-event-consumer service
	@echo "Running tests for event consumer..."
	cd $(EVENT_CONSUMER_DIR) && dotnet test
