FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and all project files for restore caching
COPY microservices-reference-arch.slnx .
COPY src/Services/Orders/Orders.API/Orders.API.csproj src/Services/Orders/Orders.API/
COPY src/Services/Orders/Orders.Application/Orders.Application.csproj src/Services/Orders/Orders.Application/
COPY src/Services/Orders/Orders.Domain/Orders.Domain.csproj src/Services/Orders/Orders.Domain/
COPY src/Services/Orders/Orders.Infrastructure/Orders.Infrastructure.csproj src/Services/Orders/Orders.Infrastructure/
COPY src/Shared/Shared.Contracts/Shared.Contracts.csproj src/Shared/Shared.Contracts/
COPY src/Shared/Shared.Observability/Shared.Observability.csproj src/Shared/Shared.Observability/
COPY src/Services/Inventory/Inventory.API/Inventory.API.csproj src/Services/Inventory/Inventory.API/
COPY src/Services/Inventory/Inventory.Application/Inventory.Application.csproj src/Services/Inventory/Inventory.Application/
COPY src/Services/Inventory/Inventory.Domain/Inventory.Domain.csproj src/Services/Inventory/Inventory.Domain/
COPY src/Services/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj src/Services/Inventory/Inventory.Infrastructure/
COPY src/Services/Notifications/Notifications.Worker/Notifications.Worker.csproj src/Services/Notifications/Notifications.Worker/
COPY src/Gateway/Gateway.API/Gateway.API.csproj src/Gateway/Gateway.API/
RUN dotnet restore src/Services/Orders/Orders.API/Orders.API.csproj

# Copy all source and publish
COPY . .
RUN dotnet publish src/Services/Orders/Orders.API/Orders.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Orders.API.dll"]
