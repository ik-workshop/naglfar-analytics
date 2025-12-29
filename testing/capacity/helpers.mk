# Capacity Testing with Gatling - Helper Commands
# These commands should be run from the project root

.PHONY: help capacity-build capacity-browse capacity-full-flow capacity-stress capacity-all capacity-results capacity-report capacity-clean capacity-shell

CAPACITY_DIR := testing/capacity
CAPACITY_IMAGE := naglfar-capacity-tests
CAPACITY_RESULTS_DIR := $(CAPACITY_DIR)/results
TIMESTAMP := $(shell date +%Y%m%d_%H%M%S)

capacity-build: ## Build Gatling capacity testing Docker image
	@echo "Building Gatling capacity test image..."
	docker build -t $(CAPACITY_IMAGE) $(CAPACITY_DIR)

capacity-browse: capacity-build ## Run browse books capacity test
	@echo "Running browse books capacity test..."
	@mkdir -p $(CAPACITY_RESULTS_DIR)
	docker run --rm \
		--network host \
		-v $(PWD)/$(CAPACITY_RESULTS_DIR):/capacity/target/gatling \
		$(CAPACITY_IMAGE) \
		gatling:test -Dscenario=scenarios/browse-books.yaml \
		| tee $(CAPACITY_RESULTS_DIR)/browse-$(TIMESTAMP).log

capacity-full-flow: capacity-build ## Run full user flow capacity test
	@echo "Running full user flow capacity test..."
	@mkdir -p $(CAPACITY_RESULTS_DIR)
	docker run --rm \
		--network host \
		-v $(PWD)/$(CAPACITY_RESULTS_DIR):/capacity/target/gatling \
		$(CAPACITY_IMAGE) \
		gatling:test -Dscenario=scenarios/full-user-flow.yaml \
		| tee $(CAPACITY_RESULTS_DIR)/full-flow-$(TIMESTAMP).log

capacity-stress: capacity-build ## Run stress capacity test
	@echo "Running stress capacity test..."
	@mkdir -p $(CAPACITY_RESULTS_DIR)
	docker run --rm \
		--network host \
		-v $(PWD)/$(CAPACITY_RESULTS_DIR):/capacity/target/gatling \
		$(CAPACITY_IMAGE) \
		gatling:test -Dscenario=scenarios/stress-test.yaml \
		| tee $(CAPACITY_RESULTS_DIR)/stress-$(TIMESTAMP).log

capacity-all: capacity-build ## Run all capacity tests
	@echo "Running all capacity tests..."
	@$(MAKE) capacity-browse
	@echo ""
	@echo "Waiting 30 seconds before next test..."
	@sleep 30
	@$(MAKE) capacity-full-flow
	@echo ""
	@echo "Waiting 30 seconds before stress test..."
	@sleep 30
	@$(MAKE) capacity-stress

capacity-results: ## Show latest capacity test results summary
	@echo "=== Capacity Test Results ==="
	@if [ -d "$(CAPACITY_RESULTS_DIR)" ]; then \
		echo "Latest test runs:"; \
		ls -lht $(CAPACITY_RESULTS_DIR)/*.log 2>/dev/null | head -5; \
		echo ""; \
		echo "Latest log output:"; \
		tail -50 $$(ls -t $(CAPACITY_RESULTS_DIR)/*.log 2>/dev/null | head -1) 2>/dev/null || echo "No results found"; \
	else \
		echo "No results directory found"; \
	fi

capacity-report: ## Open latest Gatling HTML report
	@echo "Opening latest Gatling report..."
	@LATEST_REPORT=$$(find $(CAPACITY_RESULTS_DIR) -name "index.html" -type f -print0 | xargs -0 ls -t | head -1); \
	if [ -n "$$LATEST_REPORT" ]; then \
		open "$$LATEST_REPORT" 2>/dev/null || xdg-open "$$LATEST_REPORT" 2>/dev/null || echo "Report: $$LATEST_REPORT"; \
	else \
		echo "No Gatling reports found in $(CAPACITY_RESULTS_DIR)"; \
	fi

capacity-clean: ## Clean capacity test results
	@echo "Cleaning capacity test results..."
	rm -rf $(CAPACITY_RESULTS_DIR)/*
	@echo "Results cleaned"

capacity-shell: capacity-build ## Open shell in capacity test container
	@echo "Opening shell in capacity test container..."
	docker run --rm -it \
		--network host \
		--entrypoint /bin/bash \
		$(CAPACITY_IMAGE)
