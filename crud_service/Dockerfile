# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar archivos de proyecto (hexagonal architecture)
COPY ["src/CrudService.Api/CrudService.Api.csproj", "CrudService.Api/"]
COPY ["src/CrudService.Application/CrudService.Application.csproj", "CrudService.Application/"]
COPY ["src/CrudService.Domain/CrudService.Domain.csproj", "CrudService.Domain/"]
COPY ["src/CrudService.Infrastructure/CrudService.Infrastructure.csproj", "CrudService.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "CrudService.Api/CrudService.Api.csproj"

# Copiar código fuente
COPY src/CrudService.Api/ CrudService.Api/
COPY src/CrudService.Application/ CrudService.Application/
COPY src/CrudService.Domain/ CrudService.Domain/
COPY src/CrudService.Infrastructure/ CrudService.Infrastructure/

# Compilar
RUN dotnet build "CrudService.Api/CrudService.Api.csproj" -c Release -o /app/build

# Publicar
RUN dotnet publish "CrudService.Api/CrudService.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copiar artefactos compilados
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 8080

# Inicializar aplicación
ENTRYPOINT ["dotnet", "CrudService.Api.dll"]
