# Perimeter Data Gateway v0.1 - Build Manifest

## 1. Purpose

This document records the actual SDK, NuGet package versions, Docker image tags and digests, locally built PDG image identifiers, and external dataset checksum used for the accepted PDG v0.1 build.

The values below were captured from the repository and Docker environment on 2026-08-30.

## 2. .NET SDK

Repository SDK pin from `global.json`:

- .NET SDK: `8.0.424`

Actual SDK reported by `dotnet --version`:

- .NET SDK: `8.0.424`

## 3. NuGet Package Versions

Central package version management is defined in `Directory.Packages.props`.

- `Microsoft.EntityFrameworkCore` — `8.0.30`
- `Microsoft.EntityFrameworkCore.Relational` — `8.0.30`
- `Microsoft.EntityFrameworkCore.Design` — `8.0.30`
- `Microsoft.AspNetCore.Authentication.JwtBearer` — `8.0.30`
- `Microsoft.AspNetCore.Mvc.Testing` — `8.0.30`
- `Npgsql.EntityFrameworkCore.PostgreSQL` — `8.0.11`
- `Npgsql` — `8.0.9`
- `xunit.v3` — `4.0.0`
- `Moq` — `4.20.72`
- `Testcontainers.PostgreSql` — `4.14.0`

## 4. PostgreSQL Image

Docker Compose source tag:

- `postgres:18.6-alpine`

Observed image ID:

- `sha256:d3e1620b530c944afa6e887d22eb899824da68e19c52024bf98f5220c88a65b2`

Observed repository digest:

- `postgres@sha256:d3e1620b530c944afa6e887d22eb899824da68e19c52024bf98f5220c88a65b2`

The same PostgreSQL image is used by:

- `platform-store`
- `chinook-db`

## 5. Bootstrap Base Images

### Build image

Source tag:

- `mcr.microsoft.com/dotnet/sdk:8.0-alpine3.23-amd64`

Observed registry digest:

- `sha256:71d5af24e77337f16431ce276b1b6e2248b02fe913268fc418904f1cade9cbd7`

### Runtime image

Source tag:

- `mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine3.23-amd64`

Observed registry digest:

- `sha256:7d0ba0e477ee12a043ca904428b1fb00eb9828b90796257bca05d19703391ebf`

## 6. API Base Images

### Build image

Source tag:

- `mcr.microsoft.com/dotnet/sdk:8.0.424-alpine3.23-amd64`

Observed registry digest:

- `sha256:71d5af24e77337f16431ce276b1b6e2248b02fe913268fc418904f1cade9cbd7`

At the time of this accepted build, this image resolved to the same digest as the Bootstrap SDK image.

### Runtime image

Source tag:

- `mcr.microsoft.com/dotnet/aspnet:8.0.30-alpine3.23-amd64`

Observed registry digest:

- `sha256:8345568f40980161c5d47ad5e97acbcc33fdb27b3de136ffdf37d5cc9351cb79`

## 7. Locally Built PDG Images

### PDG API

Local image:

- `perimeter-data-gateway-pdg-api:latest`

Immutable local image ID:

- `sha256:6fea70d9823508f70349e7b14ac6848a834cb2fafcf585ed84039ef667679901`

Observed local repository digest:

- `perimeter-data-gateway-pdg-api@sha256:6fea70d9823508f70349e7b14ac6848a834cb2fafcf585ed84039ef667679901`

### PDG Bootstrap

Local image:

- `perimeter-data-gateway-pdg-bootstrap:latest`

Immutable local image ID:

- `sha256:c830d8100b53f32e8422df1dad33a96c7e05d03649d4baceecb8e0952860b326`

Observed local repository digest:

- `perimeter-data-gateway-pdg-bootstrap@sha256:c830d8100b53f32e8422df1dad33a96c7e05d03649d4baceecb8e0952860b326`

The `latest` names above are Docker Compose local build tags. The accepted build is identified by the immutable image IDs recorded above.

## 8. Chinook Dataset

Dataset:

- Chinook `1.4.5`

Source file:

- `db/chinook/10-chinook-1.4.5.sql`

SHA-256:

- `e3fde5c1a5b51a2a91429a702c9ca6e69ba56e6c7f5e112724d70c3d03db695e`

The checksum is stored in:

- `db/chinook/10-chinook-1.4.5.sha256`

## 9. Accepted Build Identification

The accepted PDG v0.1 build is therefore characterized by:

- .NET SDK `8.0.424`
- the NuGet package versions listed in Section 3
- PostgreSQL `18.6-alpine`
- the .NET Docker base-image digests listed in Sections 5 and 6
- PDG API local image ID `sha256:6fea70d9823508f70349e7b14ac6848a834cb2fafcf585ed84039ef667679901`
- PDG Bootstrap local image ID `sha256:c830d8100b53f32e8422df1dad33a96c7e05d03649d4baceecb8e0952860b326`
- Chinook 1.4.5 dataset SHA-256 `e3fde5c1a5b51a2a91429a702c9ca6e69ba56e6c7f5e112724d70c3d03db695e`

This manifest records the actual accepted-build inputs and image identities. It does not contain secrets.