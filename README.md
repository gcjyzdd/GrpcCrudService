# JobService - gRPC CRUD API Tutorial

This tutorial demonstrates how to build a .NET gRPC service with Entity Framework Core to provide CRUD operations for a jobs table.

## Project Overview

This project creates a gRPC service that manages job records with the following properties:
- **Id**: Unique identifier (int)
- **Name**: Job name (string)
- **WorkDir**: Working directory path (string)
- **ClusterName**: Name of the cluster (string)
- **CreatedAt**: Creation timestamp (DateTime)

## Tutorial Steps

### Step 1: Create .NET gRPC Project ✅
```bash
dotnet new grpc -n JobService
cd JobService
git init
git add .
git commit -m "Initial commit: Create .NET gRPC project"
```

### Step 2: Add Entity Framework Core Packages ✅
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### Step 3: Create Job Entity Model ✅
```csharp
// Models/Job.cs
public class Job
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string WorkDir { get; set; } = string.Empty;
    [Required] public string ClusterName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Step 4: Create DbContext ✅
```csharp
// Data/JobContext.cs
public class JobContext : DbContext
{
    public JobContext(DbContextOptions<JobContext> options) : base(options) { }
    public DbSet<Models.Job> Jobs { get; set; }
}
```

### Step 5: Define gRPC Service Proto File ✅
```proto
// Protos/job.proto
service JobService {
  rpc CreateJob (CreateJobRequest) returns (JobResponse);
  rpc GetJob (GetJobRequest) returns (JobResponse);
  rpc GetAllJobs (GetAllJobsRequest) returns (GetAllJobsResponse);
  rpc UpdateJob (UpdateJobRequest) returns (JobResponse);
  rpc DeleteJob (DeleteJobRequest) returns (DeleteJobResponse);
}
```

### Step 6: Implement gRPC Service ✅
```csharp
// Services/JobService.cs
public class JobGrpcService : JobService.JobServiceBase
{
    // Full CRUD implementation with error handling
}
```

### Step 7: Configure Services ✅
```csharp
// Program.cs
builder.Services.AddGrpc();
builder.Services.AddDbContext<JobContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
app.MapGrpcService<JobGrpcService>();
```

### Step 8: Database Migration ✅
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Step 9: Test the Service ✅
```bash
dotnet run
# Service runs on named pipes for local communication
```

## Technologies Used

- .NET 8
- gRPC over Named Pipes/Unix Domain Sockets
- Entity Framework Core
- SQLite
- Protocol Buffers

## Getting Started

### Running the Server
1. Navigate to the JobService.Server directory
2. Restore packages: `dotnet restore`
3. Build the project: `dotnet build`
4. Start the service: `dotnet run`

The server will automatically:
- Create the SQLite database if it doesn't exist
- Apply any pending migrations
- Start listening on named pipes for local communication

**Connection Details:**
- **Windows**: Named pipe `JobServicePipe`
- **Unix/Linux**: Unix domain socket at `/tmp/jobservice.sock`

Note: The database is automatically created on startup, so no manual migration commands are needed.

### Running the Client
1. Open a new terminal
2. Navigate to the JobService.Client directory
3. Build the client: `dotnet build`
4. Run the client: `dotnet run`

The client will automatically connect to the server via named pipes and demonstrate all CRUD operations.

## Testing

The `JobService.Client` project provides a comprehensive test of all CRUD operations:

```csharp
// Example usage from JobService.Client
var client = new JobService.JobService.JobServiceClient(channel);

// Create job
var createResponse = await client.CreateJobAsync(new CreateJobRequest
{
    Name = "Test Job",
    WorkDir = "/tmp/test",
    ClusterName = "test-cluster"
});

// Get all jobs
var getAllResponse = await client.GetAllJobsAsync(new GetAllJobsRequest());

// Get specific job
var getResponse = await client.GetJobAsync(new GetJobRequest { Id = 1 });

// Update job
var updateResponse = await client.UpdateJobAsync(new UpdateJobRequest
{
    Id = 1,
    Name = "Updated Job",
    WorkDir = "/tmp/updated",
    ClusterName = "updated-cluster"
});

// Delete job
var deleteResponse = await client.DeleteJobAsync(new DeleteJobRequest { Id = 1 });
```

### Building and Running Solution

To build the entire solution:
```bash
dotnet build
```

To run both server and client:
```bash
# Terminal 1 - Start server
dotnet run --project JobService.Server/JobService.csproj

# Terminal 2 - Run client
dotnet run --project JobService.Client/JobService.Client.csproj
```

## API Operations

The service provides the following gRPC operations:
- CreateJob - Create a new job
- GetJob - Retrieve a job by ID
- GetAllJobs - Retrieve all jobs
- UpdateJob - Update an existing job
- DeleteJob - Delete a job by ID

## Project Structure

```
JobService/
├── JobService.sln                      # Solution file
├── JobService.Server/                  # Server project
│   ├── JobService.csproj               # Server project file
│   ├── Models/
│   │   └── Job.cs                      # Job entity model
│   ├── Data/
│   │   └── JobContext.cs               # EF Core DbContext
│   ├── Services/
│   │   └── JobService.cs               # gRPC service implementation
│   ├── Protos/
│   │   └── job.proto                   # Shared gRPC service definition
│   ├── Program.cs                      # Server configuration
│   ├── appsettings.json                # Server configuration
│   └── Migrations/                     # EF Core migrations
└── JobService.Client/                  # Client console application
    ├── JobService.Client.csproj        # Client project
    └── Program.cs                      # Client test application
```

## Solution Structure

The solution consists of two projects:

1. **JobService.Server** - The gRPC server project that provides the job management API
2. **JobService.Client** - A console application that demonstrates all CRUD operations

Both projects share the same proto file (`JobService.Server/Protos/job.proto`) to ensure consistency between server and client.

### Key Features:
- **Named Pipe Communication**: Uses named pipes (Windows) or Unix domain sockets (Linux/Mac) for fast local communication
- **Automatic Database Creation**: The server automatically creates the SQLite database on startup
- **Shared Proto Files**: Both client and server use the same service definition
- **Comprehensive Testing**: The client tests all CRUD operations with detailed output
- **Cross-Platform Support**: Works on Windows (named pipes) and Unix-like systems (domain sockets)
- **Error Handling**: Proper error handling and logging throughout the application