# Perimeter Data Gateway v0.1 - Performance Report

## Measurement status

Performance measurement performed according to TWP Section 18.

The TWP defines no SLA threshold. These measurements are observational and are not a pass/fail SLA acceptance gate.

## Environment

- Measurement time (UTC): 2026-08-30T13:17:32Z
- CPU: Intel(R) Core(TM) i7-7500U CPU @ 2.70GHz
- RAM: 15.89 GB
- Operating system: Майкрософт Windows 10 Домашняя для одного языка
- Docker Server: 29.2.1
- PostgreSQL: 18.6
- .NET SDK: 8.0.424

## Dataset

- Dataset: Chinook 1.4.5
- Source file: `db/chinook/10-chinook-1.4.5.sql`
- SHA256: `e3fde5c1a5b51a2a91429a702c9ca6e69ba56e6c7f5e112724d70c3d03db695e`
- `pdg.sales_summary` rows: 412

## Method

- Logical operation: read `SalesSummary` with GlobalAnalyst demo Subject `user_43`.
- Actor: `sales_copilot_v1`.
- Capability: `sales.read`.
- Result limit: 500 rows.
- Concurrency: 1.
- Warm-up iterations: 10.
- Measured iterations: 100.
- Baseline: direct fixed query to `pdg.sales_summary` under `pdg_reader`.
- Comparison: equivalent authenticated HTTP request through PDG.
- Direct database timing is collected in one persistent `psql` session using `\timing`.
- PDG timing is end-to-end client elapsed time including authentication, authorization, Platform Store access, Corporate Data Source read, mandatory audit persistence, serialization, and local HTTP transport.

## Results

### Direct database baseline

- Samples: 100
- Median: 2.049 ms
- p95: 3.086 ms

### PDG request

- Samples: 100
- Median: 33.155 ms
- p95: 60.507 ms

### Observed PDG overhead

- Median absolute overhead: 31.106 ms
- p95 absolute overhead: 57.421 ms
- Median relative overhead: 1518.49%

The observed overhead is the difference between the direct database baseline and the complete PDG request path. It must not be interpreted as database-only overhead.

## Conclusion

The required performance measurements were completed with the fixed demo dataset, at least 10 warm-up requests, at least 100 sequential measured requests, and concurrency equal to 1.

No SLA threshold is asserted by PDG v0.1 TWP.
