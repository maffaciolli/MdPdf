using System.IO.Abstractions;
using System.Runtime.Versioning;
using MdPdf.Library.Runtime;

namespace MdPdf.Library;

public sealed class BrowserPathResolver
{
    private readonly IFileSystem _fileSystem;
    private readonly ISystemEnvironment _environment;
    private readonly ICommandRunner _commandRunner;
    private readonly IWindowsRegistryReader _windowsRegistryReader;

    public BrowserPathResolver(
        IFileSystem fileSystem,
        ISystemEnvironment environment,
        ICommandRunner commandRunner,
        IWindowsRegistryReader windowsRegistryReader
    )
    {
        _fileSystem = fileSystem;
        _environment = environment;
        _commandRunner = commandRunner;
        _windowsRegistryReader = windowsRegistryReader;
    }

    public string? ResolveBrowserPath(
        string? configuredBrowserPath = null,
        IEnumerable<string>? candidatePaths = null
    )
    {
        var explicitBrowserPath = GetConfiguredBrowserPath(configuredBrowserPath);
        if (explicitBrowserPath is not null)
        {
            if (_fileSystem.File.Exists(explicitBrowserPath))
                return explicitBrowserPath;

            throw new InvalidOperationException(
                $"Browser executable '{explicitBrowserPath}' was not found."
            );
        }

        candidatePaths ??= GetDefaultCandidatePaths();
        foreach (var candidatePath in candidatePaths)
        {
            if (_fileSystem.File.Exists(candidatePath))
                return candidatePath;
        }

        return null;
    }

