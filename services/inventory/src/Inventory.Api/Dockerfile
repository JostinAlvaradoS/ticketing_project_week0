FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy only the Inventory src to keep context small and preserve project references
COPY services/inventory/src ./services/inventory/src
WORKDIR /src/services/inventory/src/Inventory.Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
# Update package repositories and install security updates
RUN apk update && apk upgrade && apk add --no-cache tzdata curl && rm -rf /var/cache/apk/*

# Create a non-root user
RUN addgroup -g 1001 -S appgroup && adduser -S appuser -u 1001 -G appgroup

WORKDIR /app
COPY --from=build /app/publish .

# Change ownership of app directory to appuser
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002

# Add health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5002/health || exit 1

ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
