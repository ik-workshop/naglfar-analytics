/**
 * K6 Stress Test: High Load Scenarios
 *
 * This test pushes the system to its limits to identify breaking points.
 *
 * Run with:
 *   k6 run stress-test.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '1m', target: 50 },    // Ramp up to 50 users
    { duration: '2m', target: 100 },   // Ramp up to 100 users
    { duration: '2m', target: 200 },   // Stress: 200 users
    { duration: '3m', target: 300 },   // Breaking point: 300 users
    { duration: '2m', target: 0 },     // Ramp down
  ],
  thresholds: {
    'http_req_duration': ['p(99)<2000'],  // 99% under 2s
    'errors': ['rate<0.2'],               // Less than 20% errors
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost';
const STORES = ['store-1', 'store-2', 'store-3', 'store-4', 'store-5'];

const ENDPOINTS = [
  '/books',
  '/cart',
  '/orders',
  '/inventory',
];

export default function () {
  const storeId = STORES[Math.floor(Math.random() * STORES.length)];
  const endpoint = ENDPOINTS[Math.floor(Math.random() * ENDPOINTS.length)];
  const url = `${BASE_URL}/api/v1/${storeId}${endpoint}`;

  const params = {
    headers: {
      'Host': 'api.local',
    },
  };

  const response = http.get(url, params);

  const success = check(response, {
    'status is 200 or 401': (r) => r.status === 200 || r.status === 401,
    'response time < 2000ms': (r) => r.timings.duration < 2000,
  });

  errorRate.add(!success);

  // Minimal think time for stress
  sleep(0.1 + Math.random() * 0.3);
}
