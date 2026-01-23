# OpenDSC Pull Server Dockerfile
# Multi-stage build for optimized image size

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY src/OpenDsc.Schema/OpenDsc.Schema.csproj src/OpenDsc.Schema/
COPY src/OpenDsc.Server/OpenDsc.Server.csproj src/OpenDsc.Server/

# Restore dependencies
RUN dotnet restore src/OpenDsc.Server/OpenDsc.Server.csproj

# Copy source code
COPY src/OpenDsc.Schema/ src/OpenDsc.Schema/
COPY src/OpenDsc.Server/ src/OpenDsc.Server/

# Build and publish
RUN dotnet publish src/OpenDsc.Server/OpenDsc.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' --home /app appuser && \
    chown -R appuser:appuser /app

# Copy published application
COPY --from=build /app/publish .

# Create data directory for SQLite database
RUN mkdir -p /app/data && chown appuser:appuser /app/data

# Switch to non-root user
USER appuser

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV Database__Provider=SQLite
ENV Database__ConnectionString="Data Source=/app/data/opendsc-server.db"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "OpenDsc.Server.dll"]
