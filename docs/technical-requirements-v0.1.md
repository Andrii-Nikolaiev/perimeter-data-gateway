# Perimeter Data Gateway (PDG)

## Technical Requirements v0.1

**Stage:** Preliminary Design / Prototype / Proof of Feasibility  
**Date:** 27 August 2026  
**Status:** Approved  
**Author:** Andrii Nikolaiev  
**Responsible for preparation of the Technical Requirements:** ChatGPT  
**Approving person:** Andrii Nikolaiev  

> **Editorial note:** this is a light editorial revision of the approved Technical Requirements v0.1
> (27 Aug 2026). An unreadable phrase in §4.1.7 was corrected, the definition of
> Effective Access in §1.5 was aligned with the formula in §4.1.6, and a dated
> comparison with known analogues was added in §6.3 (as required by GOST 19.201-78,
> §2.5). The substantive requirements were not changed.

---

# 1. Introduction

## 1.1. Development name

**Perimeter Data Gateway (PDG)** is a self-hosted controlled gateway that provides AI agents and internal services with controlled read-only access to limited data sets in existing corporate databases, without issuing direct database credentials and without exposing arbitrary SQL access.

AI is the primary calling party in the v0.1 scenario; however, the PDG architecture is not AI-exclusive and also permits use by other internal services.

## 1.2. Problem statement

Existing corporate databases were generally not designed for direct access by AI agents.

Giving AI database credentials, unrestricted SQL, or the right to determine its own accessible data scope creates unacceptable risk.

AI may interpret a user's natural-language intent, but it must not be the source of an authorization decision.

PDG must form a deterministic boundary between an AI Actor and the Corporate Data Source.

## 1.3. Scope of application

PDG is intended for organizations that use on-premises or self-hosted databases and want to provide limited, controlled, and auditable access to AI agents or internal services.

The primary v0.1 scenario is **Sales AI** with a single Published Resource: **SalesSummary**.

Other scenarios - customer care, analytical services, operator applications, and other consumers - are possible future directions but are outside the v0.1 implementation scope.

## 1.4. Maturity stage

Version v0.1 is:

- a **Preliminary Design**;
- a **Prototype**;
- a **Proof of Feasibility**.

It is not a production-ready solution.

## 1.5. Terms and definitions

**Subject** - the person on whose behalf the request is made.

**Actor** - the software agent or internal service that actually performs the request.

**Published Resource** - a predefined and fixed data-access contract that hides the physical schema of the Corporate Data Source.

**Corporate Data Source** - an existing corporate source of business data.

**Platform Store** - a separate PDG store for configuration, policies, permissions, Actor limits, Subject-Actor delegation associations, and audit metadata.

**Policy** - a deterministic rule that allows or denies access.

**Capability** - an explicitly named Actor permission to use a particular function, for example `sales.read`.

**Actor policy limits** - limitations on an Actor's capabilities stored in the Platform Store independently of Subject permissions.

**Subject-Actor delegation** - an allowed association confirming that a specific Actor may act on behalf of a specific Subject.

**Effective Access** - the resulting access scope obtained as the intersection of five independent constraints: validated JWT scope, Actor policy limits, Subject-Actor delegation, Subject permissions, and Published Resource constraints (exact formula: see §4.1.6).

---

# 2. Basis for development

## 2.1. Basis

The basis for development is the internal decision of the author, **Andrii Nikolaiev**, dated **27 Aug 2026**, to create the Perimeter Data Gateway v0.1 prototype.

Fictitious order numbers, protocol numbers, or other organizational documents are not used.

## 2.2. Hypothesis to be verified

PDG v0.1 must demonstrate the technical feasibility of the following set of properties:

- no database credentials are issued to the Actor;
- no raw/arbitrary SQL is exposed to the Actor;
- Subject and Actor are independent identities;
- Subject-Actor delegation is explicitly verified;
- Actor capabilities are constrained independently;
- policy evaluation is deterministic;
- access is restricted at resource, row, and column level;
- the Corporate Data Source is read-only;
- DB-level least privilege is enforced;
- ALLOW and DENY decisions are subject to mandatory audit;
- major failure classes are distinguishable;
- self-hosted deployment is supported.

## 2.3. Method of verifying the hypothesis

