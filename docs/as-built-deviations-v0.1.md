# Perimeter Data Gateway v0.1 - As-Built Deviations and Residual Risks

## 1. Purpose

This document records the actual as-built deviations and residual risks identified after implementation and verification of Perimeter Data Gateway (PDG) v0.1.

It supplements the approved Technical Working Project without changing its normative contracts.

## 2. Class A deviations

No Class A deviations from the approved Technical Working Project were identified.

The implemented system preserves the approved:

- API contract;
- Effective Access evaluation order;
- audit semantics;
- error categories;
- Platform Store logical schema;
- Published Resource contract;
- Country mapping;
- MaxRows behavior;
- identity and delegation semantics;
- PostgreSQL role / VIEW security boundary;
- Docker Compose topology;
- acceptance behavior T-01 through T-27.

Implementation-level corrections made during verification did not change these approved contracts or behaviors and therefore do not constitute Class A deviations.

## 3. Verification status

The as-built implementation has been verified through:

- successful solution builds;
- reproducible Docker Compose startup;
- successful security bootstrap;
- mandatory acceptance scenarios T-01 through T-27;
- real-Compose persistence, bootstrap-idempotence, reproducibility, and end-to-end smoke verification;
- performance measurement according to TWP Section 18.

Performance results are recorded separately in:

`docs/performance-report-v0.1.md`

## 4. Residual risks

### 4.1. No production high availability

PDG v0.1 does not implement production HA or automatic failover.

A failure of a required runtime component can therefore make the gateway temporarily unavailable until the component is restored.

This is an intentionally deferred capability, not an acceptance defect of v0.1.

### 4.2. Production identity provider integration is deferred

PDG v0.1 does not implement an external production Identity Provider or full OAuth Token Exchange.

The demo environment uses the defined JWT boundary and locally configured signing material.

Integration with a production identity infrastructure requires a separate future design and deployment decision.

### 4.3. Audit is not tamper-proof

The runtime application role is restricted according to the approved security model, but v0.1 does not provide cryptographically tamper-proof or externally immutable audit storage.

Administrative or owner-level access remains outside the runtime application security boundary.

A stronger evidentiary audit architecture is a future-stage concern.

### 4.4. No cumulative-abuse online detector

Authorization and row-scope enforcement are evaluated for each individual request.

PDG v0.1 does not correlate multiple otherwise valid requests to detect cumulative extraction or behavioral abuse over time.

Such detection requires a separate monitoring and policy subsystem.

### 4.5. No production SLA

The TWP defines no production latency, throughput, or availability SLA.

The performance measurements in `docs/performance-report-v0.1.md` are observational measurements from the acceptance environment and must not be interpreted as production guarantees.

### 4.6. Environment-specific performance results

The recorded performance figures depend on the measured hardware, operating system, Docker runtime, PostgreSQL version, .NET version, dataset, and local execution topology.

They establish a reproducible measurement baseline for v0.1 but are not universally portable performance characteristics.

### 4.7. Local secret management

Real credentials and the JWT signing key are supplied through the local `.env` file and are intentionally excluded from source control.

Protection, rotation, backup policy, and production secret distribution remain operational responsibilities outside the v0.1 source repository.

## 5. As-built conclusion for Step 19

No Class A deviation requiring a prior TWP revision was identified.

The residual risks listed above are either explicitly deferred by the approved TWP or are operational consequences of the deliberately limited v0.1 scope.

Step 19 - residual risks and actual deviations - is therefore documented.