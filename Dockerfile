# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the whole solution (better for caching)
COPY *.sln .
COPY WebAppTemplate1/WebAppTemplate1/*.csproj .
COPY WebAppTemplate1/WebAppTemplate1.Client/*.csproj .
RUN dotnet restore

# Copy source and publish all projects
COPY . .
RUN dotnet publish WebAppTemplate1/WebAppTemplate1.sln \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://*:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebAppTemplate1.dll"]