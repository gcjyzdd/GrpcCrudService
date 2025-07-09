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
# Service runs on http://localhost:5104
```

## Technologies Used

- .NET 8
- gRPC
- Entity Framework Core
- SQLite
- Protocol Buffers

## Getting Started

1. Clone the repository
2. Restore packages: `dotnet restore`
3. Build the project: `dotnet build`
4. Run database migrations: `dotnet ef database update`
5. Start the service: `dotnet run`

## Testing

A test client is provided (`TestClient.cs`) to demonstrate all CRUD operations:

```csharp
// Example usage
var client = new JobService.JobServiceClient(channel);

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
├── Models/
│   └── Job.cs
├── Data/
│   └── JobContext.cs
├── Services/
│   └── JobService.cs
├── Protos/
│   └── job.proto
├── Program.cs
└── appsettings.json
```