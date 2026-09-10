########################################
# Build stage
########################################
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY WebAppTemplate1/WebAppTemplate1/WebAppTemplate1.csproj WebAppTemplate1/WebAppTemplate1/
COPY WebAppTemplate1/WebAppTemplate1.Client/WebAppTemplate1.Client.csproj WebAppTemplate1/WebAppTemplate1.Client/

RUN dotnet restore WebAppTemplate1/WebAppTemplate1/WebAppTemplate1.csproj

# Copy the rest of the source code
COPY . .

# Publish the server project (this also builds/publishes the Client WASM project as a dependency)
RUN dotnet publish WebAppTemplate1/WebAppTemplate1/WebAppTemplate1.csproj \
	-c Release \
	-o /app/publish \
	--no-restore

########################################
# Runtime stage
########################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# DigitalOcean App Platform provides the PORT env var; ASP.NET Core listens on the URL from ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebAppTemplate1.dll"]