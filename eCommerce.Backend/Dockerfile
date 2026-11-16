FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY eCommerce.Backend/eCommerce.Backend.csproj eCommerce.Backend/
COPY eCommerce.BL/eCommerce.BL.csproj eCommerce.BL/
COPY eCommerce.DL/eCommerce.DL.csproj eCommerce.DL/
COPY eCommerce.Domain/eCommerce.Domain.csproj eCommerce.Domain/
COPY eCommerce.ServiceInterfaces/eCommerce.ServiceInterfaces.csproj eCommerce.ServiceInterfaces/
RUN dotnet restore eCommerce.Backend/eCommerce.Backend.csproj
COPY . .
RUN dotnet publish eCommerce.Backend/eCommerce.Backend.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

USER app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "eCommerce.Backend.dll"]


