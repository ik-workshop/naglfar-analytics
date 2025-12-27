#!/bin/bash

# scripts/auth.sh - Test authentication flow with dynamically generated E-TOKEN

# Configuration
STORE_ID="${STORE_ID:-store-1}"
RETURN_URL="${RETURN_URL:-http://localhost:8000/api/v1/store-1/books}"

# Detect OS for date command
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    EXPIRY_DATE=$(date -u -v+15M +"%Y-%m-%dT%H:%M:%S.000Z")
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    # Linux
    EXPIRY_DATE=$(date -u -d "+15 minutes" +"%Y-%m-%dT%H:%M:%S.000Z")
else
    echo "Error: Unsupported OS for date calculation"
    exit 1
fi

# Generate E-TOKEN dynamically (always fresh, expires in 15 minutes)
E_TOKEN_JSON="{\"expiry_date\":\"${EXPIRY_DATE}\",\"store_id\":\"${STORE_ID}\"}"
E_TOKEN=$(echo -n "${E_TOKEN_JSON}" | base64)

echo "=========================================="
echo "Testing Auth Service with E-TOKEN"
echo "=========================================="
echo "Store ID: ${STORE_ID}"
echo "Expiry: ${EXPIRY_DATE} (15 minutes from now)"
echo ""
echo "E-TOKEN JSON: ${E_TOKEN_JSON}"
echo "E-TOKEN (base64): ${E_TOKEN}"
echo ""
echo "Calling auth service..."
echo "=========================================="
echo ""

curl -v -H "Host: auth-service.local" "http://localhost/api/v1/auth/?e_token=${E_TOKEN}&return_url=${RETURN_URL}"
