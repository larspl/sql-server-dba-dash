# Base image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY DBADash.Web.sln ./
COPY DBADash.Web.Server/*.csproj ./DBADash.Web.Server/
COPY DBADash.Web.Client/*.csproj ./DBADash.Web.Client/
COPY DBADash.Web.Shared/*.csproj ./DBADash.Web.Shared/
COPY DBADash.Web.API/*.csproj ./DBADash.Web.API/
COPY DBADash.Web.Core/*.csproj ./DBADash.Web.Core/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the server application
RUN dotnet publish DBADash.Web.Server/DBADash.Web.Server.csproj -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install SQL Server tools (optional, for troubleshooting)
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Copy published application
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "DBADash.Web.Server.dll"]
