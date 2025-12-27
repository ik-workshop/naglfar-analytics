# Determine the directory of this helper makefile
BOOK_STORE_DIR := services/book-store

compose-rebuild-book-store: ## Rebuild and restart web-app service
	docker-compose -f infrastructure/docker-compose.yml up -d --build protected-service-eu

docker-run-book-store: ## Run the Docker container for the book-store service
	@docker run --rm -it -p 8090:8000 book-store

docker-build-book-store: ## Build the Docker image for the book-store service
	docker build -t book-store --progress plain $(BOOK_STORE_DIR)

lock-dependencies-book-store: ## Generate Pipfile.lock using Docker (no local Python/pipenv needed)
	$(eval PYTHON_IMAGE := $(shell grep "^FROM python" $(BOOK_STORE_DIR)/Dockerfile | awk '{print $$2}'))
	@echo "Generating Pipfile.lock using image: $(PYTHON_IMAGE)"
	@docker run --rm \
		-v $(PWD)/$(BOOK_STORE_DIR):/app \
		-w /app \
		$(PYTHON_IMAGE) \
		bash -c "pip install --quiet --root-user-action ignore pipenv && pipenv lock --verbose"
	@echo "✓ Pipfile.lock generated successfully in $(BOOK_STORE_DIR)/"

test-book-store: ## Run pytest tests in Docker for book-store service
	@echo "Running pytest tests for book-store service..."
	@docker run --rm \
		-v $(PWD)/$(BOOK_STORE_DIR):/app \
		-w /app \
		-e PYTHONPATH=/app/src \
		python:3.14 \
		bash -c "pip install --quiet pipenv && pipenv install --dev && pipenv run pytest -v"

test-book-store-coverage: ## Run pytest with coverage for book-store service
	@echo "Running pytest with coverage for book-store service..."
	@docker run --rm \
		-v $(PWD)/$(BOOK_STORE_DIR):/app \
		-w /app \
		-e PYTHONPATH=/app/src \
		python:3.14 \
		bash -c "pip install --quiet pipenv && pipenv install --dev && pipenv run pytest --cov=src/app --cov-report=term-missing --cov-report=html -v"
	@echo "✓ Coverage report generated in $(BOOK_STORE_DIR)/htmlcov/"
