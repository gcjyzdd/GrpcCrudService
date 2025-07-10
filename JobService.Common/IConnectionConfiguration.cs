namespace JobService.Common;

public interface IConnectionConfiguration
{
    string GetConnectionIdentifier();
    bool IsWindows { get; }
    string PipeName { get; }
    string SocketPath { get; }
}