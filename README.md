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

### Step 3: Create Job Entity Model
Create the Job entity class with required properties.

### Step 4: Create DbContext
Set up Entity Framework DbContext for database operations.

### Step 5: Define gRPC Service Proto File
Create .proto file with service definitions for CRUD operations.

### Step 6: Implement gRPC Service
Implement the gRPC service with Create, Read, Update, Delete operations.

### Step 7: Configure Services
Configure Entity Framework and gRPC services in Program.cs.

### Step 8: Database Migration
Create and apply database migrations.

### Step 9: Test the Service
Test all CRUD operations using gRPC client.

## Technologies Used

- .NET 8
- gRPC
- Entity Framework Core
- SQL Server
- Protocol Buffers

## Getting Started

1. Clone the repository
2. Ensure SQL Server is running
3. Update connection string in appsettings.json
4. Run database migrations
5. Start the service

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