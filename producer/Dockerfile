# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivos de proyecto (hexagonal architecture)
COPY ["src/Producer.Api/Producer.Api.csproj", "Producer.Api/"]
COPY ["src/Producer.Application/Producer.Application.csproj", "Producer.Application/"]
COPY ["src/Producer.Domain/Producer.Domain.csproj", "Producer.Domain/"]
COPY ["src/Producer.Infrastructure/Producer.Infrastructure.csproj", "Producer.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "Producer.Api/Producer.Api.csproj"

# Copiar código fuente
COPY src/Producer.Api/ Producer.Api/
COPY src/Producer.Application/ Producer.Application/
COPY src/Producer.Domain/ Producer.Domain/
COPY src/Producer.Infrastructure/ Producer.Infrastructure/

# Compilar
RUN dotnet build "Producer.Api/Producer.Api.csproj" -c Release -o /app/build

# Publicar
RUN dotnet publish "Producer.Api/Producer.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copiar artefactos compilados
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 8080

# Inicializar aplicación
ENTRYPOINT ["dotnet", "Producer.Api.dll"]