The hypothesis is considered confirmed only if the following are reproducibly passed:

- positive ALLOW scenarios;
- negative DENY scenarios;
- infrastructure failure scenarios;
- DB-level security tests;
- bootstrap/restart tests;
- performance measurements;
- the mandatory automated acceptance suite.

---

# 3. Purpose of the development

## 3.1. Functional purpose

PDG must provide an Actor with access to the Corporate Data Source only through a predefined Published Resource.

The Actor must not receive:

- direct access to base tables;
- database credentials;
- a raw SQL interface;
- the right to choose table/view names;
- the right to specify arbitrary columns;
- the right to construct JOINs;
- the right to modify policy;
- the right to modify Published Resources;
- the right to modify audit records;
- write operations.

Overall flow:

**Subject -> Actor -> Authentication -> Delegation / Capability / Policy -> Published Resource -> Corporate Data Source -> Result -> Audit**

AI interprets intent.

PDG makes the authorization decision.

## 3.2. Self-hosted principle

PDG must support self-hosted deployment and must not require a mandatory external cloud service to perform its core function.

PDG itself must not send corporate data to external services.

At the same time, self-hosted deployment does not guarantee that data returned to the Actor can never leave the trusted perimeter; that depends on the deployment topology and downstream consumer behavior.

---

# 4. Program requirements

# 4.1. Functional requirements

## 4.1.1. Read-only mode

The Corporate Data Source in v0.1 is used for reading only.

The following are prohibited:

- `INSERT`;
- `UPDATE`;
- `DELETE`;
- DDL;
- arbitrary SQL;
- write privileges for the runtime DB role.

## 4.1.2. Corporate Data Source

v0.1 uses one Corporate Data Source:

- PostgreSQL;
- the Chinook demonstration database.

PDG runtime must not modify Chinook business tables or business data.

Technical objects around the existing data that are required for security and deployment may be created, for example a restricted VIEW, runtime role, and grants.

## 4.1.3. Platform Store

The Platform Store must be logically separated from the Corporate Data Source and use separate credentials.

It must store:

- Published Resources;
- Subject roles / permissions;
- Actor policy limits;
- Subject-Actor delegation associations;
- policies;
- audit metadata.

The Platform Store is not:

- a copy of the Corporate Data Source;
- a data warehouse;
- a cache of corporate responses.

PDG must not persist full copies of corporate responses in the Platform Store or in its own persistent storage.

## 4.1.4. Published Resource `SalesSummary`

v0.1 contains one Published Resource:

**SalesSummary**

Its required capability is:

`sales.read`

Fixed output fields:

- `CustomerId`;
- `Country`;
- `InvoiceDate`;
- `Total`.

Fields that must not be returned through SalesSummary:

- `Address`;
- `PostalCode`;
- `Phone`;
- `Fax`;
- `Email`.

The SalesSummary contract must be fixed and must not be controlled by the Actor.

## 4.1.5. JWT authentication and Actor identity

Signed JWT Bearer authentication is used.

PDG must validate:

- signature;
- `iss`;
- `aud`;
- `exp`.

Required claims:

- `sub` - Subject;
- `act.sub` - Actor;
- `scope` - Actor capabilities.

The Actor Claim semantics are modeled after RFC 8693; however, full OAuth Token Exchange is not implemented in v0.1.

`act.sub` is mandatory for a protected resource.

A missing or invalid `act.sub` must result in:

`401 / authentication_failed`

Subject-only fallback is prohibited.

An expired JWT must result in:

`401 / authentication_failed`.

A test issuer is permitted for demo/test only.

Trusted external token issuance is required for real corporate operation.

Secrets and signing keys must:

- not be hardcoded;
- not be stored as plaintext in the source repository;
- not be included as plaintext in a published image;
- be supplied through an external configuration/secret mechanism.

## 4.1.6. Authorization sequence

PDG must apply authorization in the following logical order:

1. JWT validation.
2. Extract and validate `sub`.
3. Extract and validate `act.sub`.
4. Verify Subject-Actor delegation.
5. Extract capability from JWT `scope`.
6. Check Actor policy limits.
7. Check Subject permissions.
8. Perform resource-level authorization.
9. Check the Published Resource required capability.
10. Check explicit requested scope against allowed row scope.
11. Apply server-side row filtering.
12. Apply fixed column projection.
13. Execute the predefined read operation.
14. Write the mandatory audit record.
15. Return the protected result only after all mandatory checks and successful audit persistence.

