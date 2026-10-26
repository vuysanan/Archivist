# ---- Build stage: compiles the app using the full .NET SDK ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Archivist.Api.csproj -c Release -o /app

# ---- Run stage: a smaller image that only has what's needed to run ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# .NET 8 containers listen on port 8080 by default
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Archivist.Api.dll"]
