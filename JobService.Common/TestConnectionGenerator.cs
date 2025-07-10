namespace JobService.Common;

public static class TestConnectionGenerator
{
    public static ConnectionConfiguration GenerateRandomPaths()
    {
        var random = Path.GetRandomFileName().Replace(".", "");
        return new ConnectionConfiguration
        {
            PipeName = $"JobServicePipe_{random}",
            SocketPath = Path.Combine(Path.GetTempPath(), $"jobservice_{random}.sock")
        };
    }
}