A DENY must occur before protected data is read whenever a decision can be made without reading the Corporate Data Source.

Effective Access is defined as:

**Validated JWT scope  
∩ Actor policy limits  
∩ permitted Subject-Actor delegation  
∩ Subject permissions  
∩ Published Resource constraints**

## 4.1.7. Row-scope semantics

If a request does not specify an explicit scope (for example, calls `SalesSummary` with no parameters), PDG must automatically apply server-side filtering and return only permitted rows. In the absence of an explicit out-of-scope request, there is no error; the result is simply narrower.

Example:

`SalesManagerEurope -> SalesSummary`

-> `200 OK`

-> permitted European rows only.

If the Actor explicitly requests a scope that is not a subset of the allowed scope, the request must be denied.

Example:

`SalesManagerEurope -> SalesSummary?country=USA`

-> `403 / access_denied`

PDG must not silently widen permissions.

## 4.1.8. Demo roles and Actor

v0.1 uses the following demo roles:

**SalesManagerEurope**
- has access to SalesSummary;
- row scope is limited to Europe.

**GlobalAnalyst**
- has access to all SalesSummary rows.

**SupportAgent**
- has no access to SalesSummary.

Demo Actor:

`sales_copilot_v1`

Required capability:

`sales.read`

Actor capability is an independent authorization axis and must not be inherited automatically from the Subject role alone.

## 4.1.9. Database Defense in Depth

The runtime DB role must:

- operate by allow-list;
- have `SELECT` only on the approved restricted VIEW;
- have no `SELECT` on protected base tables;
- have no write/DDL privileges;
- not own protected base tables;
- have no privileges that can bypass the selected boundary.

The restricted VIEW must exclude fields that must not be available through SalesSummary.

Security-related database objects must be created through a reproducible idempotent bootstrap.

### Accepted residual risk

Per-Subject row scope in v0.1 is enforced in the Application/Policy layer.

PostgreSQL RLS is not used for per-Subject filtering in v0.1.

A defect in Application row filtering could theoretically disclose rows that are within the allowed restricted VIEW.

Such a defect must not allow:

- access to excluded columns;
- direct reads of protected base tables;
- write operations.

## 4.1.10. Minimal API contract

Minimal protected API:

`GET /api/resources/SalesSummary`

`GET /api/resources/SalesSummary?country=<value>`

Authentication:

`Authorization: Bearer <JWT>`

Unknown query parameters must result in:

`400 / invalid_request`

The Actor must not control:

- SQL;
- table name;
- view name;
- column names;
- JOIN;
- SQL WHERE fragment;
- ORDER BY expression;
- any SQL fragment.

Untrusted input must not be concatenated into an executable database query.

Values must be passed using parameter/data binding or an equivalent safe mechanism.

## 4.1.11. Server-side result limit

SalesSummary must enforce a hard server-side row limit.

The Actor must not be able to:

- disable the limit;
- change it;
- increase it.

The exact numeric value must be fixed in the approved demo configuration before acceptance testing.

If the result exceeds the configured limit:

`400 / result_limit_exceeded`

Silent truncation is prohibited.

## 4.1.12. Audit

Every completed ALLOW/DENY decision must be recorded.

Mandatory audit fields:

- `Timestamp`;
- `Subject`;
- `Actor`;
- `Capability`;
- `Resource`;
- `Scope`;
- `Decision`;
- `ReasonCategory`;
- normalized policy-relevant request parameters;
- `RowsReturned`.

For ALLOW:

`RowsReturned = actual number of rows returned`.

For DENY:

`RowsReturned = 0`.

The audit must not store:

- raw natural-language prompt;
- Bearer token;
- credentials;
- secrets;
- connection strings;
- full response body;
- unnecessary sensitive data.

Audit must be append-only at the application-semantics level.

Public audit update/delete operations are not permitted.

Audit in v0.1 is not claimed to be cryptographically immutable or tamper-proof.

The presence of `RowsReturned` provides post-factum observability but does not imply real-time cumulative-abuse detection.

## 4.1.13. Audit failure semantics

