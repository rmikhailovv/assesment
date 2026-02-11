# Assessment REST API

ASP.NET Core REST API application with PostgreSQL database using code-first approach.

## Project Structure

- **Assesment.Domain** - Domain models (TreeNode, ExceptionJournal, SecureException)
- **Assesment.Infrastructure** - Repository interfaces
- **Assesment.Infrastructure.Postgres** - EF Core DbContext and repository implementations
- **Assesment.Infrastructure.Postgres.Migrations** - Console app for running migrations
- **Assesment.Application** - Business logic services
- **Assesment.Api** - REST API with auto-generated controllers from schema.yaml
  - Generated/ApiControllerBase.cs - Abstract controller base with routes and models
  - Controllers/ApiController.cs - Concrete implementation
- **Assesment.Client** - Auto-generated API client from schema.yaml
  - Generated/AssessmentApiClient.cs - Type-safe client SDK
- **Assesment.Tests** - Integration tests using WebApplicationFactory

## Features

### Database Design

1. **Tree Nodes**
   - Each node belongs to a single tree
   - All child nodes belong to the same tree as their parent
   - Mandatory name field for each node
   - Unique constraint on node names among siblings

2. **Exception Journal**
   - Tracks all exceptions during REST API request processing
   - Stores: Event ID, Timestamp, Query/Body parameters, Stack trace

### Exception Handling

- **SecureException**: Custom exception with detailed error response
  - Response format: `{"type": "Secure", "id": "event_id", "data": {"message": "error_message"}}`
  
- **Other Exceptions**: Logged with generic error response
  - Response format: `{"type": "Exception", "id": "event_id", "data": {"message": "Internal server error ID = event_id"}}`

### API Endpoints

Based on `schema.yaml`:

- **Tree Management**
  - `POST /api.user.tree.get` - Get or create tree
  - `POST /api.user.tree.node.create` - Create node
  - `POST /api.user.tree.node.delete` - Delete node (must delete children first)
  - `POST /api.user.tree.node.rename` - Rename node

- **Exception Journal**
  - `POST /api.user.journal.getRange` - Get paginated journal entries
  - `POST /api.user.journal.getSingle` - Get single journal entry by ID

- **Authentication (Optional)**
  - `POST /api.user.partner.rememberMe` - Get JWT token by code

## Prerequisites

- .NET 10.0 SDK
- Docker and Docker Compose
- PostgreSQL (via Docker)

## Setup Instructions

### 1. Install Required Tools

```bash
# Install NSwag CLI (for client generation)
dotnet tool install -g NSwag.ConsoleCore

# Install EF Core CLI (for migrations)
dotnet tool install -g dotnet-ef

# Add tools to PATH (if needed)
export PATH="$PATH:$HOME/.dotnet/tools"
```

### 2. Generate API Controllers and Client Code

```bash
# Generate API controllers
cd Assesment.Api
nswag openapi2cscontroller /input:../schema.yaml /classname:ApiControllerBase /namespace:Assesment.Api.Generated /output:Generated/ApiControllerBase.cs /ControllerStyle:Abstract /ControllerBaseClass:Microsoft.AspNetCore.Mvc.ControllerBase /RouteNamingStrategy:None /UseCancellationToken:true
cd ..

# Generate client SDK
cd Assesment.Client
nswag openapi2csclient /input:../schema.yaml /classname:AssessmentApiClient /namespace:Assesment.Client /output:Generated/AssessmentApiClient.cs /generateClientInterfaces:true /injectHttpClient:true /useBaseUrl:true
cd ..
```

### 3. Build Solution

```bash
dotnet build
```

### 4. Start PostgreSQL with Docker Compose

```bash
docker-compose up -d
```

This will:
- Start PostgreSQL on port 5432
- Run migrations automatically via the `migrations` service
- Migrations container will exit after completing

### 5. Run the API

```bash
cd Assesment.Api
dotnet run
```

API will be available at: `http://localhost:5000` (or as configured)

### 6. Run Tests

Make sure Docker Compose is running with PostgreSQL:

```bash
docker-compose up -d postgres
```

Then run the tests:

```bash
dotnet test
```

Tests use the real PostgreSQL instance from Docker Compose.

## Configuration

### API Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=db;Username=postgres;Password=local_dev"
  },
  "Jwt": {
    "Key": "your-secret-key-at-least-32-characters-long-for-security",
    "Issuer": "AssessmentApi",
    "Audience": "AssessmentApiUsers",
    "ExpiryMinutes": 60
  }
}
```

### Test Configuration

Tests use environment variable `ConnectionStrings__DefaultConnection` or default to:
`Host=localhost;Port=5432;Database=db;Username=postgres;Password=local_dev`

## Docker Compose Services

### postgres
- PostgreSQL 16 database
- Port: 5432
- Credentials: postgres/local_dev
- Database: db

### migrations
- Runs EF Core migrations on startup
- Exits after completion
- Depends on postgres health check

## Development Workflow

1. Make changes to domain models
2. Create migrations:
   ```bash
   dotnet ef migrations add <MigrationName> --project Assesment.Infrastructure.Postgres --startup-project Assesment.Api
   ```
3. Rebuild solution: `dotnet build`
4. Restart API or run tests

## API Usage Examples

### Get or Create Tree

```bash
curl -X POST "http://localhost:5000/api.user.tree.get?treeName=MyTree"
```

### Create Root Node

```bash
curl -X POST "http://localhost:5000/api.user.tree.node.create?treeName=MyTree&nodeName=RootNode"
```

### Create Child Node

```bash
curl -X POST "http://localhost:5000/api.user.tree.node.create?treeName=MyTree&parentNodeId=1&nodeName=ChildNode"
```

### Rename Node

```bash
curl -X POST "http://localhost:5000/api.user.tree.node.rename?nodeId=1&newNodeName=NewName"
```

### Delete Node

```bash
curl -X POST "http://localhost:5000/api.user.tree.node.delete?nodeId=2"
```

### Get Exception Journal

```bash
curl -X POST "http://localhost:5000/api.user.journal.getRange?skip=0&take=10" \
  -H "Content-Type: application/json" \
  -d '{}'
```

### Authenticate (Optional)

```bash
curl -X POST "http://localhost:5000/api.user.partner.rememberMe?code=mycode"
```

Returns JWT token that can be used in subsequent requests:
```bash
curl -X POST "http://localhost:5000/api.user.tree.get?treeName=MyTree" \
  -H "Authorization: Bearer <your-jwt-token>"
```

## Testing

Integration tests are located in `Assesment.Tests` and use:
- **WebApplicationFactory** for hosting the API
- **Generated Client** from Assesment.Client
- **Real PostgreSQL** from Docker Compose
- **xUnit** as test framework
- **FluentAssertions** for assertions

Test categories:
- **TreeIntegrationTests**: Tree and node operations
- **JournalIntegrationTests**: Exception journal functionality

## Troubleshooting

### Migrations fail to run
- Ensure PostgreSQL is running: `docker-compose up -d postgres`
- Check connection string in appsettings.json
- Verify postgres health: `docker-compose ps`

### Tests fail to connect to database
- Ensure PostgreSQL is running: `docker-compose up -d postgres`
- Check port 5432 is not in use by another process
- Verify connection string matches docker-compose configuration

### Build errors with NSwag
- Ensure NSwag.ConsoleCore is installed globally
- Regenerate client code manually (see step 2 above)
- Check that schema.yaml exists in project root

## License

This is an assessment project.
