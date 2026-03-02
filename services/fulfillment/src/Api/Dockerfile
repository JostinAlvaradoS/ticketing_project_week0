FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the Fulfillment src to keep context small and preserve project references
COPY services/fulfillment/src ./services/fulfillment/src
WORKDIR /src/services/fulfillment/src/Api

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# Install fonts for PDF generation (User: root is required for apt-get)
USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
    fontconfig \
    libfreetype6 \
    libfontconfig1 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004
ENTRYPOINT ["dotnet", "Fulfillment.Api.dll"]
