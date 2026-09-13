# OpenDSC Pull Server Dockerfile
# Multi-stage build for optimized image size

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY src/OpenDsc.Contracts/OpenDsc.Contracts.csproj src/OpenDsc.Contracts/
COPY src/OpenDsc.Schema/OpenDsc.Schema.csproj src/OpenDsc.Schema/
COPY src/OpenDsc.Server/OpenDsc.Server.csproj src/OpenDsc.Server/

# Restore dependencies
RUN dotnet restore src/OpenDsc.Server/OpenDsc.Server.csproj

# Copy source code
COPY src/OpenDsc.Contracts/ src/OpenDsc.Contracts/
COPY src/OpenDsc.Schema/ src/OpenDsc.Schema/
COPY src/OpenDsc.Server/ src/OpenDsc.Server/

# Build and publish (the project multi-targets net10.0 and net10.0-windows;
# only net10.0 applies in the Linux container)
RUN dotnet publish src/OpenDsc.Server/OpenDsc.Server.csproj \
    -c Release \
    -f net10.0 \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is needed for the container health check
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .
COPY --chmod=755 src/OpenDsc.Server/entrypoint.sh /app/entrypoint.sh

# Create data directory for SQLite database and generated certificates,
# owned by the non-root 'app' user built into the .NET images.
# The entrypoint starts as root to fix up ownership of mounted volumes,
# then drops privileges to 'app' before starting the application.
RUN mkdir -p /app/data && chown app:app /app/data

# HTTP is used for health checks only; the application requires HTTPS
# (Secure auth cookies, mTLS node authentication). The entrypoint generates
# a self-signed certificate if none is configured.
ENV ASPNETCORE_URLS="https://+:8443;http://+:8080"
# Keep all server state (SQLite db, uploaded configurations, DataProtection
# keys, generated certificates) on the /app/data volume instead of the
# default /etc/opendsc/server path, which is not writable in the container
ENV Server__Data__DataDirectory=/app/data
ENV Database__Provider=SQLite
ENV Database__ConnectionString="Data Source=/app/data/opendsc-server.db"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose ports
EXPOSE 8080 8443

# Start the application
ENTRYPOINT ["/app/entrypoint.sh"]
