FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy only the Catalog src to keep context small and preserve project references
COPY services/catalog/src ./services/catalog/src
WORKDIR /src/services/catalog/src/Catalog.Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Generate EF Core migration bundle
RUN dotnet tool install --global dotnet-ef --version 8.0.*
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project ../Catalog.Infrastructure/Catalog.Infrastructure.csproj \
    --startup-project . \
    --context CatalogDbContext \
    -o /app/publish/efbundle \
    --self-contained \
    --force

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser

# Copy application and set ownership
COPY --from=build --chown=appuser:appuser /app/publish .
RUN chmod +x /app/efbundle

# Entrypoint script: run migrations then start app
RUN printf '#!/bin/sh\nset -e\necho "Applying EF Core migrations..."\n/app/efbundle --connection "$ConnectionStrings__Default" || echo "Migration warning: migrations may already be applied"\necho "Starting application..."\nexec dotnet Catalog.Api.dll\n' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

USER appuser

ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["/app/entrypoint.sh"]
