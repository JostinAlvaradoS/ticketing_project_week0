FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the Ordering src to keep context small and preserve project references
COPY services/ordering/src ./services/ordering/src
WORKDIR /src/services/ordering/src/Ordering.Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Generate EF Core migration bundle
RUN dotnet tool install --global dotnet-ef --version 8.0.*
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project ../Ordering.Infrastructure/Ordering.Infrastructure.csproj \
    --startup-project . \
    --context OrderingDbContext \
    -o /app/publish/efbundle \
    --self-contained \
    --force

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN chmod +x /app/efbundle

# Entrypoint script: run migrations then start app
RUN printf '#!/bin/bash\nset -e\necho "Applying EF Core migrations..."\n/app/efbundle --connection "$ConnectionStrings__DefaultConnection" || echo "Migration warning: migrations may already be applied"\necho "Starting application..."\nexec dotnet Ordering.Api.dll\n' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5003
EXPOSE 5003
ENTRYPOINT ["/app/entrypoint.sh"]
