# Snip

A real-time URL intelligence platform built with event-driven architecture.

## Architecture

Snip is composed of six services communicating through Kafka:

- **Link Service** — create, update, delete short links (REST API)
- **Redirect Service** — resolve slugs and redirect with Redis caching
- **Analytics Worker** — consumes click events, writes to ClickHouse
- **Cache Invalidator** — consumes link change events, evicts stale Redis entries
- **Notification Worker** — detects thresholds, pushes live alerts via SignalR
- **Gateway** — YARP reverse proxy with routing and rate limiting

## Infrastructure

| Service     | Purpose                     | Port  |
|-------------|-----------------------------|-------|
| PostgreSQL  | Primary relational store     | 5432  |
| Redis       | Slug cache + rate limiting   | 6379  |
| Kafka       | Event streaming              | 9092  |
| ClickHouse  | Analytical click data        | 8123  |

## Running locally

### Prerequisites
- Docker + Docker Compose
- .NET 10 SDK

### Start infrastructure
docker-compose up -d

### Run a service
cd src/Snip.LinkService
dotnet run