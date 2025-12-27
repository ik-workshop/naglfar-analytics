# Abusive User Journeys - Book Store

> Attack patterns and malicious behavior scenarios targeting the Book Store API

**Document Purpose**: Define abusive behavior patterns for Naglfar detection and mitigation

---

## Table of Contents
1. [Abuse Pattern 1: Inventory Scraping Bot](#abuse-pattern-1-inventory-scraping-bot)
2. [Abuse Pattern 2: Credential Stuffing Attack](#abuse-pattern-2-credential-stuffing-attack)
3. [Abuse Pattern 3: Inventory Denial Attack (Scalping Bot)](#abuse-pattern-3-inventory-denial-attack-scalping-bot)
4. [Comparison: Abuse vs. Normal Behavior](#comparison-abuse-vs-normal-behavior)
5. [Detection Strategies](#detection-strategies)

---

## Abuse Pattern 1: Inventory Scraping Bot

**Attacker Goal**: Harvest complete inventory data for competitive intelligence or price monitoring

**Duration**: 2-5 minutes (full catalog scrape)
**Total Requests**: 500-2,000
**Target Endpoints**: `/api/v1/books`, `/api/v1/inventory`, `/api/v1/books/{id}`

### Attack Flow

```
Phase 1: Catalog Enumeration (0:00 - 1:00)
├─ GET /api/v1/books?page=1                    (0:00.000)
├─ GET /api/v1/books?page=2                    (0:00.050)  ← 50ms gap
├─ GET /api/v1/books?page=3                    (0:00.100)  ← 50ms gap
├─ GET /api/v1/books?page=4                    (0:00.150)
├─ ... (continues rapidly)
└─ GET /api/v1/books?page=50                   (0:02.500)

Total: 50 requests in 2.5 seconds = 20 req/sec

Phase 2: Individual Book Details (1:00 - 3:00)
├─ GET /api/v1/books/1                         (1:00.000)
├─ GET /api/v1/books/2                         (1:00.080)  ← 80ms gap
├─ GET /api/v1/books/3                         (1:00.160)
├─ GET /api/v1/books/4                         (1:00.240)
├─ ... (systematic enumeration, ID 1-1000)
└─ GET /api/v1/books/1000                      (2:20.000)

Total: 1,000 requests in 80 seconds = 12.5 req/sec

Phase 3: Inventory Monitoring (3:00 - 5:00)
├─ GET /api/v1/inventory?book_id=1             (3:00.000)
├─ GET /api/v1/inventory?book_id=2             (3:00.100)
├─ GET /api/v1/inventory?book_id=3             (3:00.200)
├─ ... (systematic check of all items)
└─ GET /api/v1/inventory?book_id=1000          (4:40.000)

Total: 1,000 requests in 100 seconds = 10 req/sec

GRAND TOTAL: ~2,050 requests in 5 minutes = 6.8 req/sec average
```

### Attack Characteristics

#### Red Flags 🚨

**Timing Anomalies:**
- 🚨 **Mechanical Consistency**: Perfectly consistent gaps (50-200ms) vs natural human pauses (10s-5min)
- 🚨 **High Request Rate**: 10-100+ req/sec vs normal 0.5-2 req/min
- 🚨 **No Think Time**: Zero pause for reading/deciding

**Pattern Anomalies:**
- 🚨 **Sequential ID Enumeration**: Accesses IDs 1,2,3,4... vs random human browsing
- 🚨 **Complete Coverage**: Views 100% of inventory vs human viewing 2-7 items
- 🚨 **Pagination Speed**: Rapid page traversal with no reading time

**Behavioral Anomalies:**
- 🚨 **Zero Conversion**: Never adds to cart or checks out (0% conversion)
- 🚨 **No Authentication**: Often unauthenticated to avoid account tracking
- 🚨 **Repetitive Pattern**: Same exact pattern repeated hourly/daily

**Technical Indicators:**
- 🚨 **User-Agent**: Suspicious (python-requests, curl, or rotating UAs)
- 🚨 **IP Source**: Datacenter IPs, cloud providers, known bot networks
- 🚨 **Proxy Rotation**: Frequent IP changes from same ASN

### Timeline Visualization

```
0:00 ━━━━━━━━━━━━━━━━━━━━━━ Catalog Enumeration (50 pages in 2.5s)
     ├─ 20 req/sec
     └─ No pauses

1:00 ━━━━━━━━━━━━━━━━━━━━━━ Book Detail Scraping (1000 books in 80s)
     ├─ 12.5 req/sec
     └─ Sequential IDs (1,2,3,4...)

3:00 ━━━━━━━━━━━━━━━━━━━━━━ Inventory Monitoring (1000 items in 100s)
     ├─ 10 req/sec
     └─ Complete inventory scan

5:00 ━━━━ End (No cart, no checkout, pure data extraction)
```

### Attacker Profile

**Identity**: Competitor, price monitoring service, data aggregator
**Automation**: Fully automated script/bot
**Goal**: Extract catalog, prices, inventory levels
**Sophistication**: Low to Medium
**Damage**:
- Server resource exhaustion
- Competitive disadvantage (price matching)
- Intellectual property theft (catalog data)
- Bandwidth costs

### Detection Signals

```python
def detect_scraping_bot(session):
    if (
        session.requests_per_minute > 50 AND
        session.sequential_id_access > 10 AND
        session.checkout_rate == 0 AND
        session.session_duration < 10_minutes AND
        session.unique_items_viewed > 100
    ):
        return {
            "threat_type": "SCRAPING_BOT",
            "risk_score": 0.95,
            "confidence": "HIGH",
            "recommended_action": "BLOCK"
        }
```

### Mitigation Strategies

1. **Rate Limiting**: Max 10 requests/minute per IP for catalog endpoints
2. **CAPTCHA Challenge**: After 20 rapid sequential requests
3. **IP Reputation**: Block known datacenter/proxy IPs
4. **Require Authentication**: Force login for inventory endpoint
5. **Honeypot Endpoints**: Fake IDs to detect enumeration
6. **Response Throttling**: Artificial delays for suspicious patterns

---

## Abuse Pattern 2: Credential Stuffing Attack

**Attacker Goal**: Gain unauthorized access to user accounts using leaked credentials from other breaches

**Duration**: 5-30 minutes
**Total Requests**: 100-10,000 login attempts
**Target Endpoint**: `/api/v1/auth/login`

### Attack Flow

```
Credential Testing Phase (Rapid Fire)
├─ POST /api/v1/auth/login                     (0:00.000)
│  Body: {email: "user1@leaked.com", password: "Password123"}
│  Response: 401 Unauthorized
│  IP: 203.0.113.50
│
├─ POST /api/v1/auth/login                     (0:00.200)  ← 200ms gap
│  Body: {email: "user2@leaked.com", password: "admin2023"}
│  Response: 401 Unauthorized
│  IP: 203.0.113.50
│
├─ POST /api/v1/auth/login                     (0:00.400)
│  Body: {email: "user3@leaked.com", password: "letmein"}
│  Response: 401 Unauthorized
│  IP: 203.0.113.50
│
├─ POST /api/v1/auth/login                     (0:00.600)
│  Body: {email: "user4@leaked.com", password: "qwerty"}
│  Response: 401 Unauthorized
│  IP: 203.0.113.50
│
├─ ... (continues with leaked credential pairs)
│  [58 more failed attempts]
│
├─ POST /api/v1/auth/login                     (0:12.000)  ← SUCCESS!
│  Body: {email: "victim@email.com", password: "Summer2023!"}
│  Response: 200 OK, auth-token: "abc123..."
│  IP: 203.0.113.50
│
└─ Immediate Account Takeover Actions:
   ├─ GET /api/v1/cart                         (0:12.100)  ← 100ms after login
   ├─ GET /api/v1/orders                       (0:12.200)
   ├─ POST /api/v1/cart/items                  (0:12.500)  ← Add expensive items
   │  Body: {book_id: 999, quantity: 5}
   └─ POST /api/v1/checkout                    (0:13.000)  ← Fraud attempt
      Body: {payment_method: "saved_card"}

Total: 60 failed logins + 1 success in 12 seconds = 5 login attempts/sec
```

### Attack Characteristics

#### Red Flags 🚨

**Login Pattern Anomalies:**
- 🚨 **High Failure Rate**: 95-99% failed login attempts vs normal user <3 failures
- 🚨 **Unique Emails**: Different email on each attempt vs same email retry
- 🚨 **Mechanical Timing**: Consistent 100-500ms gaps vs human retry (30s-2min)
- 🚨 **No Registration**: Never uses /register endpoint, only /login

**Volume Anomalies:**
- 🚨 **Attempt Count**: 50-1000+ login attempts from single IP vs normal 1-2
- 🚨 **Request Rate**: 3-10 login attempts per second vs normal 1 attempt per session

**Post-Success Behavior:**
- 🚨 **Immediate Actions**: Instant cart/checkout after login (< 1 second)
- 🚨 **No Browsing**: Zero product exploration, direct to fraud
- 🚨 **Unusual Purchases**: High-value items, maximum quantities

**Technical Indicators:**
- 🚨 **IP Reputation**: VPN, proxy, Tor exit nodes, datacenter IPs
- 🚨 **User-Agent Rotation**: Changing or scripted User-Agent strings
- 🚨 **Geographic Mismatch**: Login from Russia, account registered in USA
- 🚨 **Token Abuse**: Successfully obtained tokens used across multiple IPs

### Timeline Visualization

```
0:00 ━━━━ Login attempt 1 (FAIL) ━━━━ 200ms gap
0:00.2 ━━━━ Login attempt 2 (FAIL) ━━━━ 200ms gap
0:00.4 ━━━━ Login attempt 3 (FAIL) ━━━━ 200ms gap
... [57 more failed attempts with mechanical timing]
0:12 ━━━━ Login attempt 61 (SUCCESS!)
     └─ victim@email.com compromised

0:12.1 ━━━━ GET /cart (100ms later)
0:12.2 ━━━━ GET /orders
0:12.5 ━━━━ POST /cart/items (add expensive items)
0:13.0 ━━━━ POST /checkout (fraud attempt)

Pattern: 60 failures → 1 success → immediate fraud
```

### Attack Variants

#### Variant A: Slow Credential Stuffing (Evasion)

```
Goal: Evade rate limiting by spreading attempts over time

├─ POST /api/v1/auth/login  (0:00) → 401 (user1@leaked.com)
├─ [30 second delay]
├─ POST /api/v1/auth/login  (0:30) → 401 (user2@leaked.com)
├─ [30 second delay]
├─ POST /api/v1/auth/login  (1:00) → 401 (user3@leaked.com)
├─ ... (continues at 2 attempts/minute)

Rate: 2 attempts/minute (below typical rate limits)
Detection: 100% failure rate + unique emails still suspicious
```

#### Variant B: Distributed Attack

```
Goal: Avoid per-IP detection by distributing across multiple IPs

IP: 203.0.113.1  → 10 login attempts (all failed)
IP: 203.0.113.2  → 10 login attempts (all failed)
IP: 203.0.113.3  → 10 login attempts (all failed)
IP: 203.0.113.4  → 10 login attempts (1 success!)
... (50 different IPs in coordinated attack)

Pattern: Distributed to avoid per-IP rate limits
Detection: Aggregate failure rate across IPs in time window
```

#### Variant C: Token Sharing (Post-Compromise)

```
Phase 1: Compromise Account
IP: 203.0.113.10  → Successful login → auth-token: "abc123"

Phase 2: Share Token Across Attack Network
IP: 198.51.100.5  → Uses auth-token "abc123" → Fraud transaction 1
IP: 192.0.2.20    → Uses auth-token "abc123" → Fraud transaction 2
IP: 172.16.0.15   → Uses auth-token "abc123" → Fraud transaction 3

Pattern: Same auth-token used from geographically dispersed IPs
Detection: Token reuse from multiple IPs, geographic impossibility
```

### Attacker Profile

**Identity**: Credential stuffing service, account takeover specialist, fraud ring
**Automation**: Fully automated with leaked credential databases
**Goal**: Account takeover for fraud, data theft, account resale
**Sophistication**: Medium to High
**Damage**:
- Customer account compromise
- Financial fraud (unauthorized purchases)
- Identity theft
- Reputational damage
- Legal liability

### Detection Signals

```python
def detect_credential_stuffing(session):
    if (
        session.failed_login_rate > 0.90 AND
        session.unique_emails_per_ip > 10 AND
        session.login_attempts_per_minute > 5 AND
        session.immediate_checkout_after_login == True
    ):
        return {
            "threat_type": "CREDENTIAL_STUFFING",
            "risk_score": 0.98,
            "confidence": "HIGH",
            "recommended_action": "BLOCK_IP_AND_CAPTCHA"
        }

    # Cross-IP pattern detection
    if (
        aggregate_failed_logins_last_5min > 100 AND
        unique_ips_attempting_login > 10 AND
        average_failure_rate_across_ips > 0.95
    ):
        return {
            "threat_type": "DISTRIBUTED_CREDENTIAL_STUFFING",
            "risk_score": 0.96,
            "confidence": "HIGH",
            "recommended_action": "GLOBAL_RATE_LIMIT"
        }
```

### Mitigation Strategies

1. **Rate Limiting**: Max 5 failed logins per IP per hour
2. **CAPTCHA After Failures**: Require CAPTCHA after 3 failed attempts
3. **Account Lockout**: Temporary lock after 5 failed attempts
4. **Email Verification**: Require email verification for password changes
5. **2FA/MFA**: Two-factor authentication for high-value accounts
6. **Breach Detection**: Check passwords against known breach databases
7. **Device Fingerprinting**: Detect unusual devices for known accounts
8. **Geo-Anomaly Detection**: Alert on login from unusual location

---

## Abuse Pattern 3: Inventory Denial Attack (Scalping Bot)

**Attacker Goal**: Monopolize limited inventory (rare books, limited editions) to resell at markup

**Duration**: Seconds (must be fast)
**Total Requests**: 50-200
**Target Endpoints**: `/api/v1/cart/items`, `/api/v1/checkout`

### Attack Flow

```
Pre-Attack Reconnaissance (Days Before)
├─ Daily monitoring of /api/v1/inventory for target book (ID: 999)
├─ Detection: "Limited Edition - Only 10 available"
├─ Wait for restock announcement (email, Twitter, etc.)
└─ Prepare automation script with multiple accounts

Attack Execution (Launch Moment: 0:00)

Pre-Login Phase (-10 seconds before launch)
├─ [Account 1] POST /api/v1/auth/login → auth-token-1
├─ [Account 2] POST /api/v1/auth/login → auth-token-2
├─ [Account 3] POST /api/v1/auth/login → auth-token-3
├─ [Account 4] POST /api/v1/auth/login → auth-token-4
└─ [Account 5] POST /api/v1/auth/login → auth-token-5

Simultaneous Cart Additions (0:00.000)
├─ [Account 1] POST /api/v1/cart/items        (0:00.000)
│  Body: {book_id: 999, quantity: 10}    ← Try to grab all stock
│  Response: 400 "Max 2 per customer"
│
├─ [Account 1] POST /api/v1/cart/items        (0:00.050)
│  Body: {book_id: 999, quantity: 2}
│  Response: 200 OK, cart updated
│  IP: 203.0.113.100
│
├─ [Account 2] POST /api/v1/cart/items        (0:00.100)
│  Body: {book_id: 999, quantity: 2}
│  Response: 200 OK
│  IP: 203.0.113.100  ← Same IP as Account 1!
│
├─ [Account 3] POST /api/v1/cart/items        (0:00.150)
│  Body: {book_id: 999, quantity: 2}
│  Response: 200 OK
│  IP: 203.0.113.101  ← Different IP (proxy rotation)
│
├─ [Account 4] POST /api/v1/cart/items        (0:00.200)
│  Body: {book_id: 999, quantity: 2}
│  Response: 200 OK
│  IP: 203.0.113.102
│
└─ [Account 5] POST /api/v1/cart/items        (0:00.250)
   Body: {book_id: 999, quantity: 2}
   Response: 200 OK (10 units now in carts - SOLD OUT)
   IP: 203.0.113.103

Rapid Checkout (1 second later)
├─ [Account 1] POST /api/v1/checkout          (0:01.000)
│  Response: Order 1001 confirmed
│
├─ [Account 2] POST /api/v1/checkout          (0:01.050)
│  Response: Order 1002 confirmed
│
├─ [Account 3] POST /api/v1/checkout          (0:01.100)
│  Response: Order 1003 confirmed
│
├─ [Account 4] POST /api/v1/checkout          (0:01.150)
│  Response: Order 1004 confirmed
│
└─ [Account 5] POST /api/v1/checkout          (0:01.200)
   Response: Order 1005 confirmed

Result: All 10 limited edition units purchased in <2 seconds
        Legitimate customers see "SOLD OUT" before they can react
```

### Attack Characteristics

#### Red Flags 🚨

**Coordination Anomalies:**
- 🚨 **Simultaneous Actions**: Multiple accounts acting within same 1-second window
- 🚨 **Perfect Timing**: Sub-second coordination (superhuman)
- 🚨 **Identical Behavior**: All accounts follow exact same pattern

**Account Anomalies:**
- 🚨 **Account Age**: Newly created accounts (registered hours/days before)
- 🚨 **Email Patterns**: Similar patterns (scalper1@, scalper2@, disposable emails)
- 🚨 **No History**: Zero browsing history, direct to target item

**Network Anomalies:**
- 🚨 **IP Clustering**: Multiple accounts from same IP or IP range
- 🚨 **Proxy Usage**: Different IPs but same ASN/datacenter
- 🚨 **Geographic Impossibility**: Multiple accounts from different countries acting simultaneously

**Behavioral Anomalies:**
- 🚨 **Zero Browsing**: No product exploration, direct to cart
- 🚨 **Superhuman Speed**: Cart-to-checkout in <5 seconds vs normal 45s-5min
- 🚨 **Maximum Quantity**: Always purchases maximum allowed per order
- 🚨 **Complete Depletion**: Purchases 100% of limited inventory

**Payment Anomalies:**
- 🚨 **Same Payment**: Same payment method across "different" accounts
- 🚨 **Shipping Address**: Multiple orders to same address
- 🚨 **Bulk Shipping**: All orders shipping to reseller/warehouse address

### Timeline Visualization

```
-0:10 ━━━━ Pre-login (5 accounts)
      ├─ Account 1 login
      ├─ Account 2 login
      ├─ Account 3 login
      ├─ Account 4 login
      └─ Account 5 login

0:00.000 ━━━━ ATTACK LAUNCH
      ├─ Acc1: Add to cart (50ms later)
      ├─ Acc2: Add to cart (50ms later)
      ├─ Acc3: Add to cart (50ms later)
      ├─ Acc4: Add to cart (50ms later)
      └─ Acc5: Add to cart

0:00.250 ━━━━ All 10 units in carts (SOLD OUT in 250ms!)

0:01.000 ━━━━ CHECKOUT PHASE
      ├─ Acc1: Checkout (50ms later)
      ├─ Acc2: Checkout (50ms later)
      ├─ Acc3: Checkout (50ms later)
      ├─ Acc4: Checkout (50ms later)
      └─ Acc5: Checkout

0:01.250 ━━━━ All orders confirmed

Legitimate users arrive at 0:05 → See "SOLD OUT"
```

### Attack Variants

#### Variant A: Cart Abandonment DOS

```
Goal: Deny inventory without paying (pure denial, not profit)

├─ [Accounts 1-50] POST /api/v1/cart/items (add max quantity)
├─ [Hold items in cart for 30 minutes]     ← Inventory locked
├─ [Let cart expire without checkout]      ← Inventory returns
└─ Result: Legitimate customers see "Out of Stock" during peak hours

Detection: High cart abandonment rate for specific items
```

#### Variant B: Single Account, Rapid Retry

```
Goal: Game "per order" limits with single account

├─ POST /api/v1/cart/items {qty: 2}   → 200 OK (0:00.100)
├─ POST /api/v1/checkout               → 200 OK (0:00.500)
├─ DELETE /api/v1/cart/items           → 200 OK (0:01.000)
├─ POST /api/v1/cart/items {qty: 2}   → 200 OK (0:01.500)
├─ POST /api/v1/checkout               → 200 OK (0:02.000)
└─ Repeat 5 times = 10 units purchased from single account

Detection: Rapid checkout-clear-checkout pattern
```

### Attacker Profile

**Identity**: Professional scalper, reseller, bot operator
**Automation**: Highly sophisticated, sub-second timing, distributed
**Goal**: Monopolize limited inventory for resale at markup (2-10x)
**Sophistication**: High (requires custom tools, coordination)
**Damage**:
- Legitimate customers unable to purchase
- Brand reputation damage
- Customer frustration and churn
- Fuels secondary market
- Creates bot arms race

### Detection Signals

```python
def detect_scalping_bot(transaction):
    if (
        transaction.cart_to_checkout_time < 5_seconds AND
        transaction.account_age < 7_days AND
        transaction.accounts_from_same_ip > 3 AND
        transaction.item_is_limited_edition == True AND
        transaction.purchased_quantity == max_allowed AND
        transaction.no_browsing_history == True
    ):
        return {
            "threat_type": "INVENTORY_DENIAL_SCALPING",
            "risk_score": 0.92,
            "confidence": "HIGH",
            "recommended_action": "HOLD_ORDER_FOR_REVIEW"
        }

    # Multi-account coordination detection
    if (
        same_item_purchased_by_multiple_accounts_in_1sec > 3 AND
        accounts_share_payment_method == True AND
        accounts_created_within_24h == True
    ):
        return {
            "threat_type": "COORDINATED_SCALPING_BOT",
            "risk_score": 0.96,
            "confidence": "HIGH",
            "recommended_action": "CANCEL_ALL_ORDERS_AND_BAN_ACCOUNTS"
        }
```

### Mitigation Strategies

1. **Account Age Limits**: Require account >30 days old for limited editions
2. **CAPTCHA on Checkout**: Require CAPTCHA for high-demand items
3. **Queue System**: Virtual waiting room for limited releases
4. **Purchase History**: Prioritize customers with purchase history
5. **One Per Household**: Use address + payment to enforce limits
6. **Delayed Fulfillment**: Hold orders for 24h review period
7. **Bot Detection**: Device fingerprinting, behavioral analysis
8. **Manual Review**: Flag orders that match bot patterns

---

## Comparison: Abuse vs. Normal Behavior

### Request Rate Comparison

| Metric | Normal User | Scraping Bot | Credential Stuffing | Scalping Bot |
|--------|-------------|--------------|---------------------|--------------|
| **Requests/min** | 0.5-2 | 20-100+ | 5-50+ | 10-50 |
| **Requests/sec** | 0.01-0.03 | 0.3-1.6 | 0.08-0.8 | 0.16-0.8 |
| **Session Duration** | 3-25 min | 2-10 min | 5-30 min | <2 min |
| **Total Requests** | 8-30 | 500-2000+ | 100-10000+ | 50-200 |

### Timing Pattern Comparison

| Aspect | Normal | Scraping | Credential Stuffing | Scalping |
|--------|--------|----------|---------------------|----------|
| **Gap Between Requests** | 10s-5min | 50-200ms | 100-500ms | 50-250ms |
| **Pattern** | Irregular | Mechanical | Mechanical | Coordinated |
| **Think Time** | Natural | None | None | None |
| **Superhuman Speed** | No | Yes | Yes | Yes |

### Behavioral Comparison

| Metric | Normal User | Scraping Bot | Credential Stuffing | Scalping Bot |
|--------|-------------|--------------|---------------------|--------------|
| **Conversion Rate** | 5-20% | 0% | <1% | 100% |
| **Failed Logins** | 0-2 | N/A | 50-1000+ | 0-1 |
| **Items Viewed** | 2-7 | 100-1000+ | 0 | 1 |
| **Cart Changes** | 0-3 | 0 | 0 | 0 |
| **Cart-to-Checkout** | 45s-5min | N/A | <5s | <5s |

### Identity Indicators

| Aspect | Normal | Scraping | Credential Stuffing | Scalping |
|--------|--------|----------|---------------------|----------|
| **Account Age** | Varied | No account | No account | <7 days |
| **IP Type** | Residential | Datacenter | Proxy/VPN | Mixed |
| **User-Agent** | Consistent | Scripted | Rotating | Consistent |
| **Geo Location** | Consistent | Varied | Mismatched | Clustered |

---

## Detection Strategies

### Multi-Layer Detection Approach

```
Layer 1: Request Rate Anomalies
├─ Threshold: >10 requests/minute sustained
├─ Action: Increase monitoring, soft rate limit
└─ Confidence: Low (20%)

Layer 2: Pattern Anomalies
├─ Sequential ID access: >10 sequential IDs
├─ Mechanical timing: <500ms gaps consistently
├─ Action: CAPTCHA challenge
└─ Confidence: Medium (50%)

Layer 3: Behavioral Anomalies
├─ Zero conversion rate + high volume
├─ 90%+ failed login rate
├─ Superhuman cart-to-checkout speed
├─ Action: Block IP, flag account
└─ Confidence: High (80%)

Layer 4: Multi-Signal Correlation
├─ High request rate + mechanical timing + datacenter IP + zero conversion
├─ Multiple accounts + same IP + simultaneous actions + limited edition item
├─ Action: Hard block, ban account/IP
└─ Confidence: Very High (95%)
```

### Risk Scoring Model

```python
def calculate_risk_score(session):
    score = 0.0

    # Request rate signals (0-30 points)
    if session.requests_per_minute > 10:
        score += 10
    if session.requests_per_minute > 50:
        score += 20

    # Timing pattern signals (0-20 points)
    if session.has_mechanical_timing:
        score += 15
    if session.average_gap_ms < 500:
        score += 5

    # Behavioral signals (0-30 points)
    if session.conversion_rate == 0 and session.total_requests > 50:
        score += 15
    if session.failed_login_rate > 0.9:
        score += 15
    if session.cart_to_checkout_seconds < 5:
        score += 10

    # Identity signals (0-20 points)
    if session.ip_is_datacenter:
        score += 10
    if session.account_age_days < 7:
        score += 5
    if session.user_agent_is_scripted:
        score += 5

    return min(score / 100, 1.0)  # Normalize to 0-1
```

### Response Actions by Risk Score

```
Risk Score: 0.0-0.3 (LOW)
└─ Action: Allow, normal monitoring

Risk Score: 0.3-0.6 (MEDIUM)
├─ Action: Increase monitoring
├─ Log detailed request patterns
└─ Prepare CAPTCHA challenge

Risk Score: 0.6-0.8 (HIGH)
├─ Action: CAPTCHA challenge required
├─ Rate limiting applied
└─ Flagged for human review

Risk Score: 0.8-1.0 (CRITICAL)
├─ Action: Block IP immediately
├─ Invalidate auth tokens
├─ Ban account if authenticated
└─ Alert security team
```

---

## Related Documentation

- [Normal User Journeys](./user-journeys-normal.md) - Legitimate customer behavior baselines
- [Book Store README](../README.md) - Service overview and API endpoints
- [System Design](../../../docs/system-design.md) - Overall architecture
- [Naglfar Layer Architecture](../../../docs/naglfar-layer-architecture.md) - Detection implementation

---

**Last Updated**: 2025-12-27
**Status**: Attack pattern definitions for detection system
**Priority**: Critical - These patterns inform Naglfar Analytics Worker rules