For an authenticated request in which an ALLOW or DENY authorization decision has been made, audit persistence is mandatory.

If the audit record cannot be persisted:

- protected data must not be returned;
- the response must be:

`503 / audit_write_failed`

If the initial decision was DENY but audit persistence also fails, the final response is:

`503 / audit_write_failed`.

## 4.1.14. Error contract

Minimum error contract:

| HTTP | Code | Meaning |
|---|---|---|
| 200 | `success` | Successful request |
| 400 | `invalid_request` | Invalid parameter or request shape |
| 400 | `result_limit_exceeded` | Server-side row limit exceeded |
| 401 | `authentication_failed` | JWT/Subject/Actor authentication failure |
| 403 | `access_denied` | Authorization DENY |
| 404 | `resource_not_found` | Published Resource does not exist |
| 500 | `internal_error` | Malformed/invalid mandatory policy/configuration |
| 503 | `corporate_data_source_unavailable` | Corporate Data Source unavailable |
| 503 | `platform_store_unavailable` | Platform Store unavailable |
| 503 | `audit_write_failed` | Audit persistence failure |

The response must not expose:

- stack trace;
- SQL;
- database hostname;
- IP address;
- database name;
- username;
- connection string;
- secret data.

Existing resource with no applicable allowing policy:

`403 / access_denied`

Malformed mandatory policy/configuration:

`500 / internal_error`

with fail-closed behavior.

## 4.1.15. Security invariants

The public API must not provide the Actor with operations or parameters that can modify:

- policies;
- Subject permissions;
- Actor limits;
- delegation associations;
- Published Resources;
- audit;
- security configuration.

A security decision must not depend on unsigned/self-asserted Actor claims outside validated authorization input.

Prompt injection must not be able to expand Effective Access.

### Residual cumulative-abuse risk

Multiple separately permitted requests may form an undesirable cumulative-access pattern.

v0.1 does not detect cumulative abuse online.

Risk is bounded by:

- Effective Access;
- resource restrictions;
- row restrictions;
- column restrictions;
- result limit.

Post-factum visibility is provided by audit metadata, including normalized request parameters and `RowsReturned`.

## 4.1.16. No administrative API

v0.1 does not provide a public administrative API for:

- policies;
- resources;
- Subject permissions;
- Actor limits;
- delegation associations;
- security configuration.

Initial configuration is performed by the bootstrap mechanism.

## 4.1.17. Bootstrap

Bootstrap must deterministically create/prepare:

- Platform Store schema;
- demo Subjects;
- demo Actor;
- Subject-Actor delegations;
- Published Resource SalesSummary;
- Actor policy limits;
- Subject policies;
- Chinook demo data;
- runtime DB role;
- restricted VIEW;
- required grants.

Bootstrap must be idempotent.

If a previous attempt ended partially, a repeated run must bring the system to the desired state:

- without duplicates;
- without privilege escalation;
- without destroying valid state;
- without mandatory manual cleanup.

The exact transaction/rollback mechanism is not defined by these Technical Requirements.

## 4.1.18. Performance measurement

No production SLA is defined for v0.1.

Measurements must be reproducible:

- record the reference environment;
- record software versions;
- record dataset size;
- use the same logical query;
- at least 10 warm-up requests;
- at least 100 measured sequential requests;
- concurrency = 1;
- baseline - direct request to the same restricted VIEW under the same runtime DB role;
- comparison - equivalent request through PDG.

The following must be recorded:

- median latency;
- p95 latency;
- observed PDG overhead.

Performance results for v0.1 are measured characteristics, not a pass/fail SLA.

---

# 4.2. Reliability requirements

PDG must operate according to the principle:

**fail closed**

If Effective Access cannot be determined unambiguously, data must not be returned.

Fail-closed cases include:

- Platform Store unavailable;
- mandatory policy missing;
- invalid Subject-Actor delegation;
- missing required Actor capability;
- malformed policy/configuration;
- audit write failure.

Corporate Data Source unavailability must not be converted into a fake `200`.

After restart, the following must persist:

- Platform Store data;
- audit;
- security/bootstrap state;
- Corporate Data Source data.

Invalid input must be rejected before accessing the Corporate Data Source whenever the decision can be made without reading protected data.

---

# 4.3. Operating conditions

PDG v0.1 is intended for:

