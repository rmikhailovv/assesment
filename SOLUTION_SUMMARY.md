# Solution Summary

## Overview

This is a complete ASP.NET Core REST API application with PostgreSQL database using code-first approach. The application manages independent tree structures and maintains an exception journal for all API errors.

## Architecture

### Layered Architecture Pattern

```
┌─────────────────────────────────────┐
│      Assesment.Api (REST API)       │
│  - Controllers                      │
│  - Middleware (Exception Handling)  │
│  - Program.cs (DI Configuration)    │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Assesment.Application (Services)  │
│  - TreeService                      │
│  - JournalService                   │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Assesment.Infrastructure           │
│  (Repository Interfaces)            │
│  - ITreeNodeRepository              │
│  - IExceptionJournalRepository      │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Assesment.Infrastructure.Postgres  │
│  - AssessmentDbContext              │
│  - TreeNodeRepository               │
│  - ExceptionJournalRepository       │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│     Assesment.Domain (Models)       │
│  - TreeNode                         │
│  - ExceptionJournal                 │
│  - SecureException                  │
└─────────────────────────────────────┘
```

## Key Components

### 1. Domain Layer (Assesment.Domain)

**TreeNode.cs**
- Represents a node in a tree structure
- Properties: Id, Name, TreeName, ParentId, Parent, Children
- Each node belongs to a single tree (TreeName)
- Hierarchical relationship with parent/children

**ExceptionJournal.cs**
- Tracks all exceptions during API request processing
- Properties: Id, EventId, CreatedAt, ExceptionType, Message, StackTrace, QueryParameters, BodyParameters, Endpoint
- EventId generated from timestamp ticks

**SecureException.cs**
- Custom exception class for business logic errors
- Provides user-friendly error messages
- Distinguished from system exceptions in exception handling

### 2. Infrastructure Layer

**ITreeNodeRepository**
```csharp
- GetTreeAsync(string treeName) - Retrieve entire tree with all nodes
- GetNodeByIdAsync(long nodeId) - Get single node
- CreateNodeAsync(...) - Create new node (validates uniqueness among siblings)
- DeleteNodeAsync(long nodeId) - Delete node (requires no children)
- RenameNodeAsync(...) - Rename node (validates uniqueness among siblings)
```

**IExceptionJournalRepository**
```csharp
- CreateAsync(ExceptionJournal) - Log exception
- GetByIdAsync(long id) - Get journal entry by ID
- GetRangeAsync(...) - Paginated journal with filtering
```

### 3. Data Access Layer (Assesment.Infrastructure.Postgres)

**AssessmentDbContext**
- Entity Framework Core DbContext
- Configures:
  - TreeNode with cascade delete on parent-child relationship
  - Unique index on (TreeName, ParentId, Name) for sibling uniqueness
  - ExceptionJournal with indexes on EventId and CreatedAt

**TreeNodeRepository**
- Implements tree operations with proper validation
- Loads entire tree hierarchy recursively
- Enforces business rules:
  - Node names must be unique among siblings
  - Cannot delete nodes with children
  - All children must belong to same tree as parent

**ExceptionJournalRepository**
- Logs all exceptions with full context
- Supports pagination and filtering (by date range, search text)

### 4. Application Layer (Assesment.Application)

**TreeService**
- Orchestrates tree operations
- Returns virtual root node for empty trees
- Delegates to repository for actual operations

**JournalService**
- Provides access to exception logs
- Supports filtering and pagination

### 5. API Layer (Assesment.Api)

**Generated Controllers (from schema.yaml via NSwag)**
- **ApiControllerBase** (Generated/ApiControllerBase.cs) - Abstract base controller with routes and models
  - All routes match schema.yaml exactly
  - Models (MNode, MJournal, MJournalInfo, etc.) generated from schema
  
- **ApiController** (Controllers/ApiController.cs) - Concrete implementation
  - Inherits from ApiControllerBase
  - Implements all abstract methods with business logic
  - Routes:
    - POST /api.user.tree.get
    - POST /api.user.tree.node.create
    - POST /api.user.tree.node.delete
    - POST /api.user.tree.node.rename
    - POST /api.user.journal.getRange
    - POST /api.user.journal.getSingle
    - POST /api.user.partner.rememberMe

**ExceptionHandlingMiddleware**
- Intercepts all exceptions
- Logs to ExceptionJournal with full context
- Returns appropriate response:
  - SecureException: `{"type": "Secure", "id": "...", "data": {"message": "..."}}`
  - Other: `{"type": "Exception", "id": "...", "data": {"message": "Internal server error ID = ..."}}`

**Program.cs**
- Configures dependency injection
- Sets up EF Core with PostgreSQL
- Configures JWT authentication (optional)
- Registers middleware and services

### 6. Client SDK (Assesment.Client)

**Generated API Client (from schema.yaml via NSwag)**
- Auto-generated from schema.yaml using NSwag
- Type-safe client for consuming the API
- Located in Generated/AssessmentApiClient.cs
- Methods:
  - Api_user_tree_getAsync(treeName)
  - Api_user_tree_node_createAsync(treeName, parentNodeId, nodeName)
  - Api_user_tree_node_deleteAsync(nodeId)
  - Api_user_tree_node_renameAsync(nodeId, newNodeName)
  - Api_user_journal_getRangeAsync(skip, take, filter)
  - Api_user_journal_getSingleAsync(id)
  - Api_user_partner_rememberMeAsync(code)

### 7. Migrations (Assesment.Infrastructure.Postgres.Migrations)

**Console Application**
- Runs EF Core migrations on startup
- Used in Docker container
- Exits after completion
- Configuration via appsettings.json or environment variables

### 8. Tests (Assesment.Tests)

