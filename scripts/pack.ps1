<#
.SYNOPSIS
    Creates a clean, fresh LLMKit NuGet package (.nupkg and .snupkg) in the artifacts folder.

.DESCRIPTION
    1. Checks the .NET SDK, the project files and the git state
    2. Deletes old build output (bin, obj) and old packages (artifacts)
    3. Runs all tests in Release
    4. Packs src/LLMKit/LLMKit.csproj in Release (deterministic build)
    5. Verifies the package: version, freshly built DLL, documentation, symbols, README and icon

    The package is not published; the push command is shown at the end.
    On an error the script shows the details and waits for Enter, so the window does not close.
    Everything is also written to pack.log in the repository root (ignored by git).

.PARAMETER SkipTests
    Skips the tests. Not recommended for a release.

.PARAMETER AllowUncommitted
    Packs without asking when there are uncommitted changes.

.PARAMETER NoPause
    Does not wait for Enter at the end (for automated runs).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\scripts\pack.ps1
#>

[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$AllowUncommitted,
    [switch]$NoPause
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot  = Split-Path -Parent $PSScriptRoot
$Project   = Join-Path $RepoRoot 'src\LLMKit\LLMKit.csproj'
$Solution  = Join-Path $RepoRoot 'LLMKit.sln'
$Artifacts = Join-Path $RepoRoot 'artifacts'
$BuiltDll  = Join-Path $RepoRoot 'src\LLMKit\bin\Release\net8.0\LLMKit.dll'
$LogFile   = Join-Path $RepoRoot 'pack.log'
$StartTime = Get-Date
$script:CurrentStep = 'Start'

function Write-Step([string]$Text) {
    $script:CurrentStep = $Text
    Write-Host ''
    Write-Host "==> $Text" -ForegroundColor Cyan
}

# Runs a command line tool, shows all its output (errors in red) and stops when it fails
function Invoke-Native([string]$FilePath, [string[]]$Arguments) {
    Write-Host "    > $FilePath $($Arguments -join ' ')" -ForegroundColor DarkGray
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'   # stderr lines must not stop the script before the exit code is known
    try {
        & $FilePath @Arguments 2>&1 | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) { Write-Host $_ -ForegroundColor Red }
            else { Write-Host $_ }
        }
        $code = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
    if ($code -ne 0) {
        throw "'$FilePath $($Arguments -join ' ')' failed with exit code $code. The details are in the output above."
    }
}

