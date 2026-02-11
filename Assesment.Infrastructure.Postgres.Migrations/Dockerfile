FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["Assesment.Infrastructure.Postgres.Migrations/Assesment.Infrastructure.Postgres.Migrations.csproj", "Assesment.Infrastructure.Postgres.Migrations/"]
COPY ["Assesment.Infrastructure.Postgres/Assesment.Infrastructure.Postgres.csproj", "Assesment.Infrastructure.Postgres/"]
COPY ["Assesment.Domain/Assesment.Domain.csproj", "Assesment.Domain/"]
COPY ["Assesment.Infrastructure/Assesment.Infrastructure.csproj", "Assesment.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Assesment.Infrastructure.Postgres.Migrations/Assesment.Infrastructure.Postgres.Migrations.csproj"

# Copy all source files
COPY . .

# Build
WORKDIR "/src/Assesment.Infrastructure.Postgres.Migrations"
RUN dotnet build "Assesment.Infrastructure.Postgres.Migrations.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Assesment.Infrastructure.Postgres.Migrations.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Assesment.Infrastructure.Postgres.Migrations.dll"]
