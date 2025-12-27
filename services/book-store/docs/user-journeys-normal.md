# Normal User Journeys - Book Store

> Legitimate customer behavior patterns for purchasing books

**Document Purpose**: Define baseline legitimate user behavior for comparison against abusive patterns

---

## Table of Contents
1. [Journey 1: Targeted Purchase (Quick Buyer)](#journey-1-targeted-purchase-quick-buyer)
2. [Journey 2: Browser/Researcher (Deliberate Buyer)](#journey-2-browserresearcher-deliberate-buyer)
3. [Normal Behavior Patterns](#normal-behavior-patterns)

---

## Journey 1: Targeted Purchase (Quick Buyer)

**Persona**: Sarah, a returning customer who knows exactly what book she wants

**Duration**: 3-5 minutes
**Total Requests**: ~8-10
**Behavior**: Efficient, goal-oriented, minimal browsing

### Step-by-Step Flow

```
1. Landing & Authentication (0:00)
   POST /api/v1/auth/login
   → Credentials: sarah@email.com
   → Response: auth-token (valid for session)
   → Timing: Immediate

2. Search for Specific Book (0:15 - 15 seconds later)
   GET /api/v1/books?search="Clean Code"
   → Filters by title/author
   → Response: List of matching books
   → Timing: 15 seconds (reading login confirmation)

3. View Book Details (0:30 - 30 seconds in)
   GET /api/v1/books/42
   → Book: "Clean Code" by Robert Martin
   → Response: Title, price ($45), description, reviews
   → Timing: 15 seconds after search

4. Check Inventory (0:45)
   GET /api/v1/inventory?book_id=42
   → Response: In stock (12 available)
   → Timing: Immediate after viewing details

5. Add to Cart (1:00 - 1 minute in)
   POST /api/v1/cart/items
   → Body: {book_id: 42, quantity: 1}
   → Response: Cart updated, total: $45
   → Timing: 15 seconds (reading reviews, deciding)

6. Review Cart (1:15)
   GET /api/v1/cart
   → Response: 1 item, subtotal $45, tax $3.60, total $48.60
   → Timing: 15 seconds after adding

7. Proceed to Checkout (2:00)
   POST /api/v1/checkout
   → Body: {payment_method: "card_ending_1234"}
   → Response: Order confirmation, order_id: 789
   → Timing: 45 seconds (reviewing cart, confirming)

8. View Order Confirmation (2:15)
   GET /api/v1/orders/789
   → Response: Order details, estimated delivery
   → Timing: 15 seconds after checkout
```

### Behavioral Characteristics

**Request Pattern**: Linear, sequential flow
**Timing**: Natural pauses (15-45 seconds between actions)
**IP Address**: Single, consistent (e.g., 203.0.113.45)
**Auth Token**: Same token throughout session
**User-Agent**: Consistent (Chrome/Mac)
**Geolocation**: Matches billing address (New York, US)
**Failed Attempts**: None
**Retry Behavior**: None needed

### Timeline Visualization

```
0:00 ━━━━ Login
      ⏱️ 15s
0:15 ━━━━ Search
      ⏱️ 15s
0:30 ━━━━ View Book
      ⏱️ 15s
0:45 ━━━━ Check Inventory
      ⏱️ 15s
1:00 ━━━━ Add to Cart
      ⏱️ 15s
1:15 ━━━━ Review Cart
      ⏱️ 45s
2:00 ━━━━ Checkout
      ⏱️ 15s
2:15 ━━━━ Confirmation
```

---

## Journey 2: Browser/Researcher (Deliberate Buyer)

**Persona**: Mike, a new customer browsing for a gift, comparing options

**Duration**: 15-25 minutes
**Total Requests**: ~20-30
**Behavior**: Exploratory, comparison shopping, hesitant

### Step-by-Step Flow

```
1. Browse Without Login (0:00)
   GET /api/v1/books
   → Response: List of featured books (20 items)
   → Timing: Landing on site

2. Filter by Category (0:30)
   GET /api/v1/books?category=programming
   → Response: 45 programming books
   → Timing: 30 seconds (scrolling through featured)

3. View Multiple Books (1:00 - 5:00)
   GET /api/v1/books/15  (1:00) → "Design Patterns" $52
   GET /api/v1/books/23  (1:45) → "Refactoring" $48
   GET /api/v1/books/42  (2:30) → "Clean Code" $45
   GET /api/v1/books/56  (3:15) → "Code Complete" $55
   GET /api/v1/books/15  (4:00) → Back to "Design Patterns"
   → Timing: 45s - 1min between each (reading descriptions)

4. Check Inventory for Top Choices (5:30)
   GET /api/v1/inventory?book_id=15  → 5 in stock
   GET /api/v1/inventory?book_id=42  → 12 in stock
   → Timing: 30 seconds after last book view

5. Register Account (6:00)
   POST /api/v1/auth/register
   → Body: {email: mike@email.com, password: "***"}
   → Response: Account created, auto-login, auth-token
   → Timing: 30 seconds (deciding to create account)

6. Return to Favorite Book (7:00)
   GET /api/v1/books/42  → "Clean Code" again
   → Timing: 1 minute (registration confirmation email check)

7. Add First Item to Cart (8:00)
   POST /api/v1/cart/items
   → Body: {book_id: 42, quantity: 1}
   → Response: Cart updated
   → Timing: 1 minute (final decision)

8. Continue Browsing (9:00 - 12:00)
   GET /api/v1/books?category=programming  (9:00)
   GET /api/v1/books/67  (10:00) → "Pragmatic Programmer" $43
   GET /api/v1/inventory?book_id=67  (10:30) → 8 in stock
   → Timing: Long pauses, reconsidering

9. Add Second Item (12:30)
   POST /api/v1/cart/items
   → Body: {book_id: 67, quantity: 1}
   → Response: Cart updated, 2 items
   → Timing: 2 minutes (deciding on second book)

10. Review Cart Multiple Times (13:00 - 17:00)
    GET /api/v1/cart  (13:00) → 2 items, $95.60 total
    GET /api/v1/cart  (15:00) → Still reviewing
    → Timing: Long pause (budget consideration)

11. Remove One Item (17:30)
    DELETE /api/v1/cart/items/67
    → Response: Cart updated, 1 item remaining
    → Timing: 30 seconds after last cart view

12. Final Cart Review (18:00)
    GET /api/v1/cart
    → Response: 1 item, $48.60 total
    → Timing: 30 seconds after removal

13. Checkout (20:00)
    POST /api/v1/checkout
    → Body: {payment_method: "new_card"}
    → Response: Order confirmation, order_id: 790
    → Timing: 2 minutes (entering payment info)

14. Order Confirmation (20:30)
    GET /api/v1/orders/790
    → Response: Order details
    → Timing: 30 seconds
```

### Behavioral Characteristics

**Request Pattern**: Non-linear, back-and-forth, exploratory
**Timing**: Variable pauses (30s - 2min), realistic hesitation
**IP Address**: Single, consistent (e.g., 198.51.100.88)
**Auth Token**: Created mid-session (after browsing), consistent after
**User-Agent**: Consistent (Firefox/Windows)
**Geolocation**: Consistent (Chicago, US)
**Cart Modifications**: Added 2, removed 1 (normal behavior)
**Re-views**: Multiple visits to same book (comparison shopping)
**Failed Attempts**: None
**Retry Behavior**: Cart modifications (expected)

### Timeline Visualization

```
0:00 ━━━━ Browse (no login)
      ⏱️ 30s
0:30 ━━━━ Filter Category
      ⏱️ 30s-1min (view multiple books)
1:00-5:00 ━━━━ View 5 Different Books
      ⏱️ 30s
5:30 ━━━━ Check Inventory
      ⏱️ 30s
6:00 ━━━━ Register Account
      ⏱️ 1min
7:00 ━━━━ Return to Favorite
      ⏱️ 1min
8:00 ━━━━ Add to Cart
      ⏱️ 1-3min (continue browsing)
9:00-12:00 ━━━━ Browse More Books
      ⏱️ 30s
12:30 ━━━━ Add Second Item
      ⏱️ 30s-2min (review multiple times)
13:00-17:00 ━━━━ Review Cart
      ⏱️ 30s
17:30 ━━━━ Remove Item
      ⏱️ 30s
18:00 ━━━━ Final Review
      ⏱️ 2min
20:00 ━━━━ Checkout
      ⏱️ 30s
20:30 ━━━━ Confirmation
```

---

## Normal Behavior Patterns

### Comparison Matrix

| Aspect | Journey 1 (Quick Buyer) | Journey 2 (Browser) |
|--------|------------------------|---------------------|
| **Duration** | 3-5 minutes | 15-25 minutes |
| **Total Requests** | 8-10 | 20-30 |
| **Pattern** | Linear | Exploratory |
| **Login Timing** | Immediate | Mid-session |
| **Cart Changes** | None | Multiple |
| **Book Views** | 1-2 | 5-7 |
| **Decision Time** | Fast | Deliberate |
| **Requests/Minute** | 1.6-3.3 | 0.8-2.0 |

### Common Legitimate Indicators

✅ **Timing Patterns**
- Natural pauses between actions (10s - 5min)
- Realistic reading time before decisions
- Human-speed interactions (not mechanical)
- Variable timing (not perfectly consistent)

✅ **Request Patterns**
- Logical flow: browse → detail → cart → checkout
- Reasonable number of requests (8-30 per session)
- Low request rate (0.5-3 requests per minute)
- Follows expected e-commerce funnel

✅ **Identity Consistency**
- Single, consistent IP address throughout session
- Same User-Agent string across requests
- Geographic location matches billing/shipping address
- Auth token remains consistent per session

✅ **Behavioral Authenticity**
- Cart modifications are reasonable (add/remove normal)
- Zero or very low failed login attempts (0-2 max)
- Checkout follows cart review (not instantaneous)
- Re-visiting items during comparison (human indecision)

✅ **Conversion Indicators**
- Natural checkout completion (not superhuman speed)
- Browsing history before purchase
- Account age varies (new and returning customers)
- Payment methods appear legitimate

### Request Rate Analysis

**Normal User Baseline:**
```
Quick Buyer:    8-10 requests / 3-5 min   = 1.6-3.3 req/min
Browser:       20-30 requests / 15-25 min = 0.8-2.0 req/min

Average:       ~1.0-2.5 requests per minute
Peak:          ~3-4 requests per minute (during active browsing)
Sustained:     Never sustains >5 req/min for >1 minute
```

**Threshold for Anomaly Detection:**
```
⚠️  Warning:   >5 requests per minute sustained
🚨 Critical:   >10 requests per minute sustained
🔴 Blocked:    >20 requests per minute sustained
```

---

## Use Cases for Detection

### Baseline Metrics

These journeys establish baselines for:

1. **Request Rate**: 0.8-3.3 requests/minute (normal range)
2. **Session Duration**: 3-25 minutes (typical shopping session)
3. **Timing Gaps**: 10s-5min between actions (human speed)
4. **Conversion Rate**: 5-20% (typical e-commerce conversion)
5. **Cart-to-Checkout Time**: 45s-5min (decision making time)
6. **Items Viewed**: 1-7 books per session (human attention span)
7. **Failed Logins**: 0-2 attempts (legitimate user mistakes)

### Naglfar Analytics Detection

**Normal behavior should:**
- ✅ Be allowed without challenges
- ✅ Have low risk scores (0.0-0.3)
- ✅ Pass through validation quickly
- ✅ Not trigger rate limiting

**Any significant deviation from these patterns should:**
- ⚠️ Increase risk score
- 🚨 Trigger additional monitoring
- 🔴 Potentially block if multiple anomalies detected

---

## Related Documentation

- [Abusive User Journeys](./user-journeys-abusive.md) - Attack patterns and abuse scenarios
- [Book Store README](../README.md) - Service overview and API endpoints
- [System Design](../../../docs/system-design.md) - Overall architecture
- [Naglfar Layer Architecture](../../../docs/naglfar-layer-architecture.md) - Detection mechanisms

---

**Last Updated**: 2025-12-27
**Status**: Baseline for abuse detection system