**Integration Tests**
- Uses WebApplicationFactory to host API in-memory
- Uses generated client SDK for API calls
- Connects to real PostgreSQL from Docker Compose
- Test categories:
  - **TreeIntegrationTests**: Node operations, validation, tree hierarchy
  - **JournalIntegrationTests**: Exception logging, journal queries

## Database Schema

### TreeNodes Table
```sql
CREATE TABLE TreeNodes (
    Id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    Name VARCHAR(500) NOT NULL,
    TreeName VARCHAR(500) NOT NULL,
    ParentId BIGINT NULL,
    FOREIGN KEY (ParentId) REFERENCES TreeNodes(Id) ON DELETE CASCADE,
    UNIQUE (TreeName, ParentId, Name)
);

CREATE INDEX IX_TreeNodes_TreeName ON TreeNodes(TreeName);
```

### ExceptionJournals Table
```sql
CREATE TABLE ExceptionJournals (
    Id BIGINT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    EventId BIGINT NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    ExceptionType VARCHAR(500) NOT NULL,
    Message TEXT NOT NULL,
    StackTrace TEXT NOT NULL,
    QueryParameters TEXT NOT NULL,
    BodyParameters TEXT NOT NULL,
    Endpoint VARCHAR(500) NOT NULL
);

CREATE INDEX IX_ExceptionJournals_EventId ON ExceptionJournals(EventId);
CREATE INDEX IX_ExceptionJournals_CreatedAt ON ExceptionJournals(CreatedAt);
```

## Configuration

### Connection Strings
- Development: `Host=localhost;Port=5432;Database=db;Username=postgres;Password=local_dev`
- Docker: `Host=postgres;Port=5432;Database=db;Username=postgres;Password=local_dev`

### JWT Settings (Optional)
```json
{
  "Jwt": {
    "Key": "your-secret-key-at-least-32-characters-long-for-security",
    "Issuer": "AssessmentApi",
    "Audience": "AssessmentApiUsers",
    "ExpiryMinutes": 60
  }
}
```

## Docker Compose Setup

**Services:**
1. **postgres** - PostgreSQL database with health check
2. **migrations** - Runs EF Core migrations and exits

## Build and Run

### Quick Setup
```bash
./setup.sh
```

### Manual Setup
```bash
# Install tools
dotnet tool install -g NSwag.ConsoleCore
dotnet tool install -g dotnet-ef

# Generate client
cd Assesment.Client
nswag openapi2csclient /input:../schema.yaml /classname:AssessmentApiClient /namespace:Assesment.Client /output:Generated/AssessmentApiClient.cs /generateClientInterfaces:true /injectHttpClient:true /useBaseUrl:true
cd ..

# Build
dotnet build

# Start database
docker-compose up -d

# Run API
cd Assesment.Api
dotnet run
```

### Run Tests
```bash
# Ensure PostgreSQL is running
docker-compose up -d postgres

# Run tests
dotnet test
```

## API Usage Examples

### Create Tree with Nodes
```bash
# Create root node (creates tree if doesn't exist)
curl -X POST "http://localhost:5000/api.user.tree.node.create?treeName=MyTree&nodeName=Root"

# Create child node
curl -X POST "http://localhost:5000/api.user.tree.node.create?treeName=MyTree&parentNodeId=1&nodeName=Child1"

# Get entire tree
curl -X POST "http://localhost:5000/api.user.tree.get?treeName=MyTree"
```

### Response Example
```json
{
  "id": 0,
  "name": "MyTree",
  "children": [
    {
      "id": 1,
      "name": "Root",
      "children": [
        {
          "id": 2,
          "name": "Child1",
          "children": []
        }
      ]
    }
  ]
}
```

### Exception Handling
```bash
# Try to create duplicate node (triggers SecureException)
curl -X POST "http://localhost:5000/api.user.tree.node.create?treeName=MyTree&nodeName=Root"

# Response:
{
  "type": "Secure",
  "id": "638136064526554554",
  "data": {
    "message": "A root node with name 'Root' already exists in tree 'MyTree'."
  }
}

# View exception in journal
curl -X POST "http://localhost:5000/api.user.journal.getRange?skip=0&take=10" \
  -H "Content-Type: application/json" \
  -d '{}'
```

## Key Design Decisions

1. **Code-First Approach**: EF Core migrations manage database schema
2. **Repository Pattern**: Abstracts data access, enables testing
3. **Middleware for Exception Handling**: Centralized exception logging and response formatting
4. **NSwag Code Generation**: 
   - **Server-side**: Abstract controller base with routes and models generated from schema.yaml
   - **Client-side**: Type-safe client SDK generated from schema.yaml
   - Ensures API contract consistency between server and client
   - Controllers and models always match the OpenAPI specification
5. **Integration Tests**: Test through actual HTTP calls using generated client
6. **Docker Compose**: Consistent development environment
7. **Separation of Concerns**: Clear layer boundaries, single responsibility

## Security Considerations

1. JWT authentication implemented (optional, can be enabled)
2. Secure exceptions provide user-friendly messages without exposing internals
3. System exceptions masked with generic error message
4. All exceptions logged with full details for troubleshooting
5. Connection strings should be stored in secure configuration (Azure Key Vault, etc.)

## Future Enhancements

1. Add user context to tree operations (multi-tenancy)
2. Implement soft delete for nodes
3. Add node versioning/history
4. Implement bulk operations
5. Add caching layer (Redis)
6. Add API rate limiting
7. Implement real user authentication (not just codes)
8. Add metrics and monitoring (Application Insights, Prometheus)
9. Add API documentation endpoint (Swagger UI)
10. Implement CQRS pattern for scalability
