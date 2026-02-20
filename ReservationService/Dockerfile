FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto (hexagonal architecture)
COPY src/ReservationService.Worker/ReservationService.Worker.csproj ./ReservationService.Worker/
COPY src/ReservationService.Application/ReservationService.Application.csproj ./ReservationService.Application/
COPY src/ReservationService.Domain/ReservationService.Domain.csproj ./ReservationService.Domain/
COPY src/ReservationService.Infrastructure/ReservationService.Infrastructure.csproj ./ReservationService.Infrastructure/

# Restaurar dependencias
RUN dotnet restore ReservationService.Worker/ReservationService.Worker.csproj

# Copiar codigo fuente
COPY src/ReservationService.Worker/ ./ReservationService.Worker/
COPY src/ReservationService.Application/ ./ReservationService.Application/
COPY src/ReservationService.Domain/ ./ReservationService.Domain/
COPY src/ReservationService.Infrastructure/ ./ReservationService.Infrastructure/

WORKDIR /src/ReservationService.Worker
RUN dotnet publish -c Release -o /app/publish

# Imagen final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ReservationService.Worker.dll"]
