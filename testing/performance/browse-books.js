/**
 * K6 Performance Test: Browse Books
 *
 * This test simulates users browsing the book catalog.
 *
 * Run with:
 *   k6 run browse-books.js
 *
 * Run with custom VUs and duration:
 *   k6 run --vus 10 --duration 30s browse-books.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const browseLatency = new Trend('browse_latency');

// Test configuration
export const options = {
  stages: [
    { duration: '30s', target: 10 },  // Ramp up to 10 users over 30s
    { duration: '1m', target: 10 },   // Stay at 10 users for 1 minute
    { duration: '30s', target: 50 },  // Ramp up to 50 users over 30s
    { duration: '1m', target: 50 },   // Stay at 50 users for 1 minute
    { duration: '30s', target: 0 },   // Ramp down to 0 users
  ],
  thresholds: {
    'http_req_duration': ['p(95)<500'],  // 95% of requests must complete below 500ms
    'errors': ['rate<0.1'],              // Error rate must be below 10%
  },
};

// Test data
const BASE_URL = __ENV.BASE_URL || 'http://localhost';
const STORES = ['store-1', 'store-2', 'store-3', 'store-4', 'store-5'];

export default function () {
  // Select a random store
  const storeId = STORES[Math.floor(Math.random() * STORES.length)];
  const url = `${BASE_URL}/api/v1/${storeId}/books`;

  // Add headers
  const params = {
    headers: {
      'Host': 'api.local',
      'Content-Type': 'application/json',
    },
  };

  // Make request
  const startTime = Date.now();
  const response = http.get(url, params);
  const duration = Date.now() - startTime;

  // Record metrics
  browseLatency.add(duration);

  // Validate response
  const success = check(response, {
    'status is 200': (r) => r.status === 200,
    'has books': (r) => {
      try {
        const books = JSON.parse(r.body);
        return Array.isArray(books) && books.length > 0;
      } catch (e) {
        return false;
      }
    },
    'response time < 500ms': () => duration < 500,
  });

  errorRate.add(!success);

  // Think time - simulate user reading
  sleep(1 + Math.random() * 2);  // Random sleep between 1-3 seconds
}
