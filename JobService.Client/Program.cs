using Grpc.Net.Client;
using JobService;

Console.WriteLine("JobService gRPC Client - Testing CRUD Operations");
Console.WriteLine("================================================");

// Create gRPC channel for named pipes
GrpcChannel channel;
if (OperatingSystem.IsWindows())
{
    // Windows Named Pipe
    var connectionString = "http://localhost";
    channel = GrpcChannel.ForAddress(connectionString, new GrpcChannelOptions
    {
        HttpHandler = new SocketsHttpHandler()
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var pipeClient = new System.IO.Pipes.NamedPipeClientStream(".", "JobServicePipe", System.IO.Pipes.PipeDirection.InOut);
                await pipeClient.ConnectAsync(cancellationToken);
                return pipeClient;
            }
        }
    });
    Console.WriteLine("Connecting to server via named pipe: JobServicePipe");
}
else
{
    // Unix Domain Socket
    var socketPath = Path.Combine(Path.GetTempPath(), "jobservice.sock");
    var connectionString = "http://localhost";
    channel = GrpcChannel.ForAddress(connectionString, new GrpcChannelOptions
    {
        HttpHandler = new SocketsHttpHandler()
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.Unix, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Unspecified);
                var endpoint = new System.Net.Sockets.UnixDomainSocketEndPoint(socketPath);
                await socket.ConnectAsync(endpoint, cancellationToken);
                return new System.Net.Sockets.NetworkStream(socket, true);
            }
        }
    });
    Console.WriteLine($"Connecting to server via Unix socket: {socketPath}");
}

var client = new JobService.JobService.JobServiceClient(channel);

try
{
    // Test 1: Create a job
    Console.WriteLine("\n1. Creating a job...");
    var createResponse = await client.CreateJobAsync(new CreateJobRequest
    {
        Name = "Test Job",
        WorkDir = "/tmp/test",
        ClusterName = "test-cluster"
    });
    
    Console.WriteLine($"   Success: {createResponse.Success}");
    Console.WriteLine($"   Message: {createResponse.Message}");
    
    if (createResponse.Success)
    {
        Console.WriteLine($"   Created Job ID: {createResponse.Job.Id}");
        Console.WriteLine($"   Job Name: {createResponse.Job.Name}");
        Console.WriteLine($"   Work Dir: {createResponse.Job.WorkDir}");
        Console.WriteLine($"   Cluster: {createResponse.Job.ClusterName}");
    }

    // Test 2: Get all jobs
    Console.WriteLine("\n2. Getting all jobs...");
    var getAllResponse = await client.GetAllJobsAsync(new GetAllJobsRequest());
    Console.WriteLine($"   Success: {getAllResponse.Success}");
    Console.WriteLine($"   Message: {getAllResponse.Message}");
    Console.WriteLine($"   Total jobs: {getAllResponse.Jobs.Count}");

    foreach (var job in getAllResponse.Jobs)
    {
        Console.WriteLine($"   - Job {job.Id}: {job.Name} ({job.ClusterName})");
    }

    if (getAllResponse.Jobs.Count > 0)
    {
        var firstJob = getAllResponse.Jobs[0];

        // Test 3: Get specific job
        Console.WriteLine("\n3. Getting specific job...");
        var getResponse = await client.GetJobAsync(new GetJobRequest { Id = firstJob.Id });
        Console.WriteLine($"   Success: {getResponse.Success}");
        Console.WriteLine($"   Message: {getResponse.Message}");
        
        if (getResponse.Success)
        {
            Console.WriteLine($"   Job ID: {getResponse.Job.Id}");
            Console.WriteLine($"   Job Name: {getResponse.Job.Name}");
            Console.WriteLine($"   Work Dir: {getResponse.Job.WorkDir}");
            Console.WriteLine($"   Cluster: {getResponse.Job.ClusterName}");
            Console.WriteLine($"   Created: {getResponse.Job.CreatedAt}");
        }

        // Test 4: Update job
        Console.WriteLine("\n4. Updating job...");
        var updateResponse = await client.UpdateJobAsync(new UpdateJobRequest
        {
            Id = firstJob.Id,
            Name = "Updated Test Job",
            WorkDir = "/tmp/updated",
            ClusterName = "updated-cluster"
        });
        Console.WriteLine($"   Success: {updateResponse.Success}");
        Console.WriteLine($"   Message: {updateResponse.Message}");
        
        if (updateResponse.Success)
        {
            Console.WriteLine($"   Updated Job Name: {updateResponse.Job.Name}");
            Console.WriteLine($"   Updated Work Dir: {updateResponse.Job.WorkDir}");
            Console.WriteLine($"   Updated Cluster: {updateResponse.Job.ClusterName}");
        }

        // Test 5: Create another job for demonstration
        Console.WriteLine("\n5. Creating another job...");
        var createResponse2 = await client.CreateJobAsync(new CreateJobRequest
        {
            Name = "Second Job",
            WorkDir = "/tmp/second",
            ClusterName = "second-cluster"
        });
        Console.WriteLine($"   Success: {createResponse2.Success}");
        Console.WriteLine($"   Message: {createResponse2.Message}");

        // Test 6: Get all jobs again to see both
        Console.WriteLine("\n6. Getting all jobs again...");
        var getAllResponse2 = await client.GetAllJobsAsync(new GetAllJobsRequest());
        Console.WriteLine($"   Success: {getAllResponse2.Success}");
        Console.WriteLine($"   Total jobs: {getAllResponse2.Jobs.Count}");

        foreach (var job in getAllResponse2.Jobs)
        {
            Console.WriteLine($"   - Job {job.Id}: {job.Name} ({job.ClusterName})");
        }

        // Test 7: Delete first job
        Console.WriteLine("\n7. Deleting first job...");
        var deleteResponse = await client.DeleteJobAsync(new DeleteJobRequest { Id = firstJob.Id });
        Console.WriteLine($"   Success: {deleteResponse.Success}");
        Console.WriteLine($"   Message: {deleteResponse.Message}");

        // Test 8: Final get all jobs to confirm deletion
        Console.WriteLine("\n8. Final check - getting all jobs...");
        var getAllResponse3 = await client.GetAllJobsAsync(new GetAllJobsRequest());
        Console.WriteLine($"   Success: {getAllResponse3.Success}");
        Console.WriteLine($"   Total jobs: {getAllResponse3.Jobs.Count}");

        foreach (var job in getAllResponse3.Jobs)
        {
            Console.WriteLine($"   - Job {job.Id}: {job.Name} ({job.ClusterName})");
        }
    }

    Console.WriteLine("\n✅ All tests completed successfully!");
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    if (OperatingSystem.IsWindows())
    {
        Console.WriteLine("Make sure the JobService server is running with named pipe: JobServicePipe");
    }
    else
    {
        var socketPath = Path.Combine(Path.GetTempPath(), "jobservice.sock");
        Console.WriteLine($"Make sure the JobService server is running with Unix socket: {socketPath}");
    }
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
}