    private static string? GetConfiguredBrowserPath(string? configuredBrowserPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredBrowserPath))
            return configuredBrowserPath;

        return null;
    }

    private IEnumerable<string> GetDefaultCandidatePaths()
    {
        if (_environment.IsWindows())
            return GetWindowsCandidatePaths();

        if (_environment.IsLinux())
            return GetLinuxCandidatePaths();

        if (_environment.IsMacOS())
            return GetMacCandidatePaths();

        return [];
    }

    [SupportedOSPlatform("windows")]
    private IEnumerable<string> GetWindowsCandidatePaths()
    {
        var defaultBrowserPath = TryGetWindowsDefaultBrowserExecutablePath();
        var programFiles = _environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = _environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = _environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        );

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(defaultBrowserPath))
            candidates.Add(defaultBrowserPath);

        candidates.AddRange([
            _fileSystem.Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
            _fileSystem.Path.Combine(
                programFilesX86,
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"
            ),
            _fileSystem.Path.Combine(
                programFiles,
                "Microsoft",
                "Edge",
                "Application",
                "msedge.exe"
            ),
            _fileSystem.Path.Combine(
                programFilesX86,
                "Microsoft",
                "Edge",
                "Application",
                "msedge.exe"
            ),
            _fileSystem.Path.Combine(localAppData, "Google", "Chrome", "Application", "chrome.exe"),
            _fileSystem.Path.Combine(
                localAppData,
                "Microsoft",
                "Edge",
                "Application",
                "msedge.exe"
            ),
            _fileSystem.Path.Combine(localAppData, "Chromium", "Application", "chrome.exe"),
        ]);

        return candidates;
    }

    [SupportedOSPlatform("linux")]
    private IEnumerable<string> GetLinuxCandidatePaths()
    {
        var defaultBrowserPath = TryGetLinuxDefaultBrowserExecutablePath();

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(defaultBrowserPath))
            candidates.Add(defaultBrowserPath);

        candidates.AddRange([
            "/usr/bin/google-chrome",
            "/usr/bin/google-chrome-stable",
            "/usr/bin/chromium",
            "/usr/bin/chromium-browser",
            "/snap/bin/chromium",
        ]);

        return candidates;
    }

    private IEnumerable<string> GetMacCandidatePaths()
    {
        return
        [
            "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
            "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
            "/Applications/Chromium.app/Contents/MacOS/Chromium",
        ];
    }

    internal string? ExtractExecutablePathFromCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        var trimmedCommand = command.Trim();
        if (trimmedCommand.StartsWith('"'))
        {
            var closingQuoteIndex = trimmedCommand.IndexOf('"', 1);
            if (closingQuoteIndex > 1)
                return _environment.ExpandEnvironmentVariables(
                    trimmedCommand[1..closingQuoteIndex]
                );
        }

        var executableMatch = System.Text.RegularExpressions.Regex.Match(
            trimmedCommand,
            @"^(?<path>.+?\.exe)(?:\s|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        if (executableMatch.Success)
            return _environment.ExpandEnvironmentVariables(
                executableMatch.Groups["path"].Value.Trim()
            );

        return null;
    }

    internal string? ExtractExecutablePathFromLinuxCommand(
        string? command,
        Func<string, bool>? fileExists = null
    )
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        fileExists ??= _fileSystem.File.Exists;

        var trimmedCommand = command.Trim();
        var firstToken = ReadFirstToken(trimmedCommand);
        if (string.IsNullOrWhiteSpace(firstToken))
            return null;

        var executablePath = _environment.ExpandEnvironmentVariables(firstToken);
        if (fileExists(executablePath))
            return executablePath;

        return TryResolveFromPath(executablePath, fileExists);
    }

    [SupportedOSPlatform("windows")]
    private string? TryGetWindowsDefaultBrowserExecutablePath()
    {
        if (!_environment.IsWindows())
            return null;

        foreach (var scheme in new[] { "http", "https" })
        {
            var progId = TryGetWindowsDefaultBrowserProgId(scheme);
            if (string.IsNullOrWhiteSpace(progId))
                continue;

            var command = _windowsRegistryReader.GetClassesRootValue(
                $@"{progId}\shell\open\command",
                null
            );

            var executablePath = ExtractExecutablePathFromCommand(command);
            if (!string.IsNullOrWhiteSpace(executablePath))
                return executablePath;
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private string? TryGetWindowsDefaultBrowserProgId(string scheme)
    {
        var userChoiceKeyPath =
            $@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\{scheme}\UserChoice";
        var progId = _windowsRegistryReader.GetCurrentUserValue(userChoiceKeyPath, "ProgId");
        if (!string.IsNullOrWhiteSpace(progId))
            return progId;

        return _windowsRegistryReader.GetClassesRootValue(scheme, null);
    }

    [SupportedOSPlatform("linux")]
    private string? TryGetLinuxDefaultBrowserExecutablePath()
    {
        if (!_environment.IsLinux())
            return null;

        var desktopEntry = TryGetLinuxDefaultBrowserDesktopEntry();
        if (string.IsNullOrWhiteSpace(desktopEntry))
            return null;

        var desktopEntryPath = FindLinuxDesktopEntryPath(desktopEntry);
        if (desktopEntryPath is null)
            return null;

        var execLine = ReadLinuxDesktopExecLine(desktopEntryPath);
        return ExtractExecutablePathFromLinuxCommand(execLine);
    }

    [SupportedOSPlatform("linux")]
    private string? TryGetLinuxDefaultBrowserDesktopEntry()
    {
        foreach (
            var command in new[]
            {
                new[] { "xdg-mime", "query", "default", "x-scheme-handler/http" },
                ["xdg-settings", "get", "default-web-browser"],
            }
        )
        {
            var output = _commandRunner.Run(command[0], command[1..]);
            if (!string.IsNullOrWhiteSpace(output))
                return output.Trim();
        }

        return null;
    }

    [SupportedOSPlatform("linux")]
    private string? FindLinuxDesktopEntryPath(string desktopEntry)
    {
        var candidateDirectories = new[]
        {
            _fileSystem.Path.Combine(
                _environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share",
                "applications"
            ),
            "/usr/local/share/applications",
            "/usr/share/applications",
        };

        foreach (var directory in candidateDirectories)
        {
            var candidatePath = _fileSystem.Path.Combine(directory, desktopEntry);
            if (_fileSystem.File.Exists(candidatePath))
                return candidatePath;
        }

        return null;
    }

    [SupportedOSPlatform("linux")]
    private string? ReadLinuxDesktopExecLine(string desktopEntryPath)
    {
        foreach (var line in _fileSystem.File.ReadLines(desktopEntryPath))
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith("Exec=", StringComparison.Ordinal))
                continue;

            return trimmedLine["Exec=".Length..];
        }

        return null;
    }

    private static string? ReadFirstToken(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        var trimmedCommand = command.Trim();
        if (trimmedCommand.StartsWith('"'))
        {
            var closingQuoteIndex = trimmedCommand.IndexOf('"', 1);
            if (closingQuoteIndex > 1)
                return trimmedCommand[1..closingQuoteIndex];
        }

        var spaceIndex = trimmedCommand.IndexOfAny([' ', '\t']);
        return spaceIndex < 0 ? trimmedCommand : trimmedCommand[..spaceIndex];
    }

    private string? TryResolveFromPath(string command, Func<string, bool> fileExists)
    {
        if (
            command.Contains(_fileSystem.Path.DirectorySeparatorChar)
            || command.Contains(_fileSystem.Path.AltDirectorySeparatorChar)
        )
            return null;

        var pathVariable = _environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
            return null;

        foreach (
            var pathEntry in pathVariable.Split(
                _fileSystem.Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries
            )
        )
        {
            var candidatePath = _fileSystem.Path.Combine(pathEntry.Trim(), command);
            if (fileExists(candidatePath))
                return candidatePath;
        }

        return null;
    }
}
