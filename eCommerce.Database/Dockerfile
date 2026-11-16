FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY eCommerce.Database/eCommerce.Database.csproj eCommerce.Database/
RUN dotnet restore eCommerce.Database/eCommerce.Database.csproj
COPY eCommerce.Database/ eCommerce.Database/
RUN dotnet publish eCommerce.Database/eCommerce.Database.csproj -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
USER app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "eCommerce.Database.dll"]