# Runs git and returns its output, or $null when git fails (no repository, no upstream, ...)
function Get-GitOutput([string[]]$Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & git -C $RepoRoot @Arguments 2>$null
        if ($LASTEXITCODE -ne 0) { return $null }
        return $output
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Wait-BeforeClose([string]$Message) {
    if (-not $NoPause) {
        Write-Host ''
        Read-Host $Message | Out-Null
    }
}

try { Start-Transcript -Path $LogFile -Force | Out-Null } catch { Write-Host "Could not write the log file: $($_.Exception.Message)" -ForegroundColor Yellow }

$exitCode = 0
try {
    Write-Host 'LLMKit - clean NuGet package' -ForegroundColor White
    Write-Host "Repository: $RepoRoot"

    # ---------------------------------------------------------------------------------------------
    Write-Step 'Checking the .NET SDK and the project files'
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'The .NET SDK (dotnet) was not found. Install the .NET 8 SDK or later and open a new window.'
    }
    Invoke-Native 'dotnet' @('--version')
    foreach ($path in @($Project, $Solution)) {
        if (-not (Test-Path $path)) { throw "Required file not found: $path" }
    }

    [xml]$csproj = Get-Content $Project -Raw
    $versionNode = $csproj.SelectSingleNode('/Project/PropertyGroup/Version')
    if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
        throw "No <Version> found in $Project"
    }
    $version = $versionNode.InnerText.Trim()
    Write-Host "    Package version: $version"

    # ---------------------------------------------------------------------------------------------
    Write-Step 'Checking git'
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-Host '    git not found: the package will not record a commit for Source Link.' -ForegroundColor Yellow
    }
    else {
        $commit = Get-GitOutput @('rev-parse', '--short', 'HEAD')
        if ($null -eq $commit) {
            Write-Host '    Not a git repository: the package will not record a commit for Source Link.' -ForegroundColor Yellow
        }
        else {
            Write-Host "    Commit: $commit"
            $changes = @(Get-GitOutput @('status', '--porcelain', '--untracked-files=no'))
            $ahead = Get-GitOutput @('rev-list', '--count', '@{u}..HEAD')

            if ($changes.Count -gt 0) {
                Write-Host "    WARNING: $($changes.Count) uncommitted change(s) to tracked files." -ForegroundColor Yellow
                Write-Host "    The package records commit $commit, so its symbols (Source Link) point to code" -ForegroundColor Yellow
                Write-Host '    without these changes. Commit and push first for a release.' -ForegroundColor Yellow
                if (-not $AllowUncommitted) {
                    $answer = Read-Host '    Continue anyway? (y/N)'
                    if ($answer -notmatch '^(y|yes)$') {
                        throw 'Stopped by you: commit (and push) your changes first, then run this script again.'
                    }
                }
            }
            elseif ($null -ne $ahead -and [int]$ahead -gt 0) {
                Write-Host "    WARNING: $ahead commit(s) not pushed. Push them, so Source Link finds the code on GitHub." -ForegroundColor Yellow
            }
            else {
                Write-Host '    Working tree is clean.'
            }
        }
    }

    # ---------------------------------------------------------------------------------------------
    Write-Step 'Deleting old build output and packages'
    foreach ($dir in @('src\LLMKit\bin', 'src\LLMKit\obj', 'tests\LLMKit.Tests\bin', 'tests\LLMKit.Tests\obj', 'artifacts')) {
        $full = Join-Path $RepoRoot $dir
        if (Test-Path $full) {
            try { Remove-Item $full -Recurse -Force }
            catch { throw "Could not delete '$full'. Close Visual Studio or any program using these files, then try again. Details: $($_.Exception.Message)" }
            Write-Host "    deleted $dir"
        }
    }

    # ---------------------------------------------------------------------------------------------
    # The same build properties as the pack step, so the tested build is the packed build
    if ($SkipTests) {
        Write-Step 'Skipping the tests (-SkipTests)'
        Write-Host '    WARNING: the package is not tested.' -ForegroundColor Yellow
    }
    else {
        Write-Step 'Running the tests (Release)'
        Invoke-Native 'dotnet' @('test', $Solution, '-c', 'Release', '--nologo', '-p:ContinuousIntegrationBuild=true')
    }

    # ---------------------------------------------------------------------------------------------
    Write-Step 'Building and packing (Release, deterministic)'
    Invoke-Native 'dotnet' @('pack', $Project, '-c', 'Release', '-o', $Artifacts, '--nologo', '-p:ContinuousIntegrationBuild=true')

    # ---------------------------------------------------------------------------------------------
    Write-Step 'Verifying the package'
    $nupkg  = Join-Path $Artifacts "LLMKit.$version.nupkg"
    $snupkg = Join-Path $Artifacts "LLMKit.$version.snupkg"
    foreach ($file in @($nupkg, $snupkg)) {
        if (-not (Test-Path $file)) { throw "Expected package not found: $file" }
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $verifyDir = Join-Path ([System.IO.Path]::GetTempPath()) ('llmkit-verify-' + [guid]::NewGuid().ToString('N'))
    New-Item $verifyDir -ItemType Directory | Out-Null
    try {
        $zip = [System.IO.Compression.ZipFile]::OpenRead($nupkg)
        try {
            $entries = @($zip.Entries | ForEach-Object { $_.FullName })
            foreach ($required in @('LLMKit.nuspec', 'lib/net8.0/LLMKit.dll', 'lib/net8.0/LLMKit.xml', 'README.md', 'images/LLMKit.png')) {
                if ($entries -notcontains $required) { throw "The package is missing '$required'." }
                Write-Host "    contains $required"
            }

            $packedDll = Join-Path $verifyDir 'LLMKit.dll'
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry('lib/net8.0/LLMKit.dll'), $packedDll)

            $reader = New-Object System.IO.StreamReader($zip.GetEntry('LLMKit.nuspec').Open())
            try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        }
        finally {
            $zip.Dispose()
        }

        # Version in the package metadata and in the DLL
        $nuspecVersion = $nuspec.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='version']").InnerText
        if ($nuspecVersion -ne $version) { throw "The package version is '$nuspecVersion', expected '$version'." }
        Write-Host "    package version $nuspecVersion"

        $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($packedDll).Version
        $expectedAssembly = $version.Split('-')[0]
        if ($assemblyVersion.ToString(3) -ne $expectedAssembly) {
            throw "The DLL in the package has version $assemblyVersion, expected $expectedAssembly."
        }
        Write-Host "    DLL version $assemblyVersion"

        # The packed DLL must be the one built by this run (an old DLL was packed in 3.1.0 and 3.2.0)
        if (-not (Test-Path $BuiltDll)) { throw "The built DLL was not found: $BuiltDll" }
        if ((Get-Item $BuiltDll).LastWriteTime -lt $StartTime) {
            throw "The built DLL is older than this run: $BuiltDll"
        }
        if ((Get-FileHash $packedDll).Hash -ne (Get-FileHash $BuiltDll).Hash) {
            throw 'The DLL in the package is not the DLL built by this run.'
        }
        Write-Host '    DLL is freshly built by this run'

        $dependencies = @($nuspec.SelectNodes("//*[local-name()='dependency']") | ForEach-Object { "$($_.id) $($_.version)" })
        if ($dependencies.Count -eq 0) { Write-Host '    dependencies: none' }
        else { Write-Host "    dependencies: $($dependencies -join ', ')" }

        $symbols = [System.IO.Compression.ZipFile]::OpenRead($snupkg)
        try {
            if (-not ($symbols.Entries | Where-Object { $_.FullName -eq 'lib/net8.0/LLMKit.pdb' })) {
                throw "The symbol package is missing 'lib/net8.0/LLMKit.pdb'."
            }
        }
        finally {
            $symbols.Dispose()
        }
        Write-Host '    symbol package contains lib/net8.0/LLMKit.pdb'
    }
    finally {
        Remove-Item $verifyDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    # ---------------------------------------------------------------------------------------------
    Write-Host ''
    Write-Host '==================== SUCCESS ====================' -ForegroundColor Green
    foreach ($file in @($nupkg, $snupkg)) {
        Write-Host ("  {0}  ({1:N0} KB)" -f $file, ((Get-Item $file).Length / 1KB)) -ForegroundColor Green
    }
    Write-Host ''
    Write-Host 'To publish (the .snupkg is uploaded automatically with the .nupkg):'
    Write-Host "  dotnet nuget push `"$nupkg`" --source https://api.nuget.org/v3/index.json --api-key <your-key>"
    Write-Host ''
    Write-Host "Log: $LogFile"
    Wait-BeforeClose 'Done. Press Enter to close'
}
catch {
    $exitCode = 1
    $err = $_
    Write-Host ''
    Write-Host '==================== FAILED ====================' -ForegroundColor Red
    Write-Host "Step:   $script:CurrentStep" -ForegroundColor Red
    Write-Host "Error:  $($err.Exception.Message)" -ForegroundColor Red

    $inner = $err.Exception.InnerException
    while ($null -ne $inner) {
        Write-Host "Cause:  $($inner.Message)" -ForegroundColor Red
        $inner = $inner.InnerException
    }
    if ($null -ne $err.InvocationInfo -and $err.InvocationInfo.PositionMessage) {
        Write-Host "Where:  $($err.InvocationInfo.PositionMessage)" -ForegroundColor DarkRed
    }
    if ($err.ScriptStackTrace) {
        Write-Host 'Stack:' -ForegroundColor DarkRed
        Write-Host $err.ScriptStackTrace -ForegroundColor DarkRed
    }
    Write-Host ''
    Write-Host "Full log: $LogFile"
    Write-Host 'No package was published. Fix the error above and run the script again.'
    Wait-BeforeClose 'Press Enter to close'
}
finally {
    try { Stop-Transcript | Out-Null } catch { }
}

exit $exitCode
