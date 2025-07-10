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

## Graceful Shutdown

The server supports graceful shutdown through multiple mechanisms:

### Windows:
- **Ctrl+C**: Press Ctrl+C in the console window
- **Task Manager**: Terminate the process from Task Manager
- **Console Close**: Close the console window
- **System Shutdown**: Handles system shutdown/logoff events

### Linux/Unix:
- **Ctrl+C**: Press Ctrl+C in the terminal
- **SIGTERM**: Send SIGTERM signal to the process
- **Process Exit**: Handles process exit events

### Cleanup Actions:
- Logs shutdown initiation
- Cancels ongoing operations gracefully
- Cleans up Unix socket files (on Linux/Mac)
- Closes database connections
- Provides up to 30 seconds for graceful shutdown

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
│   ├── Infrastructure/                 # Infrastructure services
│   │   ├── GracefulShutdownService.cs  # Graceful shutdown handling
│   │   ├── ServerConfiguration.cs      # Server configuration
│   │   ├── DatabaseInitializer.cs      # Database initialization
│   │   └── ServiceCollectionExtensions.cs # Service registration
│   ├── Protos/
│   │   └── job.proto                   # Shared gRPC service definition
│   ├── Program.cs                      # Clean server startup
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
- **Clean Architecture**: Well-organized code with separation of concerns and single responsibility principle
- **Graceful Shutdown**: Handles Ctrl+C, SIGTERM, and Windows Task Manager termination gracefully
- **Automatic Database Creation**: The server automatically creates the SQLite database on startup
- **Resource Cleanup**: Automatically cleans up socket files and database connections on shutdown
- **Shared Proto Files**: Both client and server use the same service definition
- **Comprehensive Testing**: The client tests all CRUD operations with detailed output
- **Cross-Platform Support**: Works on Windows (named pipes) and Unix-like systems (domain sockets)
- **Error Handling**: Proper error handling and logging throughout the application

### Architecture:
The server follows clean architecture principles:
- **Program.cs**: Minimal startup configuration
- **Infrastructure Layer**: Contains cross-cutting concerns (shutdown, configuration, database initialization)
- **Services Layer**: Contains business logic and gRPC service implementations
- **Data Layer**: Contains Entity Framework DbContext and models
- **Dependency Injection**: Proper service registration and lifetime management

## Build Script

### Overview

The `build.sh` script provides automated building, testing, and reporting for the JobService solution.

### Usage

#### Full Build (Recommended)
```bash
./build.sh
```
Performs complete build, test, and report generation.

#### Individual Commands
```bash
./build.sh --help     # Show usage information
./build.sh --clean    # Clean solution only
./build.sh --build    # Build solution only
./build.sh --test     # Run tests only
```

### Features

#### ✅ **Automated Build Process**
- Prerequisites checking
- NuGet package restoration
- Solution compilation in Release mode
- Cross-platform support (Linux/Windows)

#### 🧪 **Unit Testing**
- Runs all NUnit tests
- Generates detailed test reports (TRX format)
- Collects code coverage data
- Reports test results summary

#### 📊 **Test Reporting**
- **Test Results**: `TestReports/test-results.trx`
- **Coverage XML**: `CoverageReports/coverage.cobertura.xml`
- **Coverage HTML**: `CoverageReports/html/index.html`

#### 🎨 **Colored Output**
- ✅ Green for success messages
- ❌ Red for errors
- ⚠️ Yellow for warnings
- ℹ️ Blue for information

### Prerequisites

- .NET 8.0 SDK
- ReportGenerator tool (auto-installed)

### Project Structure

```
JobService/
├── build.sh                    # Main build script
├── JobService.sln              # Solution file
├── JobService.Server/          # gRPC Server
├── JobService.Client/          # gRPC Client
├── JobService.Client.Tests/    # Unit Tests
├── TestReports/               # Generated test results
└── CoverageReports/           # Generated coverage reports
```

### Test Results

The build script automatically generates comprehensive reports:

#### Test Summary Example
```
📊 Test Results:
   Total Tests: 7
   Passed: 7
   Failed: 0
📈 Code Coverage: 18.2%

📁 Generated Reports:
   Test Results: TestReports/test-results.trx
   Coverage XML: CoverageReports/coverage.cobertura.xml
   Coverage HTML: CoverageReports/html/index.html
```

### CI/CD Integration

The script returns appropriate exit codes:
- `0`: Success (all tests passed)
- `1`: Failure (tests failed or build error)

Perfect for integration with CI/CD pipelines.

### Troubleshooting

#### Common Issues

1. **Permission Denied**
   ```bash
   chmod +x build.sh
   ```

2. **Missing .NET SDK**
   - Install .NET 8.0 SDK from Microsoft

3. **Build Failures**
   - Run `./build.sh --clean` first
   - Check error messages in colored output

#### Support

For issues or questions, check the generated reports in:
- `TestReports/` for test failures
- `CoverageReports/html/` for coverage details
