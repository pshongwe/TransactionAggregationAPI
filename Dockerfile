# === Build stage ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY TransactionAggregation.sln ./
COPY TransactionAggregation.Api/TransactionAggregation.Api.csproj TransactionAggregation.Api/
COPY TransactionAggregation.Domain/TransactionAggregation.Domain.csproj TransactionAggregation.Domain/
COPY TransactionAggregation.Tests/TransactionAggregation.Tests.csproj TransactionAggregation.Tests/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Publish the API project (Release)
RUN dotnet publish TransactionAggregation.Api/TransactionAggregation.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# === Runtime stage ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Listen on 8080 externally
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TransactionAggregation.Api.dll"]