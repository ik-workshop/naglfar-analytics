# E2E Testing Helper Makefile
E2E_DIR := testing/e2e
E2E_IMAGE := naglfar-e2e-tests:latest
E2E_RESULTS_DIR := $(E2E_DIR)/results
TIMESTAMP := $(shell date +%Y%m%d_%H%M%S)

# Docker network (use host network for localhost access)
DOCKER_NETWORK := host

## E2E Testing Commands

e2e-build: ## Build E2E testing Docker image
	@echo "Building E2E testing Docker image..."
	docker build -t $(E2E_IMAGE) $(E2E_DIR)

e2e-browse: e2e-build ## Run browse books E2E test
	@echo "Running browse books E2E test (YAML scenario)..."
	@mkdir -p $(E2E_RESULTS_DIR)
	docker run --rm \
		--network $(DOCKER_NETWORK) \
		-v $(PWD)/$(E2E_RESULTS_DIR):/e2e/results \
		$(E2E_IMAGE) \
		scenarios/browse-books.yaml --verbose \
		| tee $(E2E_RESULTS_DIR)/browse-$(TIMESTAMP).log

e2e-purchase: e2e-build ## Run purchase book E2E test
	@echo "Running purchase book E2E test (YAML scenario)..."
	@mkdir -p $(E2E_RESULTS_DIR)
	docker run --rm \
		--network $(DOCKER_NETWORK) \
		-v $(PWD)/$(E2E_RESULTS_DIR):/e2e/results \
		$(E2E_IMAGE) \
		scenarios/purchase-book.yaml --verbose \
		| tee $(E2E_RESULTS_DIR)/purchase-$(TIMESTAMP).log

e2e-full-flow: e2e-build ## Run full user flow E2E test
	@echo "Running full user flow E2E test (YAML scenario)..."
	@mkdir -p $(E2E_RESULTS_DIR)
	docker run --rm \
		--network $(DOCKER_NETWORK) \
		-v $(PWD)/$(E2E_RESULTS_DIR):/e2e/results \
		$(E2E_IMAGE) \
		scenarios/full-user-flow.yaml --verbose \
		| tee $(E2E_RESULTS_DIR)/full-flow-$(TIMESTAMP).log

e2e-all: e2e-build ## Run all E2E tests
	@echo "Running all E2E tests..."
	@$(MAKE) e2e-browse
	@echo ""
	@$(MAKE) e2e-purchase
	@echo ""
	@$(MAKE) e2e-full-flow

e2e-scenario: e2e-build ## Run custom YAML scenario (Usage: make e2e-scenario SCENARIO=scenarios/my-test.yaml)
	@if [ -z "$(SCENARIO)" ]; then \
		echo "Error: SCENARIO parameter required"; \
		echo "Usage: make e2e-scenario SCENARIO=scenarios/browse-books.yaml"; \
		exit 1; \
	fi
	@echo "Running custom E2E scenario: $(SCENARIO)..."
	@mkdir -p $(E2E_RESULTS_DIR)
	docker run --rm \
		--network $(DOCKER_NETWORK) \
		-v $(PWD)/$(E2E_DIR):/e2e \
		$(E2E_IMAGE) \
		$(SCENARIO) --verbose \
		| tee $(E2E_RESULTS_DIR)/custom-$(TIMESTAMP).log

e2e-results: ## Show latest E2E test results
	@echo "=== E2E Test Results ==="
	@if [ -d "$(E2E_RESULTS_DIR)" ] && [ "$$(ls -A $(E2E_RESULTS_DIR) 2>/dev/null)" ]; then \
		echo "Results directory: $(E2E_RESULTS_DIR)"; \
		echo ""; \
		echo "Latest test runs:"; \
		ls -lht $(E2E_RESULTS_DIR) | head -5; \
		echo ""; \
		echo "=== Latest Result ==="; \
		cat $$(ls -t $(E2E_RESULTS_DIR)/*.log 2>/dev/null | head -1) 2>/dev/null || echo "No log files found"; \
	else \
		echo "No results found. Run 'make e2e-all' first."; \
	fi

e2e-clean: ## Clean E2E test results
	@echo "Cleaning E2E test results..."
	rm -rf $(E2E_RESULTS_DIR)/*
	@echo "Results cleaned."

e2e-shell: e2e-build ## Open shell in E2E test container
	@echo "Opening shell in E2E test container..."
	docker run --rm -it \
		--network $(DOCKER_NETWORK) \
		-w /e2e \
		--entrypoint /bin/bash \
		$(E2E_IMAGE)

e2e-list-scenarios: ## List available test scenarios
	@echo "=== Available E2E Test Scenarios ==="
	@for file in $(E2E_DIR)/scenarios/*.yaml; do \
		echo ""; \
		echo "Scenario: $$(basename $$file)"; \
		grep "^name:" $$file | sed 's/name: //'; \
		grep "^description:" $$file | sed 's/description: /  /'; \
	done
