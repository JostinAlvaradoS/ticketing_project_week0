FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the Notification src to keep context small and preserve project references
COPY services/notification/src ./services/notification/src
WORKDIR /src/services/notification/src/Notification.Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Generate EF Core migration bundle
RUN dotnet tool install --global dotnet-ef --version 8.0.*
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project ../Notification.Infrastructure/Notification.Infrastructure.csproj \
    --startup-project . \
    --context NotificationDbContext \
    -o /app/publish/efbundle \
    --self-contained \
    --force

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
RUN chmod +x /app/efbundle

# Entrypoint script: run migrations then start app
RUN printf '#!/bin/bash\nset -e\necho "Applying EF Core migrations..."\n/app/efbundle --connection "$ConnectionStrings__Default" || echo "Migration warning: migrations may already be applied"\necho "Starting application..."\nexec dotnet Notification.Api.dll\n' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5005
EXPOSE 5005
ENTRYPOINT ["/app/entrypoint.sh"]
