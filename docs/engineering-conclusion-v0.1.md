# Perimeter Data Gateway v0.1 — Engineering Conclusion

## 1. Purpose

This document records the engineering conclusion for Perimeter Data Gateway (PDG) v0.1 after implementation, integration, containerized runtime verification, acceptance testing, restart/recovery testing, and performance measurement.

The conclusion is limited to the implemented v0.1 scope and the verified environment. It does not claim production high availability, external certification, or a production SLA.

---

## 2. Implementation Result

PDG v0.1 has been implemented as a controlled gateway between an authenticated caller and a corporate PostgreSQL data source.

The implemented architecture preserves the required separation between:

- the Platform Store, which contains subjects, actors, delegations, resource contracts, permissions, row scopes, and audit records;
- the Corporate Data Source, which exposes the permitted corporate dataset through the dedicated `pdg.sales_summary` view;
- the API runtime identity, which uses restricted database roles rather than owner credentials;
- the bootstrap identity, which performs schema migration, seed, role creation, grants, and security verification before the API becomes ready.

The API does not provide arbitrary SQL access. A caller requests a named resource through the defined HTTP contract, and PDG evaluates the request against the configured identity, delegation, capability, resource, permission, row-scope, parameter, and result-limit rules.

---

## 3. Effective Access Control

The implemented Effective Access flow verifies the required security conditions before corporate data is returned.

The verified behavior includes:

- authenticated subject identity;
- delegated actor identity;
- active subject-to-actor delegation;
- actor capability;
- subject role;
- resource existence and resource contract;
- subject-level permission for the resource;
- row-scope restrictions;
- query parameter validation;
- output-field contract;
- maximum result-row limit;
- restricted corporate database role;
- audit recording of the resulting decision.

The implementation preserves the required deny-by-default behavior for unsupported, invalid, unauthorized, or unavailable conditions.

---

## 4. Database Security Boundary

The Platform Store and Corporate Data Source use separate PostgreSQL databases and separate runtime roles.

The API runtime does not use database owner identities.

For the corporate database, `pdg_reader` receives only the permissions required for the approved PDG view and is not granted direct access to the underlying corporate base tables.

For the Platform Store, `pdg_platform_app` receives the minimum permissions required by the API runtime.

Bootstrap operations are performed separately from API runtime operation.

This separation was verified by integration and acceptance tests, including direct-access denial scenarios.

---

## 5. Bootstrap and Reproducibility

The v0.1 bootstrap sequence is implemented as a one-shot process.

The Platform Store sequence is:

1. PostgreSQL becomes healthy.
2. EF Core migration bundle applies the Platform Store schema.
3. The deterministic demo seed is applied.
4. The runtime role is created or updated.
5. Minimum grants are applied.
6. Security verification is executed.
7. Only after successful bootstrap may the API become ready.

The corporate database bootstrap performs:

1. PostgreSQL readiness verification.
2. SHA-256 verification of the pinned Chinook SQL artifact.
3. Chinook import when required.
4. Creation or update of the PDG schema and approved view.
5. Creation or update of `pdg_reader`.
6. Minimum grants.
7. Security verification.

Bootstrap operations are designed to be repeatable and were tested for idempotence and recovery from a partially completed Platform Store bootstrap.

A clean Docker Compose reset and rebuild was also verified.

---

## 6. Docker Compose Runtime Topology

The validated v0.1 topology contains:

- `platform-store`;
- `chinook-db`;
- `pdg-bootstrap`;
- `pdg-api`.

The API depends on successful completion of the bootstrap service.

Database containers use persistent Docker volumes in the normal restart scenario.

The bootstrap image contains the migration bundle, database scripts, and bootstrap scripts required to prepare both databases.

The accepted image and package versions are recorded separately in:

`docs/build-manifest-v0.1.md`

---

## 7. Integration Verification

Integration tests verify the real PostgreSQL security and bootstrap behavior rather than replacing the databases with in-memory substitutes.

The verified integration scope includes the database roles, grants, view boundary, bootstrap scripts, migration state, and required access restrictions.

The final solution and acceptance build completed with:

- warnings: 0;
- errors: 0.

---

## 8. Acceptance Verification

The complete PDG v0.1 acceptance suite was executed as one shared test run after correcting test-environment lifecycle isolation around Testcontainers database restarts.

The final result was:

- Total: 28
- Errors: 0
- Failed: 0
- Skipped: 0
- Not Run: 0

The full acceptance wrapper completed successfully.

The acceptance suite verifies the required T-01 through T-27 behavior, including positive access, denial paths, authentication failures, delegation and capability checks, row filtering, resource restrictions, invalid parameters, result limits, audit behavior, database unavailability, restart persistence, bootstrap recovery, clean reproducibility, and end-to-end runtime behavior.

Some negative acceptance scenarios intentionally produce runtime `warn:` or `fail:` log entries because the test deliberately makes a dependency unavailable or submits a rejected request. Such expected failure-path logging is not an acceptance-test failure. The acceptance criterion is the expected API behavior together with zero xUnit errors and zero failed tests.