- local development;
- self-hosted deployment;
- demo/test environments;
- a Docker-based reproducible environment.

A Bearer token may be transmitted over HTTP only:

- on localhost;
- or within an isolated Docker network on the same machine.

The mere use of Docker does not make remote cleartext traffic safe.

TLS is required when crossing a machine/network trust boundary.

v0.1 is not:

- a High Availability solution;
- a production security platform;
- a production SLA solution.

---

# 4.4. Hardware composition and parameter requirements

The environment must be capable of running:

- PDG;
- Platform Store;
- Corporate Data Source;
- Docker;
- automated tests.

These Technical Requirements do not impose arbitrary minimums for:

- CPU;
- RAM;
- disk.

The actual reference environment must be recorded during performance testing.

---

# 4.5. Information and software compatibility requirements

Primary v0.1 stack:

- C#;
- .NET 8;
- ASP.NET Core;
- EF Core;
- PostgreSQL;
- Npgsql;
- Docker / Docker Compose;
- JWT Bearer;
- xUnit;
- Moq.

PostgreSQL is the first provider.

This does not mean support for "any database."

Application logic should not depend on PostgreSQL-specific APIs unless necessary.

Provider-specific behavior must be localized in the infrastructure adapter.

The EF Core provider model reduces coupling but does not guarantee automatic portability between DBMSs.

Exact package/image versions must be pinned in manifests.

Use of `latest` for the accepted demo environment is prohibited.

---

# 4.6. Marking and packaging requirements

PDG is a software product.

Version/build must be unambiguously identifiable.

When third-party software/data are used, required license and attribution notices must be preserved.

---

# 4.7. Transportation and storage requirements

Source code and documentation are stored in version control.

The published source repository must not contain:

- secrets;
- production credentials;
- local secret configuration files.

---

# 4.8. Special requirements

PDG v0.1 must adhere to the following principles:

- secure by default;
- least privilege;
- read-only Corporate Data Source;
- deterministic authorization;
- independent Subject and Actor identities;
- explicit delegation validation;
- defense in depth;
- fail closed;
- explicit trust boundaries;
- mandatory auditability;
- reproducibility;
- AI is not the security authority;
- minimum scope sufficient to prove feasibility.

---

# 5. Software documentation requirements

For v0.1, documentation must be sufficient to understand:

- purpose;
- trust boundaries;
- authorization model;
- Published Resource;
- deployment;
- demo scenario;
- tests;
- residual risks;
- measured results.

Minimum set:

1. these Technical Requirements;
2. architectural / technical rationale for the Preliminary Design stage decisions;
3. technical implementation description;
4. startup instructions;
5. demo scenario description;
6. acceptance test description and results;
7. third-party license / attribution notices.

---

# 6. Technical and economic indicators

## 6.1. Objective of stage v0.1

The primary objective is to reduce technical uncertainty and confirm:

- feasibility of the trust boundary;
- Subject + Actor model;
- delegation;
- Actor capability;
- DB defense in depth;
- reproducibility;
- residual risks.

## 6.2. Expected technical and operational benefits

Compared with giving AI direct database credentials, the expected benefits are:

- no DB credentials for the Actor;
- no raw SQL;
- centralized deterministic authorization boundary;
- independent Subject and Actor identities;
- resource/row/column restrictions;
- explicit delegation;
- mandatory audit;
- DB least privilege.

The self-hosted/vendor-neutral approach is treated as a testable engineering hypothesis for organizations that do not want an external cloud/vendor platform to be mandatory.

This is not a claim that PDG is unconditionally superior to particular commercial products.

## 6.3. Comparison with known analogues (as of 27 Aug 2026)

The category of "controlled AI-agent access to corporate data" is not empty. Snowflake released Cortex AI Gateway (following its acquisition of Natoma); Microsoft Agent 365 uses a `sub`=user / `act`=agent model similar in spirit to ours; TrueFoundry publicly documents OBO based on RFC 8693 with intersection of Subject and Actor permissions, including multi-agent delegation chains; Midplane is a self-hosted MIT product in front of PostgreSQL with parser-level policy and audit; DBShifts is a managed SaaS gateway with table/column allow-lists and PII masking.

