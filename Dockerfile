# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PayrollManagementSystemMVC/PayrollManagementSystemMVC.csproj PayrollManagementSystemMVC/
RUN dotnet restore PayrollManagementSystemMVC/PayrollManagementSystemMVC.csproj

COPY PayrollManagementSystemMVC/ PayrollManagementSystemMVC/
WORKDIR /src/PayrollManagementSystemMVC
RUN dotnet publish -c Release -o /app/publish

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway assigns a random $PORT at runtime; Kestrel must bind to it.
CMD ASPNETCORE_URLS=http://+:$PORT dotnet PayrollManagementSystemMVC.dll