---

## 9. Restart and Test-Lifecycle Verification

The acceptance environment includes destructive tests that stop and restart PostgreSQL Testcontainers.

A restart can result in a different mapped host port. Therefore, retaining the original long-lived test web host after a container restart could leave the test API configured with a stale connection string even though the database container itself was healthy.

The acceptance-test infrastructure was corrected so that, after the affected database containers are restored:

- Npgsql connection pools are cleared;
- the acceptance `WebApplicationFactory` is recreated;
- current Testcontainers connection strings are captured again;
- subsequent tests use the current mapped endpoints.

This correction is limited to acceptance-test lifecycle management and does not alter the production API access-control contract.

After this correction, the complete shared acceptance run passes all 28 tests.

---

## 10. Restart Persistence and Recovery

Restart without deleting the database containers was verified to preserve the required Platform Store and corporate demo state.

The verification covers the required Platform Store entities and the required corporate Chinook/PDG state.

The bootstrap process was also verified for:

- repeated execution;
- idempotent desired-state restoration;
- recovery after a partial Platform Store bootstrap;
- clean recreation after Docker volume removal.

These tests establish reproducibility for the demonstrated v0.1 environment.

---

## 11. End-to-End Verification

The real Docker Compose environment was verified end to end.

The checked path includes:

- service startup;
- database readiness;
- successful bootstrap;
- API readiness;
- JWT authentication;
- an allowed resource request;
- a denied resource request;
- audit creation.

This demonstrates that the principal v0.1 flow works through the assembled containerized runtime rather than only through isolated application-layer tests.

---

## 12. Performance Observation

A controlled local performance measurement was performed using the same corporate PostgreSQL data source and the same restricted `pdg_reader` database identity for both the direct baseline and the PDG path.

Test conditions:

- 100 sequential measured requests;
- 10 warm-up requests;
- concurrency: 1.

Observed values:

- direct database baseline median: approximately 2.049 ms;
- direct database baseline p95: approximately 3.086 ms;
- PDG median: approximately 33.155 ms;
- PDG p95: approximately 60.507 ms;
- measured median gateway overhead: approximately 31.106 ms;
- measured p95 gateway overhead: approximately 57.421 ms.

These values describe the tested local environment only.

No production latency SLA is claimed from this measurement.

The detailed measurement is recorded in:

`docs/performance-report-v0.1.md`

---

## 13. Class A Conformance

No known Class A deviation from the v0.1 TWP remains at engineering conclusion.

The preserved Class A areas include:

- HTTP API contract;
- Effective Access decision order;
- authentication and delegation semantics;
- actor capability semantics;
- resource contract;
- parameter validation;
- Country and row-scope behavior;
- `MaxRows` enforcement;
- output-field restrictions;
- audit semantics;
- error categories;
- database schema;
- database runtime-role boundaries;
- approved corporate VIEW boundary;
- Docker Compose topology;
- accepted build/runtime versions;
- required T-01 through T-27 behavior.

Known implementation observations and residual limitations are recorded separately in:

`docs/as-built-deviations-v0.1.md`

---

## 14. Residual Risks and Boundaries

The successful v0.1 verification does not remove the following production concerns:

- production high availability has not been implemented or demonstrated;
- the demonstrated deployment uses a single instance of each principal service;
- external production Identity Provider integration and production token exchange remain outside the demonstrated v0.1 environment;
- the audit store is operationally useful but is not presented as cryptographically tamper-proof;
- cumulative abuse/anomaly detection across many individually valid requests is not implemented as a dedicated subsystem;
- production backup, restore, disaster recovery, and operational monitoring require environment-specific procedures;
- production secrets must be supplied through an appropriate secret-management mechanism;
- performance measurements are environment-specific and do not constitute an SLA;
- production capacity limits require separate load and endurance testing.

These items do not invalidate the verified v0.1 functional and security behavior. They define the boundary between the completed engineering demonstration and a future production operating model.

---

## 15. Engineering Conclusion

Perimeter Data Gateway v0.1 has reached a coherent engineering baseline.

The implementation demonstrates that access to corporate data can be mediated through a deterministic gateway in which identity, delegation, capability, resource permission, row scope, request parameters, output contract, result limits, restricted database privileges, and audit are evaluated as one controlled access path.

The required database security boundary is present.

The bootstrap process is repeatable.

The Docker Compose topology is reproducible.

Restart and recovery behavior has been exercised.

The real containerized end-to-end path has been verified.

The final complete acceptance suite passes with:

**28 total tests, 0 errors, 0 failures.**

Accordingly, PDG v0.1 may be considered **engineering-complete for the scope defined by the v0.1 TWP and the verified demonstration environment**.

This conclusion is not a claim of production high availability, external certification, or unrestricted production readiness. Those concerns belong to the next engineering stage and require their own deployment, security, operational, capacity, and reliability criteria.