# Perimeter Data Gateway v0.1 — Documentation

This directory contains the normative public documentation set for **Perimeter Data Gateway (PDG) v0.1**.

## Reading order

1. [`technical-requirements-v0.1.md`](technical-requirements-v0.1.md) — approved requirements for the v0.1 Preliminary Design / Prototype / Proof of Feasibility stage.
2. [`preliminary-design-explanatory-note-v0.1.docx`](preliminary-design-explanatory-note-v0.1.docx) — engineering rationale, trust-boundary analysis, design alternatives, residual risks, and preliminary-design groundwork.
3. [`technical-working-project-explanatory-note-v0.1.docx`](technical-working-project-explanatory-note-v0.1.docx) — implementation-level Technical Working Project (TWP) design.
4. [`appendix-a-implementation-file-manifest-v0.1.docx`](appendix-a-implementation-file-manifest-v0.1.docx) — complete planned implementation file manifest and acceptance-test file mapping.
5. [`appendix-b-ef-core-migrations-and-platform-store-bootstrap-v0.1.docx`](appendix-b-ef-core-migrations-and-platform-store-bootstrap-v0.1.docx) — normative clarification of the Platform Store schema lifecycle, EF Core Migrations, migration bundle, bootstrap identities, grants, seed, and security verification.

## Document precedence

- **Technical Requirements** define what PDG v0.1 must provide and are approved.
- The **Preliminary Design Explanatory Note** explains the architectural rationale and the engineering hypothesis.
- The **Technical Working Project Explanatory Note** fixes implementation-level design decisions.
- **Appendix A** is the implementation file manifest used to prevent omissions during implementation.
- **Appendix B** is mandatory for the TWP documentation set and supersedes any earlier wording that could be read as allowing SQL to create the Platform Store application schema/tables, or allowing the runtime API identity to apply EF Core Migrations.

Where Appendix B refines an earlier Platform Store bootstrap description, **Appendix B takes precedence for that subject**.

## Language and public-repository policy

The public documentation set is maintained in **English**. Russian originals and superseded working documents are retained outside the public source tree as project archive material.

The public source tree must not contain secrets, production credentials, local `.env` files, or other private runtime configuration.

## Current document status

- Technical Requirements v0.1 — **Approved**.
- Preliminary Design Explanatory Note v0.1 — **Issued for approval**.
- Technical Working Project Explanatory Note v0.1 — **Draft for approval**.
- Appendix A — **Draft for approval together with the TWP Explanatory Note**.
- Appendix B — **Draft for approval together with the TWP Explanatory Note**.

## Demo environment startup

### Prerequisites

The PDG v0.1 demo environment requires:

- Docker;
- Docker Compose;
- sufficient local resources to run two PostgreSQL containers, the one-shot bootstrap container, and the PDG API container.

All commands below are executed from the repository root.

### Local environment configuration

The repository contains `.env.example` with the names of the required secret values:

- `PLATFORM_OWNER_PASSWORD`
- `PLATFORM_APP_PASSWORD`
- `CHINOOK_OWNER_PASSWORD`
- `PDG_READER_PASSWORD`
- `JWT_SIGNING_KEY`

Create a local `.env` file from `.env.example` and provide local values for all required variables.

The real `.env` file contains secrets and must not be committed to the source repository.

Do not place real passwords, signing keys, or connection strings in documentation, source files, Docker images, or committed configuration.

### Standard startup

Build and start the complete demo environment:

`docker compose up --build -d`

The Compose topology contains four services:

- `platform-store` — PostgreSQL Platform Store;
- `chinook-db` — PostgreSQL Chinook Corporate Data Source;
- `pdg-bootstrap` — one-shot bootstrap service;
- `pdg-api` — PDG HTTP API.

Startup sequencing is controlled by Docker Compose:

1. `platform-store` and `chinook-db` are started.
2. Both PostgreSQL services must become healthy.
3. `pdg-bootstrap` runs after both databases are healthy.
4. The Platform Store EF Core migration bundle is applied.
5. Platform demo seed, runtime role, grants, and security verification are applied.
6. The Chinook dataset and PDG corporate projection, runtime role, grants, and security verification are prepared.
7. `pdg-bootstrap` must complete successfully.
8. `pdg-api` starts only after successful bootstrap completion.

### Startup verification

Verify the resulting service state:

`docker compose ps -a`

The expected state is:

- `platform-store` — running and healthy;
- `chinook-db` — running and healthy;
- `pdg-bootstrap` — exited successfully with code `0`;
- `pdg-api` — running.

The API is exposed only on the local host:

`127.0.0.1:8080`

The readiness endpoint may be checked with:

`curl -f http://127.0.0.1:8080/health/ready`

A successful clean startup requires no manual database schema creation, manual seed insertion, manual role creation, manual GRANT/REVOKE changes, or direct database repair.

## Clean reproducibility startup

The clean-start procedure is used for reproducibility testing, including acceptance test T-26.

### Warning

The following reset command removes the local PDG Docker volumes and therefore deletes the current local demo database state.

Do not use it when the existing local Docker volume contents must be preserved.

Remove the current Compose environment together with its demo volumes:

`docker compose down -v --remove-orphans`

Then rebuild and start the complete environment from empty Docker volumes:

`docker compose up --build -d`

Verify the resulting state:

`docker compose ps -a`

The expected result is the same as for the standard startup:

- both PostgreSQL services are healthy;
- `pdg-bootstrap` has completed successfully with exit code `0`;
- `pdg-api` is running;
- the demo environment is ready without manual database intervention.

This clean-start sequence is the documented reproducibility procedure for acceptance test **T-26**.

## Normal shutdown

To stop the running environment while preserving database volumes:

`docker compose down`

A subsequent standard startup reuses the preserved volumes.

## Destructive demo reset

To stop the environment and delete the local demo database volumes:

`docker compose down -v --remove-orphans`

This operation is intentionally destructive and is used only when an empty demo environment is required.
