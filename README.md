# Perimeter Data Gateway (PDG) v0.1

Perimeter Data Gateway (PDG) is a self-hosted .NET gateway for controlled, limited, and auditable access by software agents and internal services to data in existing corporate information systems.

PDG v0.1 demonstrates a narrow and testable trust boundary between a software Actor and an existing Corporate Data Source.

## Architecture at a Glance

- **No database credentials or arbitrary SQL for the Actor** — the Actor interacts only with predefined Published Resources.
- **Published Resource boundary** — resource parameters, row scope, output fields, and query shape are controlled by the gateway, not by the Actor.
- **Independent Subject and Actor identities** — delegation and Actor capabilities are validated independently of Subject permissions.
- **Deterministic Effective Access** — authorization is the intersection of validated JWT scope, Actor policy limits, permitted Subject–Actor delegation, Subject permissions, and Published Resource constraints.
- **Defense in Depth and least privilege** — application-level authorization is reinforced by an independent database security boundary and restricted runtime identities.
- **Audit before release / Fail Closed** — protected data is never released when mandatory audit cannot be persisted.
- **Separation of Platform Store and Corporate Data Source** — policy, configuration, delegation, permissions, and audit state are separated from corporate business data and use separate credentials and privilege sets.
- **Self-hosted and reproducible deployment** — bootstrap, security verification, and Docker Compose provide a reproducible environment.

## Core Request Flow

```text
Subject
  ↓
Actor
  ↓
Authentication
  ↓
Delegation / Capability / Policy
  ↓
Published Resource
  ↓
Corporate Data Source
  ↓
Audit
  ↓
Result
```

## Technology

- C# 12
- .NET 8 / ASP.NET Core
- Entity Framework Core
- Npgsql
- PostgreSQL
- JWT Bearer authentication
- Docker / Docker Compose
- xUnit

## Repository Structure

- `src/` — production source code
- `tests/` — unit, integration, and acceptance tests
- `db/` — database bootstrap and security scripts
- `docker/` — container build definitions
- `scripts/` — bootstrap and verification scripts
- `docs/` — technical requirements, design documents, implementation specifications, and engineering documentation

## Engineering Documentation

The detailed PDG v0.1 documentation is available here:

[docs/README.md](docs/README.md)

The documentation covers the Technical Requirements, Preliminary Design, Technical Working Project, implementation manifest, Platform Store design, security boundaries, bootstrap procedure, and acceptance criteria.
