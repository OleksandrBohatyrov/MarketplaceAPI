FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MarketplaceAPI.csproj", "./"]
RUN dotnet restore "MarketplaceAPI.csproj"
COPY . .
RUN dotnet publish "MarketplaceAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MarketplaceAPI.dll"]
