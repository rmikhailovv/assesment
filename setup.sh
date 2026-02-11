#!/bin/bash
set -e

echo "=== Assessment API Setup Script ==="
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK is not installed. Please install .NET 10.0 SDK first."
    exit 1
fi

# Check if docker is installed
if ! command -v docker &> /dev/null; then
    echo "ERROR: Docker is not installed. Please install Docker first."
    exit 1
fi

echo "1. Installing required tools..."
dotnet tool install -g NSwag.ConsoleCore || echo "NSwag already installed"
dotnet tool install -g dotnet-ef || echo "dotnet-ef already installed"

# Add tools to PATH
export PATH="$PATH:$HOME/.dotnet/tools"

echo ""
echo "2. Generating API controllers from schema.yaml..."
cd Assesment.Api
nswag openapi2cscontroller /input:../schema.yaml /classname:ApiControllerBase /namespace:Assesment.Api.Generated /output:Generated/ApiControllerBase.cs /ControllerStyle:Abstract /ControllerBaseClass:Microsoft.AspNetCore.Mvc.ControllerBase /RouteNamingStrategy:None /UseCancellationToken:true
cd ..

echo ""
echo "3. Generating client code from schema.yaml..."
cd Assesment.Client
nswag openapi2csclient /input:../schema.yaml /classname:AssessmentApiClient /namespace:Assesment.Client /output:Generated/AssessmentApiClient.cs /generateClientInterfaces:true /injectHttpClient:true /useBaseUrl:true
cd ..

echo ""
echo "4. Building solution..."
dotnet build

echo ""
echo "5. Starting PostgreSQL with Docker Compose..."
docker-compose up -d

echo ""
echo "6. Waiting for PostgreSQL to be ready..."
sleep 5

echo ""
echo "7. Running migrations..."
dotnet ef database update --project Assesment.Infrastructure.Postgres --startup-project Assesment.Api

echo ""
echo "=== Setup Complete! ==="
echo ""
echo "Next steps:"
echo "1. Run the API: cd Assesment.Api && dotnet run"
echo "2. Run tests: dotnet test"
echo ""
echo "API will be available at: http://localhost:5000"
echo "PostgreSQL is running on: localhost:5432"
echo ""