None of the reviewed sources combines all of the following at once: delegated Subject-Actor authorization with permission intersection, a closed Published Resource data plane with no SQL surface for the Actor, row/column enforcement over an existing corporate database, an independent DB-level least-privilege boundary, and fail-closed mandatory audit as a condition for releasing the result. The Subject/Actor/delegation model by itself is not a differentiator for PDG; it has already been published by others. The detailed analysis with sources and review dates is in `preliminary-design-explanatory-note-v0.1.docx`, section "Review of Analogues."

This is a prior-art analysis of published products as of the stated date; it is not a patent search and does not make any claim of patentability.

## 6.4. Limitation of economic claims

The v0.1 Technical Requirements do not include unverified claims regarding:

- market size;
- revenue forecast;
- valuation;
- funding requirements;
- ROI;
- future project cost.

Such indicators require a separate study.

---

# 7. Development stages and phases

## 7.1. Current stage

Current stage:

**Preliminary Design / Prototype / Proof of Feasibility v0.1**

The objective is to demonstrate the technical feasibility of the architectural hypothesis.

## 7.2. Scope of v0.1 work

Stage v0.1 includes:

1. approval of the Technical Requirements;
2. architectural and technical rationale;
3. vertical-slice implementation;
4. reproducible demo environment;
5. automated tests;
6. acceptance testing;
7. performance measurement;
8. recording residual risks;
9. an engineering conclusion on whether the hypothesis is confirmed or not confirmed.

## 7.3. Next stage

The next stage and its scope are not defined by these Technical Requirements.

A separate decision is made after v0.1 is completed and evaluated.

---

# 8. Control and acceptance procedure

## 8.1. General principle

The existence of source code alone does not mean v0.1 is complete.

The prototype must be verified by scenarios.

Checks that can reasonably be automated must be automated.

## 8.2. Mandatory acceptance suite

### T-01 - Valid ALLOW

`SalesManagerEurope + permitted sales_copilot_v1 + sales.read`

Expected:

- `200`;
- permitted rows only;
- permitted columns only;
- ALLOW audit;
- correct `RowsReturned`.

### T-02 - Resource-level DENY

`SupportAgent -> SalesSummary`

Expected:

- `403 / access_denied`;
- protected source is not read;
- DENY audit;
- `RowsReturned = 0`.

### T-03 - Automatic row filtering

`SalesManagerEurope -> SalesSummary`

Expected:

- `200`;
- European rows only.

### T-04 - Explicit out-of-scope request

`SalesManagerEurope -> SalesSummary?country=USA`

Expected:

- `403 / access_denied`;
- DENY audit.

### T-05 - GlobalAnalyst access

Expected:

- `200`;
- all SalesSummary rows;
- fixed output fields only.

### T-06 - Column restriction

The SalesSummary result does not contain:

- Email;
- Phone;
- Address;
- PostalCode;
- Fax.

### T-07 - Base-table SELECT denied

Direct `SELECT` against a protected base table under the same runtime DB security identity used by the test PDG instance.

Expected:

- PostgreSQL permission denied.

### T-08 - Write denied

Write attempt under the runtime DB role.

Expected:

- permission denied.

### T-09 - Missing/invalid JWT

Expected:

- `401 / authentication_failed`.

### T-10 - Expired JWT

Expected:

- `401 / authentication_failed`.

### T-11 - Missing/invalid `act.sub`

Expected:

- `401 / authentication_failed`;
- no Subject-only fallback.

### T-12 - Invalid Subject-Actor delegation

Expected:

- `403 / access_denied`;
- Corporate Data Source is not read;
- DENY audit.

### T-13 - Actor capability absent

The JWT does not contain `sales.read`.

Expected:

- `403 / access_denied`.

### T-14 - Actor policy limit disallows capability

The JWT contains `sales.read`, but Actor policy limits do not permit the capability.

Expected:

- `403 / access_denied`.

### T-15 - Unknown Published Resource

Expected:

- `404 / resource_not_found`.

### T-16 - Invalid/unknown parameter

Expected:

- `400 / invalid_request`.

### T-17 - Result limit exceeded

The configured row limit is exceeded.

Expected:

- `400 / result_limit_exceeded`;
- no silent truncation.

### T-18 - Corporate Data Source unavailable

Expected:

- `503 / corporate_data_source_unavailable`.

