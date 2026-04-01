# Snip

A real-time URL shortening platform built with event-driven architecture, real-time analytics, and distributed systems patterns.

## Overview

Snip is a full-stack URL shortener where users can create short links, share them, and watch click analytics update in real time. Every redirect triggers an event pipeline that flows through Kafka into ClickHouse, powering live dashboards with per-hour charts, device breakdowns, and country detection.

The primary goal of this project is to demonstrate distributed systems concepts: event-driven architecture, message brokers, caching strategies, OLAP analytics, and real-time communication — all running locally via Docker Compose.

---

## Architecture
```
Browser → YARP Gateway → Link Service (REST + SignalR)
                       → Auth Service (JWT)
                       → Redirect Service → Redis Cache → PostgreSQL

Redirect Service → Kafka (click.events)
                         ↓
        ┌────────────────┼────────────────┐
        ↓                ↓                ↓
Analytics Worker  Cache Invalidator  Notification Worker
(ClickHouse)      (Redis eviction)   (Milestone alerts)
        ↓
Link Service ← Kafka (click.events, link.alerts)
        ↓
SignalR → Frontend (live dashboard)
```

### Services

| Service | Purpose | Tech |
|---|---|---|
| **Snip.Gateway** | Single entry point, JWT validation, rate limiting | YARP, ASP.NET Core |
| **Snip.AuthService** | User registration, login, JWT issuance | ASP.NET Core Identity |
| **Snip.LinkService** | Link CRUD, analytics queries, SignalR hub | ASP.NET Core, EF Core |
| **Snip.RedirectService** | Slug resolution, Redis caching, GeoIP | ASP.NET Core |
| **Snip.AnalyticsWorker** | Kafka consumer, ClickHouse writer | .NET BackgroundService |
| **Snip.CacheInvalidator** | Kafka consumer, Redis cache eviction | .NET BackgroundService |
| **Snip.NotificationWorker** | Milestone detection, alert publishing | .NET BackgroundService |
| **Snip.Shared** | Shared models, events, constants | .NET Class Library |
| **snip-web** | React frontend | Vite, React, Recharts, SignalR |

### Infrastructure

| Service | Purpose | Port |
|---|---|---|
| PostgreSQL | Primary relational store (links, users) | 5433 |
| Redis | Slug cache + rate limiting | 6379 |
| Kafka (KRaft) | Event streaming | 9092 |
| ClickHouse | OLAP click analytics | 8123 |
| Jaeger | Distributed tracing UI | 16686 |

---

## Key Design Decisions

### Event-driven architecture
Every redirect publishes a `ClickEvent` to Kafka. Three independent consumers process it:
- **Analytics Worker** writes to ClickHouse (persistent analytics)
- **Cache Invalidator** evicts stale Redis entries when links change
- **Notification Worker** detects click milestones and publishes alerts

No service calls another directly for analytics — they communicate through Kafka topics.

### Two-tier caching
Redis sits in front of PostgreSQL for slug resolution. The cache-aside pattern means the first request for a slug hits PostgreSQL and warms Redis — subsequent requests never touch the database. TTL is 30 minutes.

Cache invalidation is event-driven: when a link is updated or deleted, the Link Service publishes a `LinkUpdated` or `LinkDeleted` event. The Cache Invalidator consumes it and evicts the Redis entry. The Link Service itself has no knowledge of Redis.

### ClickHouse for analytics
PostgreSQL is used for relational data (links, users). ClickHouse is used exclusively for click analytics. A `SELECT count()` query across millions of rows takes under a millisecond in ClickHouse due to its columnar storage and `MergeTree` engine ordered by `(slug, timestamp)`.

The live dashboard updates work as follows: a `ClickEventListener` BackgroundService inside the Link Service reads `click.events` from Kafka and sends a SignalR nudge to connected browsers. The browser then re-fetches analytics from ClickHouse. This means the displayed count is always the real total — no in-memory counting.

### Notification milestones
The Notification Worker initializes click counts from ClickHouse on startup, then tracks in-memory increments per slug. When a milestone is crossed (10, 50, 100, 500, 1000...) it publishes a `LinkAlertEvent` to Kafka. The Link Service consumes this and pushes a toast notification to the user's dashboard via SignalR.

### JWT authentication
Auth Service issues JWTs signed with HMAC-SHA256. The Gateway validates tokens before forwarding requests to protected routes. The Link Service also validates tokens independently for SignalR connections and analytics ownership checks.

All analytics endpoints verify that the requested slug belongs to the authenticated user — querying analytics for someone else's link returns 404.

### GeoIP detection
The Redirect Service uses MaxMind GeoLite2 (loaded in memory at startup) to detect country from IP address. Country is included in the `ClickEvent` and stored in ClickHouse. Localhost IPs resolve as "Unknown" — country data is only meaningful with real external traffic.

