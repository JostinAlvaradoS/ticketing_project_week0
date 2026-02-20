# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivos de proyecto (hexagonal architecture)
COPY ["src/PaymentService.Worker/PaymentService.Worker.csproj", "PaymentService.Worker/"]
COPY ["src/PaymentService.Application/PaymentService.Application.csproj", "PaymentService.Application/"]
COPY ["src/PaymentService.Domain/PaymentService.Domain.csproj", "PaymentService.Domain/"]
COPY ["src/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj", "PaymentService.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "PaymentService.Worker/PaymentService.Worker.csproj"

# Copiar código fuente
COPY src/PaymentService.Worker/ PaymentService.Worker/
COPY src/PaymentService.Application/ PaymentService.Application/
COPY src/PaymentService.Domain/ PaymentService.Domain/
COPY src/PaymentService.Infrastructure/ PaymentService.Infrastructure/

# Compilar
RUN dotnet build "PaymentService.Worker/PaymentService.Worker.csproj" -c Release -o /app/build

# Publicar
RUN dotnet publish "PaymentService.Worker/PaymentService.Worker.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copiar artefactos compilados
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 8080

# Inicializar aplicación
ENTRYPOINT ["dotnet", "PaymentService.Worker.dll"]