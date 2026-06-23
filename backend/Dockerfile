FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers for better caching
COPY ["EMS.API/EMS.API.csproj", "EMS.API/"]
RUN dotnet restore "EMS.API/EMS.API.csproj"

# Copy everything else and publish
COPY . .
WORKDIR /src/EMS.API
RUN dotnet publish "EMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production

# Create a non-root user for improved security
RUN addgroup -S app && adduser -S app -G app

COPY --from=build /app/publish .
USER app

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s CMD wget --no-verbose --tries=1 --spider http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "EMS.API.dll"]
