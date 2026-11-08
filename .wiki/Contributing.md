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

## Continuous Integration

GitHub Actions runs `.github/workflows/ci.yml` on every pull request to `master` and every push
(merge) to `master`. It builds the solution in Release (warnings fail the build), runs the tests,
packs LLMKit and verifies the package. The `.nupkg` and `.snupkg` can be downloaded from the run
(artifact `packages`). CI never publishes.

## Release Process

1. Update the version (`<Version>` in `src/LLMKit/LLMKit.csproj`) and the release notes
   (`<PackageReleaseNotes>` in the same file; start with `Version <version>:`)
2. Merge the pull request into `master` after CI passed
3. Tag the merge commit on `master` and push the tag. The tag must be `v` + the version:
   ```bash
   git switch master
   git pull
   git tag v<version>
   git push origin v<version>
   ```
4. `.github/workflows/release.yml` runs CI again, stops if the tag does not match `<Version>`, and
   then waits for an approval in the `nuget` environment (GitHub > Actions > the run > Review
   deployments)
5. After the approval it publishes the `.nupkg` and `.snupkg` to NuGet and creates the GitHub
   release with the release notes of this version and both files

### One-time setup

- **NuGet Trusted Publishing** (no API key stored in GitHub): on nuget.org > your account >
  Trusted Publishing, add a policy with owner `MohammedJayyab`, repository `LLMKit`, workflow
  file `release.yml` and environment `nuget`
- **GitHub secret** `NUGET_USER`: your nuget.org user name (not the e-mail address), in
  Settings > Secrets and variables > Actions
- **GitHub environment** `nuget`: in Settings > Environments, add yourself as a required
  reviewer, and under deployment branches and tags allow only tags matching `v*.*.*`
- **Branch protection** for `master`: in Settings > Rules (or Branches), require a pull request
  and the status check `Build, test and pack` before merging

### Creating a package locally

To create and check a package on your machine without publishing it, run the script. It deletes
old build output, runs the tests, packs in Release and verifies the package (version, freshly
built DLL, documentation, symbols). On an error it shows the details and keeps the window open:
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

If the release workflow cannot be used, publish by hand (`dotnet nuget push` uploads the
`.snupkg` next to the `.nupkg` automatically):
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