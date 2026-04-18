using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace MdPdf.Library;

public static class BrowserPathResolver
{
    public static string? ResolveBrowserPath(
        string? configuredBrowserPath = null,
        IEnumerable<string>? candidatePaths = null,
        Func<string, bool>? fileExists = null
    )
    {
        fileExists ??= File.Exists;

        var explicitBrowserPath = GetConfiguredBrowserPath(configuredBrowserPath);
        if (explicitBrowserPath is not null)
        {
            if (fileExists(explicitBrowserPath))
                return explicitBrowserPath;

            throw new InvalidOperationException(
                $"Browser executable '{explicitBrowserPath}' was not found."
            );
        }

        candidatePaths ??= GetDefaultCandidatePaths();
        foreach (var candidatePath in candidatePaths)
        {
            if (fileExists(candidatePath))
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

    private static IEnumerable<string> GetDefaultCandidatePaths()
    {
        if (OperatingSystem.IsWindows())
            return GetWindowsCandidatePaths();

        if (OperatingSystem.IsLinux())
            return GetLinuxCandidatePaths();

        if (OperatingSystem.IsMacOS())
            return GetMacCandidatePaths();

        return [];
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> GetWindowsCandidatePaths()
    {
        var defaultBrowserPath = TryGetWindowsDefaultBrowserExecutablePath();
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        );

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(defaultBrowserPath))
            candidates.Add(defaultBrowserPath);

        candidates.AddRange([
            Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(localAppData, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(localAppData, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(localAppData, "Chromium", "Application", "chrome.exe"),
        ]);

        return candidates;
    }

    [SupportedOSPlatform("linux")]
    private static IEnumerable<string> GetLinuxCandidatePaths()
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

    private static IEnumerable<string> GetMacCandidatePaths()
    {
        return
        [
            "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
            "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
            "/Applications/Chromium.app/Contents/MacOS/Chromium",
        ];
    }

    internal static string? ExtractExecutablePathFromCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        var trimmedCommand = command.Trim();
        if (trimmedCommand.StartsWith('"'))
        {
            var closingQuoteIndex = trimmedCommand.IndexOf('"', 1);
            if (closingQuoteIndex > 1)
                return Environment.ExpandEnvironmentVariables(trimmedCommand[1..closingQuoteIndex]);
        }

        var executableMatch = System.Text.RegularExpressions.Regex.Match(
            trimmedCommand,
            @"^(?<path>.+?\.exe)(?:\s|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        if (executableMatch.Success)
            return Environment.ExpandEnvironmentVariables(
                executableMatch.Groups["path"].Value.Trim()
            );

        return null;
    }

    internal static string? ExtractExecutablePathFromLinuxCommand(
        string? command,
        Func<string, bool>? fileExists = null
    )
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        fileExists ??= File.Exists;

        var trimmedCommand = command.Trim();
        var firstToken = ReadFirstToken(trimmedCommand);
        if (string.IsNullOrWhiteSpace(firstToken))
            return null;

        var executablePath = Environment.ExpandEnvironmentVariables(firstToken);
        if (fileExists(executablePath))
            return executablePath;

        return TryResolveFromPath(executablePath, fileExists);
    }

    [SupportedOSPlatform("windows")]
    private static string? TryGetWindowsDefaultBrowserExecutablePath()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        foreach (var scheme in new[] { "http", "https" })
        {
            var progId = TryGetWindowsDefaultBrowserProgId(scheme);
            if (string.IsNullOrWhiteSpace(progId))
                continue;

            var command = Registry
                .GetValue($@"HKEY_CLASSES_ROOT\{progId}\shell\open\command", null, null)
                ?.ToString();

            var executablePath = ExtractExecutablePathFromCommand(command);
            if (!string.IsNullOrWhiteSpace(executablePath))
                return executablePath;
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static string? TryGetWindowsDefaultBrowserProgId(string scheme)
    {
        var userChoiceKeyPath =
            $@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\{scheme}\UserChoice";
        using var userChoiceKey = Registry.CurrentUser.OpenSubKey(userChoiceKeyPath);
        var progId = userChoiceKey?.GetValue("ProgId") as string;
        if (!string.IsNullOrWhiteSpace(progId))
            return progId;

        using var schemeKey = Registry.ClassesRoot.OpenSubKey(scheme);
        return schemeKey?.GetValue(null) as string;
    }

    [SupportedOSPlatform("linux")]
    private static string? TryGetLinuxDefaultBrowserExecutablePath()
    {
        if (!OperatingSystem.IsLinux())
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
    private static string? TryGetLinuxDefaultBrowserDesktopEntry()
    {
        foreach (
            var command in new[]
            {
                new[] { "xdg-mime", "query", "default", "x-scheme-handler/http" },
                ["xdg-settings", "get", "default-web-browser"],
            }
        )
        {
            var output = RunCommand(command[0], command[1..]);
            if (!string.IsNullOrWhiteSpace(output))
                return output.Trim();
        }

        return null;
    }

    [SupportedOSPlatform("linux")]
    private static string? FindLinuxDesktopEntryPath(string desktopEntry)
    {
        var candidateDirectories = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share",
                "applications"
            ),
            "/usr/local/share/applications",
            "/usr/share/applications",
        };

        foreach (var directory in candidateDirectories)
        {
            var candidatePath = Path.Combine(directory, desktopEntry);
            if (File.Exists(candidatePath))
                return candidatePath;
        }

        return null;
    }

    [SupportedOSPlatform("linux")]
    private static string? ReadLinuxDesktopExecLine(string desktopEntryPath)
    {
        foreach (var line in File.ReadLines(desktopEntryPath))
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

    private static string? TryResolveFromPath(string command, Func<string, bool> fileExists)
    {
        if (
            command.Contains(Path.DirectorySeparatorChar)
            || command.Contains(Path.AltDirectorySeparatorChar)
        )
            return null;

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
            return null;

        foreach (
            var pathEntry in pathVariable.Split(
                Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries
            )
        )
        {
            var candidatePath = Path.Combine(pathEntry.Trim(), command);
            if (fileExists(candidatePath))
                return candidatePath;
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    private static string? RunCommand(string fileName, IEnumerable<string> arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);
            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);
            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