### T-19 - Platform Store unavailable

Expected:

- `503 / platform_store_unavailable`;
- fail closed;
- Corporate Data Source is not read.

### T-20 - Audit write failure

Expected:

- `503 / audit_write_failed`;
- protected result is not returned externally.

### T-21 - Malformed mandatory policy/config

Expected:

- `500 / internal_error`;
- fail closed;
- response contains no sensitive internal details.

### T-22 - SQL injection / arbitrary SQL attempt

An untrusted SQL-like string must not:

- execute;
- modify query structure;
- expand access;
- access arbitrary DB objects.

### T-23 - Audit content

Verify presence of mandatory fields:

- Timestamp;
- Subject;
- Actor;
- Capability;
- Resource;
- Scope;
- Decision;
- ReasonCategory;
- normalized policy-relevant parameters;
- RowsReturned.

For DENY:

`RowsReturned = 0`

Verify absence of:

- raw prompt;
- JWT;
- credentials;
- connection strings;
- secrets;
- full response body.

### T-24 - Restart / persistence

Required Platform Store and Corporate Data Source data/state must persist after restart.

### T-25 - Bootstrap idempotence

Repeated bootstrap after complete or partial prior initialization:

- creates no duplicates;
- causes no privilege escalation;
- reaches the desired state.

### T-26 - Reproducibility

The documented startup procedure must reproducibly bring up the demo environment.

### T-27 - End-to-end smoke

The full chain:

startup  
-> authentication  
-> ALLOW  
-> DENY  
-> audit

must be reproducible successfully.

## 8.3. Types of tests

Mandatory:

- unit tests for policy/application logic;
- integration tests for API/auth/database security/failure scenarios;
- mandatory acceptance suite T-01 - T-27.

Criterion:

**100% of mandatory acceptance tests must pass.**

This is not a requirement for 100% code coverage.

No code-coverage threshold is defined by these Technical Requirements.

## 8.4. Performance measurements

Measurements are performed according to §4.1.18.

The report records:

- reference environment;
- versions;
- dataset;
- median;
- p95;
- observed overhead.

This is a measurement, not an SLA acceptance gate.

## 8.5. Completion criterion for v0.1

The work is considered complete when:

- the main security controls are implemented;
- reproducibility is achieved;
- mandatory acceptance tests pass;
- performance measurements are performed;
- residual risks are recorded;
- an engineering conclusion is prepared:

**hypothesis confirmed / not confirmed.**

---

# 9. Boundaries and known limitations

PDG v0.1 is not:

- an LLM;
- a chatbot;
- a CRM;
- an ETL system;
- a BI system;
- an Identity Provider;
- a universal database proxy;
- an arbitrary SQL endpoint;
- a SQL-over-HTTP service;
- a write gateway;
- a production-ready platform;
- a High Availability solution;
- a full OAuth Token Exchange implementation;
- a tamper-proof audit system;
- a persistent corporate-data replica;
- a real-time cumulative-abuse detection system;
- a complete prompt-injection solution;
- a DB-level per-Subject RLS implementation;
- an administrative policy-management API.

v0.1 has:

- one Corporate Data Source;
- one Published Resource - SalesSummary;
- a test identity issuer used only for demo/test.

## 9.1. Residual risks

### 1. Application row-filtering defect

A row-filtering defect may disclose rows within the restricted VIEW.

It must not disclose:

- excluded columns;
- base tables;
- write privileges.

### 2. Cumulative authorized query abuse

A set of individually allowed requests may collectively reveal more information than expected.

v0.1 provides post-factum visibility but does not provide real-time cumulative-abuse detection.

### 3. Audit is not tamper-proof

Append-only semantics are enforced at the application-design level, but audit is not cryptographically immutable.

### 4. Demo identity infrastructure

The test issuer does not represent a production identity architecture.

---

# 10. Approval

**Development:** Perimeter Data Gateway  
**Version:** v0.1  
**Stage:** Preliminary Design / Prototype / Proof of Feasibility  
**Author:** Andrii Nikolaiev  
**Responsible for preparation of the Technical Requirements:** ChatGPT  
**Date:** 27 August 2026  
**Status:** Approved  
**Approving person:** Andrii Nikolaiev  
**Decision:** Approved  
**Approval date:** 27 August 2026
