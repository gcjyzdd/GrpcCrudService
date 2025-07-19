namespace VersionInfo;

public interface IEnvironmentService
{
    string GetMachineName();
    string? GetEnvironmentVariable(string name);
}