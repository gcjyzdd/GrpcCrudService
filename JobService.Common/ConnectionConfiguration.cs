namespace JobService.Common;

public class ConnectionConfiguration : IConnectionConfiguration
{
    public string PipeName { get; set; } = "JobServicePipe";
    public string SocketPath { get; set; } = "/tmp/jobservice.sock";
    public bool IsWindows => OperatingSystem.IsWindows();
    
    public string GetConnectionIdentifier() => IsWindows ? PipeName : SocketPath;
    
    public ConnectionConfiguration() { }
    
    public ConnectionConfiguration(string pipeName, string socketPath)
    {
        PipeName = pipeName;
        SocketPath = socketPath;
    }
}