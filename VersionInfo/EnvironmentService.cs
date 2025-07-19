namespace VersionInfo;

public class EnvironmentService : IEnvironmentService
{
    public string GetMachineName()
    {
        return Environment.MachineName;
    }

    public string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}