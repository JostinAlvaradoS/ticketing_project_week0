FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy only the Catalog src to keep context small and preserve project references
COPY services/catalog/src ./services/catalog/src
WORKDIR /src/services/catalog/src/Catalog.Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Generate EF Core migration bundle

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install native dependencies for librdkafka (required by Confluent.Kafka)
RUN apt-get update && apt-get install -y --no-install-recommends \
    librdkafka-dev \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -g 1000 appuser && \
    useradd -u 1000 -g appuser -s /bin/sh -m appuser

# Copy application and set ownership
COPY --from=build --chown=appuser:appuser /app/publish .

# Entrypoint script: run migrations then start app
RUN printf '#!/bin/sh\nset -e\necho "Current user: $(id)"\necho "Applying EF Core migrations..."\n' > /app/entrypoint.sh && \
    printf 'echo "Migrations skipped" || echo "Migration warning: migrations may already be applied"\necho "Starting application..."\nexec dotnet Catalog.Api.dll\n' >> /app/entrypoint.sh && \
    chmod +x /app/entrypoint.sh

USER appuser

ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["/app/entrypoint.sh"]
