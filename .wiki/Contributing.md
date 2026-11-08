# Contributing to LLMKit

Thank you for your interest in contributing to LLMKit! This document provides guidelines and instructions for contributing to the project.

## Getting Started

1. Fork the repository
2. Clone your fork
3. Create a new branch for your feature/fix
4. Make your changes
5. Submit a pull request

## Development Setup

1. Ensure you have .NET 8.0 SDK installed
2. Clone the repository
3. Open the solution in your preferred IDE
4. Restore NuGet packages
5. Build the solution

## Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and small
- Follow SOLID principles
- Write unit tests for new features

## Pull Request Process

1. Create a new branch for your feature/fix
2. Make your changes
3. Add/update tests
4. Update documentation
5. Submit a pull request

### Pull Request Guidelines

- Use a clear and descriptive title
- Provide a detailed description of changes
- Reference any related issues
- Include test coverage
- Update documentation as needed

## Testing

- Write unit tests for new features
- Ensure all tests pass
- Maintain or improve test coverage
- Test edge cases and error conditions

## Documentation

- Update README.md if needed
- Add XML documentation for public APIs
- Update examples if applicable
- Document breaking changes

## Adding New Providers

1. Create a new provider class deriving from `BaseLLMProvider` (it provides the shared HTTP client, retries and error handling)
2. Add provider-specific response models as `internal` classes (they are not part of the public API)
3. Override `CreateRequestData` and `GenerateTextAsync`. Map `LLMRequest` (including `ReasoningEffort`) and fill `LLMResponse` (including `Reasoning` if the provider returns it)
4. Add unit tests
5. Update documentation (README, wiki and the release notes in `LLMKit.csproj`)

Example:
```csharp
public class NewProvider : BaseLLMProvider
{
    public NewProvider(string apiKey, string model, Uri? endpoint = null)
        : base(apiKey, model, endpoint ?? new Uri("https://api.example.com/v1/chat"))
    {
    }

    public override async Task<LLMResponse> GenerateTextAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

## Bug Reports

When reporting bugs, please include:
- Description of the issue
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment details
- Code example if applicable

## Feature Requests

When requesting features, please include:
- Description of the feature
- Use cases
- Potential implementation approach
- Any relevant examples

## Code Review Process

1. Pull requests are reviewed by maintainers
2. Address any feedback
3. Make requested changes
4. Request re-review if needed
5. Merge when approved

## Release Process

1. Update version numbers (`<Version>` in `src/LLMKit/LLMKit.csproj`)
2. Update changelog
3. Create release notes (`<PackageReleaseNotes>` in the same file)
4. Tag the release
5. Commit and push, then create the NuGet package with the script. It deletes old build output, runs
   the tests, packs in Release and verifies the package (version, freshly built DLL, documentation,
   symbols). On an error it shows the details and keeps the window open:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\pack.ps1
   ```
   Or by hand (`dotnet pack` builds the latest code first, so the package always contains the
   current DLL):
   ```bash
   dotnet pack src/LLMKit/LLMKit.csproj -c Release -o artifacts -p:ContinuousIntegrationBuild=true
   ```
   This writes `LLMKit.<version>.nupkg` and the symbol package `LLMKit.<version>.snupkg` to
   `artifacts/` (ignored by git). Building the projects does not create packages.
6. Publish both. `dotnet nuget push` uploads the `.snupkg` next to the `.nupkg` automatically:
   ```bash
   dotnet nuget push artifacts/LLMKit.<version>.nupkg --source https://api.nuget.org/v3/index.json --api-key <key>
   ```

## Questions?

If you have questions, please:
1. Check the documentation
2. Search existing issues
3. Open a new issue if needed

## License

By contributing, you agree that your contributions will be licensed under the project's MIT License. 