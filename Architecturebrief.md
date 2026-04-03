# Microservices Reference Architecture — Project Brief

> This document is the authoritative reference for GitHub Copilot, Codex, and any AI agent
> working on this repository. Read this file before writing any code, creating any file,
> or making any architectural decision. Every section reflects a deliberate design choice
> that must be respected throughout the codebase.

---

## Table of contents

1. [Project purpose](#1-project-purpose)
2. [Business domain](#2-business-domain)
3. [Services and components](#3-services-and-components)
4. [Architecture constraints](#4-architecture-constraints)
5. [Event flow](#5-event-flow)
6. [Folder structure](#6-folder-structure)
7. [Events catalogue](#7-events-catalogue)
8. [Observability requirements](#8-observability-requirements)
9. [ADR topics](#9-adr-topics)
10. [What the README must demonstrate](#10-what-the-readme-must-demonstrate)
11. [Infrastructure — local hosting](#11-infrastructure--local-hosting)
12. [Interview mapping](#12-interview-mapping)

---

## 1. Project purpose

This project exists to answer one specific interview question that separates implementers
from architects:

> *"Have you designed a microservices system from scratch — and can you explain every
> decision you made?"*

This repository is a **reference architecture**, not a tutorial and not a demo. It should
read like something a real engineering team would use as a starting point. Every structural
decision has documented reasoning in an ADR. Every constraint is enforced structurally, not
just by convention.

**Target audience for this repo:**
- Hiring managers evaluating Lead / Staff / Principal Engineer candidates
- Technical interviewers running system design rounds
- Engineers using it as a reference for real microservices projects

**What this repo proves:**
- The author can decompose a domain into bounded contexts
- The author understands async communication and its consequences
- The author treats observability as a first-class concern, not an afterthought
- The author knows the difference between a working demo and a production-ready architecture

---

## 2. Business domain

**Domain: Order Management**

Order Management is used as the business domain because:
- It is universally understood — no domain knowledge required to evaluate it
- It decomposes naturally into independent bounded contexts
- It has genuine async communication requirements (not forced)
- It maps directly to financial services experience (trade lifecycle, settlement flow)
- It produces realistic event flows that demonstrate eventual consistency clearly

### Bounded contexts

**Orders** — the command centre. Accepts new orders, validates them, persists order state,
and publishes domain events. Owns its own database. Never calls Inventory or Notifications
directly under any circumstances.

**Inventory** — reacts to order events. Reserves stock against incoming orders, confirms
availability or signals insufficiency, and publishes its own events. Owns its own database.
Has no knowledge of Orders Service internals.

**Notifications** — purely reactive. Listens for events from both Orders and Inventory.
Sends confirmation or failure notifications to customers. Stateless beyond what it needs to
dispatch a message.

### Why three services and not more

Three services is a deliberate choice. Adding more services for the sake of it obscures the
architectural patterns. Three services is enough to demonstrate:
- Database-per-service
- Async event-driven communication
- Distributed tracing across process boundaries
- Independent deployability

A fourth service (e.g. Payments) is documented in the README as a "what I would add next"
item, showing the architecture is extensible without being over-engineered from the start.

---

## 3. Services and components

| Component | Technology | Purpose |
|---|---|---|
| Orders Service | .NET 8 Web API | Accepts and manages order lifecycle |
| Inventory Service | .NET 8 Web API | Manages stock reservation against orders |
| Notifications Service | .NET 8 Worker Service | Dispatches notifications on domain events |
| API Gateway | YARP (.NET 8) | Single entry point — routing, rate limiting |
| Message Broker | RabbitMQ 3.13 (Docker) | Async communication between all services |
| Distributed Tracing | OpenTelemetry + Seq | End-to-end trace visibility across services |
| Local Infrastructure | Docker Compose | Full stack in a single `docker-compose up` |

### Why YARP and not nginx or Kong

YARP (Yet Another Reverse Proxy) is a .NET-native reverse proxy library. It was chosen
because:
- Configuration is C# — no separate DSL to learn or maintain
- It runs as a .NET project — consistent tooling, debugging, and observability with the
  rest of the stack
- Route and cluster config can be loaded from `appsettings.json` or changed at runtime
- OpenTelemetry integration is native — gateway traces participate in the same pipeline
  as the services

nginx would require a separate config format and would sit outside the .NET observability
pipeline. Kong adds significant operational complexity for a reference architecture.

### Why RabbitMQ and not Azure Service Bus or Kafka

RabbitMQ runs locally in Docker at zero cost — no cloud subscription, no account required.
For a portfolio project this is the correct choice. The messaging abstraction (MassTransit)
supports swapping RabbitMQ for Azure Service Bus via a one-line configuration change. This
is documented in ADR 002 and called out in the README.

### Why Seq and not Grafana / Loki / Jaeger

Seq provides traces, logs, and metrics in a single UI with a generous free tier for a
single user. For a local reference architecture, one tool that shows all three observability
pillars is more valuable than a correctly configured but complex Grafana + Loki + Jaeger
stack. The architecture is OpenTelemetry-native — the backend is swappable.

---

## 4. Architecture constraints

These are non-negotiable. Every constraint is a deliberate decision documented in an ADR.
Copilot and Codex must never violate these constraints regardless of what would be simpler
or faster to implement.

### Constraint 1 — No synchronous inter-service calls

Services **never** call each other via HTTP or gRPC. All cross-service communication is
event-driven via RabbitMQ. If Orders Service needs to know that inventory has been reserved,
it waits for a `StockReserved` event — it never calls Inventory's API endpoint directly.

**Why this matters:** Synchronous inter-service calls create runtime coupling. If Inventory
is down, Orders fails too. Async communication gives temporal decoupling — services can be
deployed, restarted, and scaled independently.

**How it is enforced:** Services have no HTTP client configuration pointing at other
services. The only outbound network calls from a service are to its own database and to
RabbitMQ.

### Constraint 2 — Database per service, strictly enforced

Orders Service owns `orders_db`. Inventory Service owns `inventory_db`. There are no shared
tables, no shared connection strings, and no cross-database queries anywhere in the codebase.

Each database is a separate container in Docker Compose. Each service's connection string
is in its own `appsettings.json`. No service has a connection string pointing to another
service's database.

**Why this matters:** A shared database is the most common way microservices become a
distributed monolith. Separate databases enforce the bounded context boundary at the
infrastructure level, not just the code level.

### Constraint 3 — All external traffic enters via the gateway

No service exposes a public-facing port except the YARP gateway on port 8080. In Docker
Compose, individual service ports are mapped to internal Docker network ports only. All
external consumers — including Swagger UI, integration tests, and curl commands — go
through the gateway.

**Why this matters:** The gateway is the single place for cross-cutting concerns — auth
header validation, rate limiting, routing versioning. Bypassing it in development creates
habits that break in production.

### Constraint 4 — Observability is first-class, not an afterthought

Every service instruments OpenTelemetry traces, metrics, and structured logs from the first
commit. Trace context propagates across service boundaries via RabbitMQ message headers.
No service uses `Console.WriteLine` or unstructured logging anywhere.

**Why this matters:** Retrofitting observability into an existing microservices system is
significantly harder than building it in from the start. This constraint demonstrates that
the author understands production operations, not just development.

### Constraint 5 — Services are independently deployable

Each service has its own `Dockerfile`. Each service has its own solution folder. Each
service has its own database migration strategy. Adding a fourth service requires changes
only to `docker-compose.yml`, the gateway routing config, and the new service folder —
nothing inside the existing three services changes.

---

## 5. Event flow

This is the core architecture story. This diagram should be on the whiteboard in any
Lead / Staff interview discussion about this project.

```
External Client (curl / Swagger / Frontend)
         │
         ▼
  YARP Gateway  :8080
         │
         ▼
  Orders Service
         │
         │  publishes ──► OrderPlaced
         │
         ▼
     RabbitMQ
         │
         ├─────────────────────────────────────┐
         ▼                                     ▼
  Inventory Service                  Notifications Service
  consumes: OrderPlaced              consumes: OrderPlaced
  reserves stock                     sends: "order received" notification
         │
         │  publishes ──► StockReserved
         │             or StockInsufficient
         │
         ▼
     RabbitMQ
         │
         ├─────────────────────────────────────┐
         ▼                                     ▼
  Orders Service                     Notifications Service
  consumes: StockReserved            consumes: StockReserved
  updates status → Confirmed         sends: "order confirmed" notification
         │
  consumes: StockInsufficient        consumes: StockInsufficient
  updates status → Failed            sends: "order failed" notification
```

### Key properties of this flow

- **Zero HTTP calls between services** — every arrow between services is a RabbitMQ event
- **Each service reacts independently** — Notifications does not wait for Inventory
- **Eventual consistency** — Orders status updates asynchronously after Inventory responds
- **Every step is traceable** — trace context propagates through every RabbitMQ message
  header so the full flow is visible as a single trace in Seq

---

## 6. Folder structure

```
microservices-reference-arch/
│
├── docs/
│   ├── architecture.md              ← C4 diagrams, event flow, system overview
│   ├── events.md                    ← formal event catalogue (see section 7)
│   ├── runbook.md                   ← how to run, debug, and observe locally
│   └── adr/
│       ├── 001-database-per-service.md
│       ├── 002-async-only-inter-service-comms.md
│       ├── 003-gateway-single-entry-point.md
│       ├── 004-opentelemetry-first.md
│       └── 005-eventual-consistency.md
│
├── src/
│   │
│   ├── Gateway/
│   │   └── Gateway.API/             ← YARP reverse proxy, routing config
│   │
│   ├── Services/
│   │   │
│   │   ├── Orders/
│   │   │   ├── Orders.API/          ← controllers, middleware, DI wiring
│   │   │   ├── Orders.Application/  ← CQRS handlers, interfaces, DTOs
│   │   │   ├── Orders.Domain/       ← entities, enums, domain events
│   │   │   └── Orders.Infrastructure/ ← EF Core, repos, RabbitMQ publishers
│   │   │
│   │   ├── Inventory/
│   │   │   ├── Inventory.API/
│   │   │   ├── Inventory.Application/
│   │   │   ├── Inventory.Domain/
│   │   │   └── Inventory.Infrastructure/
│   │   │
│   │   └── Notifications/
│   │       └── Notifications.Worker/ ← .NET Worker Service, event consumers only
│   │
│   └── Shared/
│       ├── Shared.Contracts/        ← event message schemas only — no business logic
│       └── Shared.Observability/    ← OpenTelemetry setup, reusable across services
│
├── docker-compose.yml               ← full local stack
├── docker-compose.override.yml      ← local dev port overrides
├── .env.example                     ← all required env vars documented, no values
├── ARCHITECTURE_BRIEF.md            ← this file
└── README.md
```

### Notes on the Shared projects

`Shared.Contracts` is the **only** thing services share. It contains event message schemas
as plain C# record types — nothing else. No repositories, no domain entities, no business
logic, no EF Core references. It is the contract that defines what events look like on the
wire. All three services reference this project.

`Shared.Observability` contains the OpenTelemetry configuration extension method that each
service calls during startup (`builder.Services.AddObservability(...)`). This ensures
consistent instrumentation across all services without duplicating configuration.

Both shared projects are internal NuGet-style packages within the solution. They are not
published externally.

---

## 7. Events catalogue

This catalogue lives in `docs/events.md` and is the formal contract between services.
Changing an event schema is a breaking change. All events carry a `version` field from
day one.

| Event | Publisher | Subscribers | Key payload fields |
|---|---|---|---|
| `OrderPlaced` | Orders | Inventory, Notifications | OrderId, CustomerId, Items[], TotalAmount, PlacedAt, Version |
| `StockReserved` | Inventory | Orders, Notifications | OrderId, ReservedAt, Items[], Version |
| `StockInsufficient` | Inventory | Orders, Notifications | OrderId, FailedAt, Reason, Version |
| `OrderConfirmed` | Orders | Notifications | OrderId, ConfirmedAt, Version |
| `OrderFailed` | Orders | Notifications | OrderId, FailedAt, Reason, Version |
| `NotificationSent` | Notifications | — | OrderId, Channel, RecipientId, SentAt, Version |

### Event schema conventions

- All event names are past tense — they describe something that **has happened**
- All event IDs are `Guid`
- All timestamps are UTC ISO 8601
- All events carry `Version: 1` from initial implementation
- All events carry `CorrelationId` (same as the originating HTTP request trace ID)
- No event contains a nested object deeper than two levels
- Events are immutable once published — handlers must be idempotent

### Dead letter queue strategy

Each consumer queue has a corresponding dead letter queue (DLQ):
- `orders.placed` → `orders.placed.dlq`
- `stock.reserved` → `stock.reserved.dlq`
- `stock.insufficient` → `stock.insufficient.dlq`

Failed messages are routed to the DLQ after three retry attempts with exponential backoff
(5s, 25s, 125s). DLQ contents are visible in the RabbitMQ management UI. Re-processing DLQ
messages is documented in `docs/runbook.md`.

---

## 8. Observability requirements

Observability is a first-class architectural concern in this project, not a post-deployment
addition. The goal is that a single business transaction — one order placed — is fully
traceable from the initial HTTP request through every service, every event handler, and
every database call, visible as a single correlated trace in Seq.

### Three pillars — all required

**Traces**

- Every incoming HTTP request to the gateway generates a trace ID
- Trace context propagates via RabbitMQ message headers (`traceparent`, `tracestate`)
- Every event handler continues the trace from the message headers
- In Seq, searching by `OrderId` shows the complete journey:
  `Gateway → Orders API → RabbitMQ publish → Inventory consumer → RabbitMQ publish → Orders consumer → Notifications consumer`
- Span names follow the format: `{ServiceName}.{Operation}` e.g. `Orders.PlaceOrder`,
  `Inventory.ReserveStock`

**Metrics**

Each service emits the following metrics via OpenTelemetry:
- `http.server.request.duration` — HTTP request latency (histogram)
- `messaging.publish.duration` — time to publish a message to RabbitMQ
- `messaging.process.duration` — time to process a consumed message
- `orders.placed.total` — counter, Orders Service
- `stock.reservations.total` — counter with `result` label (reserved / insufficient)
- `notifications.sent.total` — counter with `channel` label

**Structured logs**

- All logging via Serilog with OpenTelemetry sink
- Every log entry includes: `ServiceName`, `TraceId`, `SpanId`, `OrderId` (where in scope),
  `Environment`
- No `Console.WriteLine` anywhere in the codebase
- Log levels: `Information` for business events, `Warning` for handled failures,
  `Error` for unhandled exceptions, `Debug` for development-only detail

### Seq configuration

Seq runs in Docker on port 5341 (ingestion) and port 8081 (UI). Free single-user licence,
no account required. Connection string: `http://localhost:5341`.

All services send OpenTelemetry data to Seq via OTLP exporter. Seq receives traces, metrics,
and logs in a single pipeline.

### The README Seq screenshot

The README must include a screenshot of Seq showing a distributed trace for a single
`OrderPlaced` event spanning all three services. This screenshot is more persuasive in an
interview context than any amount of code. It proves the observability stack actually works
end to end.

---

## 9. ADR topics

Five Architecture Decision Records are required. Each lives in `docs/adr/` as a markdown
file. Each follows this format:

```
# ADR-00X: Title
Date: YYYY-MM-DD
Status: Accepted

## Context
Why was this decision needed? What problem does it solve in this specific project?

## Decision
What was decided and how it is implemented in this codebase.

## Consequences
### Positive
- benefit 1
- benefit 2
### Negative / Trade-offs
- trade-off 1
- trade-off 2

## Alternatives considered
- Alternative A — reason it was rejected
- Alternative B — reason it was rejected
```

### ADR 001 — Database per service

**Context:** Multiple services need to persist state. A shared database is the path of least
resistance.

**Decision:** Each service owns exactly one database. No service has a connection string
pointing to another service's database. No cross-database joins anywhere in the codebase.

**Positive consequences:** True bounded context isolation. Services can use different
database engines if needed. Schema changes in one service never break another.

**Negative consequences:** No joins across service boundaries. Reporting queries require
aggregation at the application layer or a separate read model. More Docker containers to
manage locally.

**Alternatives rejected:** Shared database (creates hidden coupling, defeats the purpose
of microservices). Shared schema with separate tables (still creates coupling via foreign
key relationships).

---

### ADR 002 — Async-only inter-service communication

**Context:** Services need to communicate. HTTP is the simplest option.

**Decision:** No service calls another service via HTTP or gRPC. All cross-service
communication is asynchronous via RabbitMQ events. MassTransit is used as the messaging
abstraction — swapping RabbitMQ for Azure Service Bus requires a one-line config change.

**Positive consequences:** Temporal decoupling — services can be deployed and restarted
independently. No cascading failures when a downstream service is unavailable. Natural
audit trail via event log.

**Negative consequences:** Eventual consistency — callers cannot get an immediate
synchronous answer from downstream services. Debugging requires distributed tracing.
Idempotency must be implemented on all event handlers.

**Alternatives rejected:** Synchronous HTTP (creates runtime coupling — if Inventory is
down, Orders fails). gRPC (still synchronous, adds proto schema complexity without solving
the coupling problem).

---

### ADR 003 — Gateway as single entry point

**Context:** Three services need to be accessible to external clients. Each could expose
its own port.

**Decision:** YARP gateway is the only externally exposed service. All external traffic
enters on port 8080. Individual service ports are internal to the Docker network only.

**Positive consequences:** Single place for cross-cutting concerns (auth header validation,
rate limiting, routing versioning). Internal topology is hidden from consumers. Easy to add
or rename services without changing external URLs.

**Negative consequences:** Gateway is a critical path dependency — if it is down, all
services are unreachable. Requires careful health checking and circuit breaker configuration
for production.

**Alternatives rejected:** Direct service exposure (leaks internal topology to consumers,
no single place for cross-cutting concerns). Kong / nginx (separate config DSL, outside the
.NET observability pipeline, operational overhead).

---

### ADR 004 — OpenTelemetry from day one

**Context:** Microservices are inherently harder to debug than monoliths. Observability
could be added after features are built.

**Decision:** OpenTelemetry instrumentation is added in the first commit, before any
business logic. All three pillars — traces, metrics, structured logs — are required from
the start. Seq is the local backend. The stack is backend-agnostic via OTLP.

**Positive consequences:** Distributed traces are available from the first end-to-end test.
Trace context propagates correctly through RabbitMQ from the beginning. No retrofitting
required. Observable system from day one.

**Negative consequences:** Small per-request overhead. Requires a running Seq container
locally. OpenTelemetry SDK adds NuGet package weight.

**Alternatives rejected:** Logging only (no cross-service correlation, cannot trace a
request across process boundaries). Proprietary APM vendor (vendor lock-in, cost, not
portable to a portfolio project).

---

### ADR 005 — Eventual consistency as the default model

**Context:** In a distributed system with async communication, strong consistency is
achievable only with significant coupling or performance cost.

**Decision:** Eventual consistency is the default. Order status transitions asynchronously
after downstream events are processed. All event handlers are idempotent — processing the
same event twice produces the same result. The API response for `POST /orders` returns
`202 Accepted` with an `OrderId`, not the final confirmed order.

**Positive consequences:** Services are truly decoupled. No distributed transactions.
Natural fit with event-driven architecture.

**Negative consequences:** UI must handle a `Pending` state. Clients must poll or use
webhooks to get the final order status. Compensating transaction logic is required for
failure cases (e.g. stock insufficient after order placed).

**Alternatives rejected:** Two-phase commit (severe performance cost, notoriously difficult
to implement correctly in distributed systems, rarely used in practice). Saga pattern with
choreography (this is what this architecture implements — the name is just not emphasised).
Strong consistency via synchronous calls (reintroduces the coupling this architecture
is designed to avoid).

---

## 10. What the README must demonstrate

The README is the whiteboard. A hiring manager or technical lead should be able to evaluate
the architectural thinking of this project from the README alone, without reading any code.

### Required sections in README.md

**1. Project context**
One paragraph explaining what this is, why it was built, and what architectural thinking
it demonstrates. Mention Lead / Staff level interview preparation explicitly — this sets
expectations correctly.

**2. C4 context diagram (Mermaid)**
Shows: external actor (customer), the Order Management System boundary, and the external
systems it interacts with (email / notification channel). No internal detail at this level.

**3. C4 container diagram (Mermaid)**
Shows: YARP Gateway, Orders Service, Inventory Service, Notifications Worker, RabbitMQ,
orders_db, inventory_db, Seq. Shows communication direction and protocol
(HTTP for gateway→service, AMQP for service→RabbitMQ).

**4. Event flow diagram (Mermaid)**
The full event flow from section 5 of this document, rendered as a Mermaid sequence diagram.

**5. Key architectural decisions**
Four bullet points, each linking to the corresponding ADR file. One sentence summary per
decision.

**6. How to run locally**
Three commands:
```bash
git clone https://github.com/harshil-sh/microservices-reference-arch
cd microservices-reference-arch
docker-compose up
```
That is the entire local setup. All services, databases, RabbitMQ, and Seq start together.

**7. How to observe a request end to end**
Step-by-step walkthrough:
- Step 1: Place an order via curl or Swagger at `http://localhost:8080/orders`
- Step 2: Open RabbitMQ management UI at `http://localhost:15672` — observe the
  `order.placed` queue receive and process a message
- Step 3: Open Seq at `http://localhost:8081` — search by the returned `OrderId`
- Step 4: Observe the full distributed trace spanning Orders → Inventory → Notifications
- Step 5: Check order status via `GET /orders/{id}` — status should be `Confirmed`

**8. Services and ports reference table**

| Service | Internal port | External port | UI |
|---|---|---|---|
| YARP Gateway | 80 | 8080 | — |
| Orders Service | 80 | internal only | Swagger via gateway |
| Inventory Service | 80 | internal only | Swagger via gateway |
| Notifications Worker | — | — | — |
| RabbitMQ | 5672 | 5672 | :15672 |
| Seq | 5341 | 5341 | :8081 |
| orders_db (SQL Server) | 1433 | internal only | — |
| inventory_db (SQL Server) | 1433 | internal only | — |

**9. Tech stack table**

| Layer | Technology |
|---|---|
| Services | .NET 8 Web API / Worker Service |
| Gateway | YARP (.NET 8) |
| Messaging | RabbitMQ 3.13 + MassTransit |
| Databases | SQL Server 2022 (Docker) + EF Core 8 |
| Observability | OpenTelemetry + Seq |
| Infrastructure | Docker Compose |
| Testing | xUnit + Moq + Testcontainers |

**10. What I would add for production**
This section signals that the author understands the gap between a reference architecture
and production-ready infrastructure. Include:
- Kubernetes manifests and Helm charts for each service
- Azure Service Bus replacing RabbitMQ (one-line MassTransit config change)
- Azure Blob Storage for any file-based payloads
- Polly circuit breakers on all outbound calls
- Azure Key Vault for secrets management
- Separate read models / projections for reporting queries
- API versioning strategy via the gateway
- A Payments Service as the fourth bounded context

**11. Seq screenshot**
A screenshot of Seq showing a real distributed trace for a single `OrderPlaced` event
spanning all three services. This is the single most compelling evidence in the repository
for a technical interviewer.

---

## 11. Infrastructure — local hosting

The entire stack runs locally via Docker Compose at zero cost.

```yaml
# Services included in docker-compose.yml

rabbitmq:
  image: rabbitmq:3.13-management
  ports:
    - "5672:5672"    # AMQP — services connect here
    - "15672:15672"  # Management UI — http://localhost:15672

seq:
  image: datalust/seq:latest
  ports:
    - "5341:5341"    # OTLP ingestion
    - "8081:80"      # Seq UI — http://localhost:8081
  environment:
    ACCEPT_EULA: Y

orders-db:
  image: mcr.microsoft.com/mssql/server:2022-latest
  environment:
    SA_PASSWORD: ${ORDERS_DB_PASSWORD}
    ACCEPT_EULA: Y

inventory-db:
  image: mcr.microsoft.com/mssql/server:2022-latest
  environment:
    SA_PASSWORD: ${INVENTORY_DB_PASSWORD}
    ACCEPT_EULA: Y
```

**Total cost: £0.** No accounts, no sign-ups, no trial periods.

---

## 12. Interview mapping

Every question a Lead / Staff interviewer could ask about microservices architecture
has a documented, reasoned answer somewhere in this repository.

| Interview question | Where the answer lives |
|---|---|
| Have you designed microservices from scratch? | Folder structure + ADRs |
| How do services communicate in your design? | ADR 002 + events catalogue |
| How do you handle a service being unavailable? | ADR 002 (temporal decoupling) + DLQ strategy |
| How do you trace a request across services? | ADR 004 + Seq screenshot in README |
| What is the hardest part of microservices? | ADR 005 (eventual consistency) with specific consequences |
| How would you scale this for production? | README "what I would add next" section |
| Why YARP and not nginx? | ADR 003 + Services and components section |
| How do you prevent a shared database anti-pattern? | ADR 001 — enforced structurally |
| What does database-per-service cost you? | ADR 001 negative consequences |
| How would you add a fourth service? | Constraint 5 — independent deployability |

---

*This document was authored as part of a senior engineering portfolio project.
Last updated: April 2026.*