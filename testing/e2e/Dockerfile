# E2E Testing Docker Image
FROM python:3.12-slim

WORKDIR /e2e

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy test code and scenarios
COPY tests/ tests/
COPY scenarios/ scenarios/

# Make scripts executable
RUN chmod +x tests/scenario_runner.py tests/naglfar_test.py

# Default command: run scenario runner with help
ENTRYPOINT ["python", "tests/scenario_runner.py"]
CMD ["--help"]
