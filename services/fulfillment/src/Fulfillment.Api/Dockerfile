FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy only the Fulfillment src to keep context small and preserve project references
COPY services/fulfillment/src ./services/fulfillment/src
WORKDIR /src/services/fulfillment/src/Fulfillment.Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
# Update package repositories and install fonts/libraries for PDF generation
RUN apk update && apk upgrade && apk add --no-cache \
    fontconfig \
    freetype \
    libx11 \
    libxext \
    libxrender \
    ttf-liberation \
    tzdata \
    curl \
    && rm -rf /var/cache/apk/*

# Create a non-root user
RUN addgroup -g 1001 -S appgroup && adduser -S appuser -u 1001 -G appgroup

WORKDIR /app
COPY --from=build /app/publish .

# Change ownership of app directory to appuser
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004

# Add health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5004/health || exit 1

ENTRYPOINT ["dotnet", "Fulfillment.Api.dll"]
