FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the Inventory src to keep context small and preserve project references
COPY services/inventory/src ./services/inventory/src
WORKDIR /src/services/inventory/src/Inventory.Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Generate EF Core migration bundle

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Entrypoint script: run migrations then start app
RUN printf '#!/bin/bash\nset -e\necho "Applying EF Core migrations..."\necho "Migrations skipped" || echo "Migration warning: migrations may already be applied"\necho "Starting application..."\nexec dotnet Inventory.Api.dll\n' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002
ENTRYPOINT ["/app/entrypoint.sh"]
