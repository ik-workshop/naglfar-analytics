/**
 * K6 Performance Test: Full User Flow
 *
 * This test simulates the complete user journey:
 * 1. Browse books
 * 2. Add book to cart
 * 3. View cart
 * 4. Checkout
 *
 * Run with:
 *   k6 run full-user-flow.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const checkoutSuccess = new Rate('checkout_success');
const totalOrders = new Counter('total_orders');
const browseLatency = new Trend('browse_latency');
const cartLatency = new Trend('cart_latency');
const checkoutLatency = new Trend('checkout_latency');

export const options = {
  stages: [
    { duration: '30s', target: 5 },   // Ramp up to 5 concurrent users
    { duration: '2m', target: 5 },    // Sustain 5 users
    { duration: '30s', target: 20 },  // Spike to 20 users
    { duration: '1m', target: 20 },   // Sustain spike
    { duration: '30s', target: 0 },   // Ramp down
  ],
  thresholds: {
    'http_req_duration': ['p(95)<1000'],
    'errors': ['rate<0.05'],
    'checkout_success': ['rate>0.9'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost';
const STORES = ['store-1', 'store-2', 'store-3'];

export default function () {
  const storeId = STORES[Math.floor(Math.random() * STORES.length)];

  const params = {
    headers: {
      'Host': 'api.local',
      'Content-Type': 'application/json',
    },
  };

  let authToken = null;
  let authTokenId = null;

  // Helper to update auth tokens from response
  const updateAuthTokens = (response) => {
    if (response.headers['Auth-Token']) {
      authToken = response.headers['Auth-Token'];
    }
    if (response.headers['Auth-Token-Id']) {
      authTokenId = response.headers['Auth-Token-Id'];
    }
    if (authToken) {
      params.headers['AUTH-TOKEN'] = authToken;
    }
  };

  // Group 1: Browse Books
  group('Browse Books', function () {
    const url = `${BASE_URL}/api/v1/${storeId}/books`;
    const start = Date.now();
    const response = http.get(url, params);
    browseLatency.add(Date.now() - start);

    updateAuthTokens(response);

    const success = check(response, {
      'browse: status 200': (r) => r.status === 200,
      'browse: has books': (r) => {
        try {
          const books = JSON.parse(r.body);
          return Array.isArray(books) && books.length > 0;
        } catch (e) {
          return false;
        }
      },
    });

    errorRate.add(!success);
    if (!success) return;  // Stop if browse failed

    // Parse books and select one
    const books = JSON.parse(response.body);
    const selectedBook = books[Math.floor(Math.random() * books.length)];

    sleep(1);  // User thinks about the book

    // Group 2: Add to Cart
    group('Add to Cart', function () {
      const cartUrl = `${BASE_URL}/api/v1/${storeId}/cart`;
      const payload = JSON.stringify({
        book_id: selectedBook.id,
        quantity: 1,
      });

      const start = Date.now();
      const response = http.post(cartUrl, payload, params);
      cartLatency.add(Date.now() - start);

      updateAuthTokens(response);

      const success = check(response, {
        'cart: status 200 or 201': (r) => r.status === 200 || r.status === 201,
      });

      errorRate.add(!success);
      if (!success) return;
    });

    sleep(0.5);  // User reviews cart

    // Group 3: View Cart
    group('View Cart', function () {
      const cartUrl = `${BASE_URL}/api/v1/${storeId}/cart`;
      const response = http.get(cartUrl, params);

      updateAuthTokens(response);

      const success = check(response, {
        'view cart: status 200': (r) => r.status === 200,
        'view cart: has items': (r) => {
          try {
            const cart = JSON.parse(r.body);
            return cart.items && cart.items.length > 0;
          } catch (e) {
            return false;
          }
        },
      });

      errorRate.add(!success);
      if (!success) return;
    });

    sleep(1);  // User decides to checkout

    // Group 4: Checkout
    group('Checkout', function () {
      const orderUrl = `${BASE_URL}/api/v1/${storeId}/orders`;
      const start = Date.now();
      const response = http.post(orderUrl, null, params);
      checkoutLatency.add(Date.now() - start);

      updateAuthTokens(response);

      const success = check(response, {
        'checkout: status 200 or 201': (r) => r.status === 200 || r.status === 201,
        'checkout: has order_id': (r) => {
          try {
            const order = JSON.parse(r.body);
            return order.order_id !== undefined;
          } catch (e) {
            return false;
          }
        },
      });

      errorRate.add(!success);
      checkoutSuccess.add(success);

      if (success) {
        totalOrders.add(1);
      }
    });

    sleep(2);  // User completes session
  });
}

export function handleSummary(data) {
  return {
    'stdout': textSummary(data, { indent: ' ', enableColors: true }),
  };
}

function textSummary(data, options) {
  const indent = options.indent || '';
  const enableColors = options.enableColors || false;

  let summary = `\n${indent}Test Summary:\n`;
  summary += `${indent}  Total Requests: ${data.metrics.http_reqs.values.count}\n`;
  summary += `${indent}  Failed Requests: ${data.metrics.http_req_failed.values.rate * 100}%\n`;
  summary += `${indent}  Avg Response Time: ${data.metrics.http_req_duration.values.avg}ms\n`;
  summary += `${indent}  P95 Response Time: ${data.metrics.http_req_duration.values['p(95)']}ms\n`;

  if (data.metrics.total_orders) {
    summary += `${indent}  Total Orders: ${data.metrics.total_orders.values.count}\n`;
  }

  if (data.metrics.checkout_success) {
    summary += `${indent}  Checkout Success Rate: ${data.metrics.checkout_success.values.rate * 100}%\n`;
  }

  return summary;
}
