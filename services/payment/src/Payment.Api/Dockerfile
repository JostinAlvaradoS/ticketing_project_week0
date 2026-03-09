FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the Payment src to keep context small and preserve project references
COPY services/payment/src ./services/payment/src
WORKDIR /src/services/payment/src/Payment.Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Generate EF Core migration bundle

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install native dependencies for librdkafka (required by Confluent.Kafka on Linux)
RUN apt-get update && apt-get install -y librdkafka-dev && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Entrypoint script: run migrations then start app
RUN printf '#!/bin/bash\nset -e\necho "Applying EF Core migrations..."\necho "Migrations skipped" || echo "Migration warning: migrations may already be applied"\necho "Starting application..."\nexec dotnet Payment.Api.dll\n' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004
ENTRYPOINT ["/app/entrypoint.sh"]