### Automatic migrations
- **PostgreSQL** (Link Service, Auth Service): `db.Database.Migrate()` runs on startup via EF Core
- **ClickHouse** (Analytics Worker): custom migration runner reads embedded `.sql` files, tracks applied migrations in a `schema_migrations` table

---

## Known Tradeoffs

### localStorage for JWT tokens
Tokens are stored in `localStorage` for simplicity. This is vulnerable to XSS attacks. The production hardening would be HttpOnly cookies, which JavaScript cannot read. For a portfolio project this is a standard and understood tradeoff.

### Public redirects
Short links are intentionally public — anyone with a slug can use it for redirecting. Only analytics are protected by ownership checks. This matches the behavior of real URL shorteners (Bitly, TinyURL).

### SignalR bypasses Gateway for WebSocket
SignalR connects through the Gateway (port 5000) which proxies WebSocket connections to the Link Service. The JWT token is passed via `accessTokenFactory` during the negotiate request. The hub itself does not require authorization — the security boundary is at the analytics API level, not the WebSocket level.

### ClickHouse queried on every live update
Every redirect triggers a SignalR nudge which causes the frontend to re-fetch analytics from ClickHouse. For high-traffic links this means many ClickHouse queries. In practice this is fine — ClickHouse is purpose-built for this workload and `count()` queries are sub-millisecond. A Redis counter approach was considered and rejected because it introduces a second source of truth that diverges on restart.

### Notification Worker in-memory counts
Click counts are initialized from ClickHouse on startup but tracked in memory afterwards. If the worker restarts, it re-initializes from ClickHouse correctly. `AutoOffsetReset.Latest` prevents replaying historical events — only new clicks are counted after restart.

### No pagination on analytics
The analytics queries return all data for a given time range without pagination. For links with millions of clicks this could be slow. Addressed with ClickHouse's columnar engine but worth noting for very high traffic scenarios.

### Dashboard pagination
The link dashboard paginates at 10 links per page. The default `pageSize` is fixed — future improvement would allow user-configurable page sizes.

### Chunk size warning
The production React bundle is ~666KB due to Recharts and SignalR being bundled together. Dynamic imports for the analytics page would split this into separate chunks loaded on demand.

---

## Running Locally

### Prerequisites
- Docker + Docker Compose
- .NET 10 SDK
- Node.js 20+
- MaxMind GeoLite2-Country.mmdb (optional — free account at maxmind.com, place in `src/Snip.RedirectService/`. Without it the service still runs but country detection is disabled and all clicks show "Unknown" country.)

### Start infrastructure
```bash
docker compose up -d
```

### Run all backend services (separate terminals)
```bash
cd src/Snip.AuthService && dotnet run
cd src/Snip.LinkService && dotnet run
cd src/Snip.RedirectService && dotnet run
cd src/Snip.Gateway && dotnet run
cd src/Snip.AnalyticsWorker && dotnet run
cd src/Snip.CacheInvalidator && dotnet run
cd src/Snip.NotificationWorker && dotnet run
```

### Run the frontend
```bash
cd frontend/snip-web
npm install --legacy-peer-deps
npm run dev
```

Open `http://localhost:5173`

### API documentation
- Link Service: `http://localhost:5089/swagger`
- Auth Service: `http://localhost:5297/swagger`

### Distributed tracing
Open `http://localhost:16686` to view traces in Jaeger.

---

## Project Structure
```
snip/
├── docker-compose.yml
├── README.md
├── src/
│   ├── Snip.AuthService/
│   ├── Snip.LinkService/
│   ├── Snip.RedirectService/
│   ├── Snip.Gateway/
│   ├── Snip.AnalyticsWorker/
│   ├── Snip.CacheInvalidator/
│   ├── Snip.NotificationWorker/
│   └── Snip.Shared/
└── frontend/
    └── snip-web/
```

---

## Tech Stack

**Backend:** .NET 10, ASP.NET Core, Entity Framework Core, ASP.NET Core Identity  
**Messaging:** Apache Kafka (KRaft mode, no Zookeeper)  
**Databases:** PostgreSQL 16, Redis 7, ClickHouse 24  
**Proxy:** YARP (Yet Another Reverse Proxy)  
**Observability:** Serilog, OpenTelemetry, Jaeger  
**Frontend:** React 19, Vite, Recharts, SignalR, CSS Modules  
**Auth:** JWT (HMAC-SHA256), ASP.NET Core Identity  
**GeoIP:** MaxMind GeoLite2  

---

## Future Improvements

- HttpOnly cookie storage for JWT tokens
- Custom domain support for short links
- Link expiry dates
- QR code generation per link
- Rate limiting per user (currently per IP at gateway level)
- Worker health check HTTP endpoints for Kubernetes readiness probes
- Dynamic frontend bundle splitting