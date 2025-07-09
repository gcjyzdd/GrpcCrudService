# JobService Project Requirements

## Project Description
Create a new .NET project with gRPC and Entity Framework Core to provide CRUD services for managing a jobs table.

## Job Entity Requirements
Each job record must contain:
- **id**: Unique identifier (int, primary key)
- **name**: Job name (string, required)
- **workdir**: Working directory path (string, required)
- **cluster name**: Name of the cluster (string, required)
- **createdAt**: Creation timestamp (DateTime, auto-generated)

## Technical Requirements
- Use .NET 8 with gRPC template
- Implement Entity Framework Core for database operations
- Use SQL Server as the database provider
- Create proper database migrations
- Implement full CRUD operations via gRPC:
  - Create new job
  - Read job by ID
  - Read all jobs
  - Update existing job
  - Delete job by ID

## Development Approach
- Tutorial-style implementation with step-by-step commits
- Document each step in README.md
- Commit after each major milestone
- No Claude references in commit messages

## Project Structure
```
JobService/
├── Models/Job.cs          # Job entity model
├── Data/JobContext.cs     # EF Core DbContext
├── Services/JobService.cs # gRPC service implementation
├── Protos/job.proto       # gRPC service definition
├── Program.cs            # Service configuration
└── appsettings.json      # Configuration including DB connection
```

## Database Configuration
- Use SQL Server with Entity Framework Core
- Connection string in appsettings.json
- Automatic database creation via migrations

## Testing
- Test all CRUD operations
- Verify gRPC service functionality
- Ensure proper error handling

## Commands to Remember
```bash
# Build the project
dotnet build

# Run the service
dotnet run

# Create migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```