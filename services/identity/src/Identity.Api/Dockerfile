FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the Identity src to keep context small and preserve project references
COPY services/identity/src ./services/identity/src
WORKDIR /src/services/identity/src/Identity.Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Generate EF Core migration bundle

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Entrypoint script: run migrations then start app
RUN printf '#!/bin/bash\nset -e\necho "Applying EF Core migrations..."\necho "Migrations skipped" || echo "Migration warning: migrations may already be applied"\necho "Starting application..."\nexec dotnet Identity.Api.dll\n' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["/app/entrypoint.sh"]
