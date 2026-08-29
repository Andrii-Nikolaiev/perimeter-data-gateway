FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.23-amd64 AS build

WORKDIR /src

COPY . .

RUN dotnet tool restore
RUN dotnet restore Perimeter.Gateway.sln

RUN dotnet ef migrations bundle \
    --project src/Perimeter.Gateway.Infrastructure \
    --startup-project src/Perimeter.Gateway.Api \
    --context PlatformStoreDbContext \
    --configuration Release \
    --self-contained \
    --target-runtime linux-musl-x64 \
    --output /out/pdg-platform-migrate

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine3.23-amd64

RUN apk add --no-cache postgresql-client

COPY --from=build /out/pdg-platform-migrate /bootstrap/pdg-platform-migrate
COPY db /bootstrap/db
COPY scripts /bootstrap/scripts

ENTRYPOINT ["/bin/sh", "/bootstrap/scripts/bootstrap.sh"]
