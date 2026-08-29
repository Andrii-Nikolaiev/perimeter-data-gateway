FROM mcr.microsoft.com/dotnet/sdk:8.0.424-alpine3.23-amd64 AS build

WORKDIR /src

COPY . .

RUN dotnet restore Perimeter.Gateway.sln

RUN dotnet publish \
    src/Perimeter.Gateway.Api/Perimeter.Gateway.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /out \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0.30-alpine3.23-amd64

WORKDIR /app

COPY --from=build /out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Perimeter.Gateway.Api.dll"]
