FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/BuildingBlocks/", "src/BuildingBlocks/"]
COPY ["src/Services/Ordering/Ordering.Domain/", "src/Services/Ordering/Ordering.Domain/"]
COPY ["src/Services/Ordering/Ordering.Application/", "src/Services/Ordering/Ordering.Application/"]
COPY ["src/Services/Inventory/Inventory.Domain/", "src/Services/Inventory/Inventory.Domain/"]
COPY ["src/Services/Inventory/Inventory.Application/", "src/Services/Inventory/Inventory.Application/"]
COPY ["src/Services/Identity/", "src/Services/Identity/"]
RUN dotnet restore "src/Services/Identity/Identity.Api/Identity.Api.csproj"
RUN dotnet publish "src/Services/Identity/Identity.Api/Identity.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
