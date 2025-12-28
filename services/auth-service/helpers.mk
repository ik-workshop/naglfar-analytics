# Determine the directory of this helper makefile
AUTH_SERVICE_DIR := services/auth-service

compose-rebuild-auth-service: ## Rebuild and restart web-app service
	docker-compose -f infrastructure/docker-compose.yml up -d --build auth-service

docker-run-auth-service: ## Run the Docker container for the book-store service
	@docker run --rm -it -p 8090:8000 book-store

docker-build-auth-service: ## Build the Docker image for the book-store service
	docker build -t auth-service --progress plain $(AUTH_SERVICE_DIR)

lock-dependencies-auth-service: ## Generate Pipfile.lock using Docker
	$(eval PYTHON_IMAGE := $(shell grep "^FROM python" $(AUTH_SERVICE_DIR)/Dockerfile | awk '{print $$2}'))
	@echo "Generating Pipfile.lock using image: $(PYTHON_IMAGE)"
	@docker run --rm \
		-v $(PWD)/$(AUTH_SERVICE_DIR):/app \
		-w /app \
		$(PYTHON_IMAGE) \
		bash -c "pip install --quiet --root-user-action ignore pipenv && pipenv lock"
	@echo "✓ Pipfile.lock generated successfully in $(AUTH_SERVICE_DIR)/"

