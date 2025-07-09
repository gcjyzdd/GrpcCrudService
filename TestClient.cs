using Grpc.Net.Client;
using JobService;

class TestClient
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing JobService gRPC API...");
        
        using var channel = GrpcChannel.ForAddress("http://localhost:5104");
        var client = new JobService.JobServiceClient(channel);

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
            
            Console.WriteLine($"Create Job Response: {createResponse.Success} - {createResponse.Message}");
            if (createResponse.Success)
            {
                Console.WriteLine($"Created Job ID: {createResponse.Job.Id}");
            }

            // Test 2: Get all jobs
            Console.WriteLine("\n2. Getting all jobs...");
            var getAllResponse = await client.GetAllJobsAsync(new GetAllJobsRequest());
            Console.WriteLine($"Get All Jobs Response: {getAllResponse.Success} - {getAllResponse.Message}");
            Console.WriteLine($"Total jobs: {getAllResponse.Jobs.Count}");

            if (getAllResponse.Jobs.Count > 0)
            {
                var firstJob = getAllResponse.Jobs[0];
                Console.WriteLine($"First job: ID={firstJob.Id}, Name={firstJob.Name}, WorkDir={firstJob.WorkDir}, Cluster={firstJob.ClusterName}");

                // Test 3: Get specific job
                Console.WriteLine("\n3. Getting specific job...");
                var getResponse = await client.GetJobAsync(new GetJobRequest { Id = firstJob.Id });
                Console.WriteLine($"Get Job Response: {getResponse.Success} - {getResponse.Message}");

                // Test 4: Update job
                Console.WriteLine("\n4. Updating job...");
                var updateResponse = await client.UpdateJobAsync(new UpdateJobRequest
                {
                    Id = firstJob.Id,
                    Name = "Updated Test Job",
                    WorkDir = "/tmp/updated",
                    ClusterName = "updated-cluster"
                });
                Console.WriteLine($"Update Job Response: {updateResponse.Success} - {updateResponse.Message}");

                // Test 5: Delete job
                Console.WriteLine("\n5. Deleting job...");
                var deleteResponse = await client.DeleteJobAsync(new DeleteJobRequest { Id = firstJob.Id });
                Console.WriteLine($"Delete Job Response: {deleteResponse.Success} - {deleteResponse.Message}");
            }

            Console.WriteLine("\nAll tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}