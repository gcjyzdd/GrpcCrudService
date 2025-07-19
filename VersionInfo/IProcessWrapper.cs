namespace VersionInfo;

public interface IProcessWrapper
{
    ProcessExecutionResult ExecuteProcess(string fileName, string arguments);
}

public record ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError);