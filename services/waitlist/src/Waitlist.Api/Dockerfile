FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/waitlist/src ./services/waitlist/src
WORKDIR /src/services/waitlist/src/Waitlist.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN printf '#!/bin/bash\nset -e\necho "Starting Waitlist API..."\nexec dotnet Waitlist.Api.dll\n' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5006
EXPOSE 5006
ENTRYPOINT ["/app/entrypoint.sh"]
