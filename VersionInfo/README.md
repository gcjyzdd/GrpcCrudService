# VersionInfo

A .NET 8 console application that generates version strings based on environment context, git information, and CI/CD build parameters.

## Features

- **Multi-CI Support**: Jenkins, GitLab CI, GitHub Actions, Azure DevOps
- **Smart Version Formatting**: Different formats based on environment and git state
- **Comprehensive Logging**: Structured logging with Serilog (console + file)
- **AOT Compatible**: Optimized for Ahead-of-Time compilation
- **Fully Tested**: 44 unit tests with comprehensive coverage

## Version Format Rules

| Context | Output Format | Example |
|---------|---------------|---------|
| Developer machine | `{version}-developerbuild` | `1.2.3-developerbuild` |
| CI without git tag | `{version}a-{commit}-{build}` | `1.2.3a-abc1234-123` |
| CI with RC tag | `{version}a-RC{n}` | `1.2.3a-RC1` |
| CI with release tag | `{version}` | `1.2.3` |

## Usage

```bash
# Basic usage
dotnet run -- "1.2.3"

# Example outputs
1.2.3-developerbuild     # On developer machine
1.2.3a-abc1234-456      # On CI without git tag
1.2.3a-RC1              # On CI with git tag "1.2.3_RC1"
1.2.3                   # On CI with git tag "1.2.3"
```

## CI Environment Detection

The application automatically detects CI environments:

- **Jenkins**: `JenkinsAgent` hostname pattern
- **GitLab CI**: `GITLAB_CI` environment variable
- **GitHub Actions**: `GITHUB_ACTIONS` environment variable  
- **Azure DevOps**: `TF_BUILD` environment variable

## Build & Test

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run with coverage (requires C# Dev Kit)
# Coverage excludes Program.cs via .runsettings

# Run application
dotnet run -- "1.0.0"
```

## Architecture

```
VersionInfo/
├── Program.cs                    # Entry point with CI detection
├── VersionInfoApp.cs            # Main application logic
├── VersionInfoAppFactory.cs     # Factory for DI configuration
├── VersionInfoConfiguration.cs  # CI-specific configurations
├── Services/
│   ├── CommandExecutor.cs       # Process execution wrapper
│   ├── EnvironmentService.cs    # Environment variable access
│   ├── GitService.cs           # Git command operations
│   └── VersionFormatter.cs     # Version string formatting
├── Abstractions/
│   ├── IProcessWrapper.cs       # Process abstraction
│   └── ProcessWrapper.cs       # Process implementation
└── VersionInfo.Test/           # Comprehensive test suite
```

## Dependencies

- **.NET 8.0**: Target framework
- **Serilog**: Structured logging with file/console sinks
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **NUnit**: Testing framework (test project)
- **Moq**: Mocking framework (test project)

## Configuration

Each CI environment uses specific build number variables:

| CI Platform | Build Variable |
|--------------|----------------|
| Jenkins | `BUILD_NUMBER` |
| GitLab CI | `CI_PIPELINE_ID` |
| GitHub Actions | `GITHUB_RUN_NUMBER` |
| Azure DevOps | `BUILD_BUILDNUMBER` |

## Logging

- **Console**: Information level and above (no timestamps)
- **File**: Trace level and above (`logs/versioninfo-{date}.log`)
- **Structured**: JSON-formatted file logs for analysis

## Testing

44 comprehensive unit tests covering:

- ✅ Version formatting logic (8 tests)
- ✅ Git operations (7 tests) 
- ✅ Process execution (6 tests)
- ✅ Environment handling (5 tests)
- ✅ Application flow (6 tests)
- ✅ Configuration management (6 tests)
- ✅ Command execution (6 tests)

Run tests: `dotnet test`