FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PayrollManagementSystemMVC/PayrollManagementSystemMVC.csproj PayrollManagementSystemMVC/
RUN dotnet restore PayrollManagementSystemMVC/PayrollManagementSystemMVC.csproj

COPY PayrollManagementSystemMVC/ PayrollManagementSystemMVC/
WORKDIR /src/PayrollManagementSystemMVC
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet PayrollManagementSystemMVC.dll"]
