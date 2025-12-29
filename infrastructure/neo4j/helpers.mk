# Determine the directory of this helper makefile
INFRASTRUCTURE_NEO4J_DIR := infrastructure/neo4j

# compose-up: ## Build and start all services with docker-compose
# 	@echo "Starting application with docker-compose..."
# 	@echo "Application will be available at: http://localhost:8080"
# 	@echo "Health check: http://localhost:8080/healthz"
# 	@echo "Readiness check: http://localhost:8080/readyz"
# 	docker-compose -f $(INFRASTRUCTURE_DIR)/docker-compose.yml up --build

# compose-down: ## Stop and remove docker-compose containers
# 	@echo "Stopping docker-compose services..."
# 	docker-compose -f $(INFRASTRUCTURE_DIR)/docker-compose.yml down

# compose-logs: ## Stop and remove docker-compose containers
# 	@echo "Showing docker-compose logs..."
# 	docker-compose -f $(INFRASTRUCTURE_DIR)/docker-compose.yml logs -f

# compose-restart: ## Rebuild and restart multiple services
# 	@docker compose -f $(INFRASTRUCTURE_DIR)/docker-compose.yml up -d --build api-gateway
# 	@docker compose -f $(INFRASTRUCTURE_DIR)/docker-compose.yml up -d --build redis-insight
# 	@docker compose -f $(INFRASTRUCTURE_DIR)/docker-compose.yml up -d --build neo4j

compose-neo4j-up: ## Build neo4j services
	@echo "Starting Neo4J with docker-compose..."
	@echo "Application will be available at: http://localhost:8080"
	@echo "Health check: http://localhost:8080/healthz"
	@echo "Readiness check: http://localhost:8080/readyz"
	docker-compose -f $(INFRASTRUCTURE_NEO4J_DIR)/docker-compose.yml up

compose-neo4j-down: ## Stop and remove neo4j containers
	@echo "Stopping docker-compose services..."
	docker-compose -f $(INFRASTRUCTURE_NEO4J_DIR)/docker-compose.yml down --remove-orphans --volumes
