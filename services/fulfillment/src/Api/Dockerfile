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
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004
ENTRYPOINT ["dotnet", "Fulfillment.Api.dll"]
