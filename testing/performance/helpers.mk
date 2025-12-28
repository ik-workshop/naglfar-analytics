# Performance Testing Helper Makefile
PERF_DIR := testing/performance
PERF_IMAGE := naglfar-perf-tests:latest
PERF_RESULTS_DIR := $(PERF_DIR)/results

# Docker network (use the same network as the services)
DOCKER_NETWORK := naglfar-network

## Performance Testing Commands

perf-build: ## Build performance testing Docker image
	@echo "Building performance testing Docker image..."
	docker build -t $(PERF_IMAGE) $(PERF_DIR)

perf-browse: perf-build ## Run browse books load test
	@echo "Running browse books load test..."
	@mkdir -p $(PERF_RESULTS_DIR)
	docker run --rm \
		--network $(DOCKER_NETWORK) \
		-v $(PWD)/$(PERF_RESULTS_DIR):/tests/results \
		-e BASE_URL=http://naglfar-validation:8000 \
		$(PERF_IMAGE) \
		run --out json=/tests/results/browse-$$(date +%Y%m%d-%H%M%S).json \
		browse-books.js

perf-full-flow: perf-build ## Run full user flow load test
	@echo "Running full user flow load test..."
	@mkdir -p $(PERF_RESULTS_DIR)
	docker run --rm \
		--network $(DOCKER_NETWORK) \
		-v $(PWD)/$(PERF_RESULTS_DIR):/tests/results \
		-e BASE_URL=http://naglfar-validation:8000 \
		$(PERF_IMAGE) \
		run --out json=/tests/results/full-flow-$$(date +%Y%m%d-%H%M%S).json \
		full-user-flow.js

perf-stress: perf-build ## Run stress test
	@echo "Running stress test..."
	@mkdir -p $(PERF_RESULTS_DIR)
	docker run --rm \
		--network $(DOCKER_NETWORK) \
		-v $(PWD)/$(PERF_RESULTS_DIR):/tests/results \
		-e BASE_URL=http://naglfar-validation:8000 \
		$(PERF_IMAGE) \
		run --out json=/tests/results/stress-$$(date +%Y%m%d-%H%M%S).json \
		stress-test.js

perf-quick: perf-build ## Run quick smoke test (low load, short duration)
	@echo "Running quick smoke test..."
	@mkdir -p $(PERF_RESULTS_DIR)
	docker run --rm \
		--network $(DOCKER_NETWORK) \
		-v $(PWD)/$(PERF_RESULTS_DIR):/tests/results \
		-e BASE_URL=http://naglfar-validation:8000 \
		$(PERF_IMAGE) \
		run --vus 5 --duration 30s \
		--out json=/tests/results/smoke-$$(date +%Y%m%d-%H%M%S).json \
		browse-books.js

perf-all: perf-build ## Run all performance tests
	@echo "Running all performance tests..."
	@$(MAKE) perf-browse
	@$(MAKE) perf-full-flow

perf-results: ## Show performance test results summary
	@echo "=== Performance Test Results ==="
	@if [ -d "$(PERF_RESULTS_DIR)" ] && [ "$$(ls -A $(PERF_RESULTS_DIR) 2>/dev/null)" ]; then \
		echo "Results directory: $(PERF_RESULTS_DIR)"; \
		echo ""; \
		ls -lht $(PERF_RESULTS_DIR) | head -10; \
		echo ""; \
		echo "=== Latest Test Summary ==="; \
		LATEST=$$(ls -t $(PERF_RESULTS_DIR)/*.json 2>/dev/null | head -1); \
		if [ -n "$$LATEST" ]; then \
			echo "File: $$LATEST"; \
			echo ""; \
			cat "$$LATEST" | jq -r '.metrics | to_entries[] | select(.key | test("http_req|check|iteration")) | "\(.key): \(.value.values | to_entries | map("\(.key)=\(.value)") | join(", "))"' 2>/dev/null || echo "Install jq for detailed metrics (brew install jq)"; \
		else \
			echo "No result files found"; \
		fi; \
	else \
		echo "No results found. Run 'make perf-all' first."; \
	fi

perf-clean: ## Clean performance test results
	@echo "Cleaning performance test results..."
	rm -rf $(PERF_RESULTS_DIR)
	@echo "Results cleaned."

perf-compare: ## Compare last two test runs
	@echo "=== Comparing Last Two Test Runs ==="
	@if [ -d "$(PERF_RESULTS_DIR)" ]; then \
		FILES=$$(ls -t $(PERF_RESULTS_DIR)/*.json 2>/dev/null | head -2); \
		if [ $$(echo "$$FILES" | wc -l) -eq 2 ]; then \
			FILE1=$$(echo "$$FILES" | sed -n 1p); \
			FILE2=$$(echo "$$FILES" | sed -n 2p); \
			echo "Latest: $$FILE1"; \
			echo "Previous: $$FILE2"; \
			echo ""; \
			if command -v jq >/dev/null 2>&1; then \
				echo "HTTP Request Duration (avg):"; \
				echo -n "  Latest:   "; cat "$$FILE1" | jq -r '.metrics.http_req_duration.values.avg // "N/A"'; \
				echo -n "  Previous: "; cat "$$FILE2" | jq -r '.metrics.http_req_duration.values.avg // "N/A"'; \
				echo ""; \
				echo "Request Rate (count):"; \
				echo -n "  Latest:   "; cat "$$FILE1" | jq -r '.metrics.http_reqs.values.count // "N/A"'; \
				echo -n "  Previous: "; cat "$$FILE2" | jq -r '.metrics.http_reqs.values.count // "N/A"'; \
			else \
				echo "Install jq for detailed comparison (brew install jq)"; \
			fi; \
		else \
			echo "Not enough test runs to compare. Need at least 2."; \
		fi; \
	else \
		echo "No results found."; \
	fi

perf-shell: perf-build ## Open shell in performance test container
	@echo "Opening shell in performance test container..."
	docker run --rm -it \
		--network $(DOCKER_NETWORK) \
		--entrypoint /bin/sh \
		$(PERF_IMAGE)
