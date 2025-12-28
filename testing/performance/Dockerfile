# Performance Testing Docker Image with k6
FROM grafana/k6:latest

WORKDIR /tests

# Copy test scripts
COPY *.js .

# Default command (can be overridden)
ENTRYPOINT ["k6"]
CMD ["run", "--help"]
