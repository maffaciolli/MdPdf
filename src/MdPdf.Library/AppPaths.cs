using System.IO.Abstractions;
using MdPdf.Library.Runtime;

namespace MdPdf.Library;

public sealed class AppPaths
{
    private readonly IFileSystem _fileSystem;
    private readonly ISystemEnvironment _environment;

    public AppPaths(IFileSystem fileSystem, ISystemEnvironment environment)
    {
        _fileSystem = fileSystem;
        _environment = environment;
    }

    public string GetConfigPath()
    {
        if (_environment.IsWindows())
        {
            var localApplicationData = _environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            );
            return GetWindowsConfigPath(localApplicationData);
        }

        if (_environment.IsLinux())
        {
            var homeDirectory =
                _environment.GetEnvironmentVariable("HOME")
                ?? _environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var xdgConfigHome = _environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            return GetLinuxConfigPath(homeDirectory, xdgConfigHome);
        }

        if (_environment.IsMacOS())
        {
            var homeDirectory =
                _environment.GetEnvironmentVariable("HOME")
                ?? _environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return GetMacConfigPath(homeDirectory);
        }

        throw new PlatformNotSupportedException("Unsupported operating system.");
    }

    public string GetAssetsPath()
    {
        if (_environment.IsWindows())
        {
            var localApplicationData = _environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            );
            return GetWindowsAssetsPath(localApplicationData);
        }

        if (_environment.IsLinux())
        {
            var homeDirectory =
                _environment.GetEnvironmentVariable("HOME")
                ?? _environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var xdgDataHome = _environment.GetEnvironmentVariable("XDG_DATA_HOME");
            return GetLinuxAssetsPath(homeDirectory, xdgDataHome);
        }

        if (_environment.IsMacOS())
        {
            var homeDirectory =
                _environment.GetEnvironmentVariable("HOME")
                ?? _environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return GetMacAssetsPath(homeDirectory);
        }

        throw new PlatformNotSupportedException("Unsupported operating system.");
    }

    private string GetWindowsConfigPath(string? localApplicationData)
    {
        if (string.IsNullOrWhiteSpace(localApplicationData))
            throw new InvalidOperationException("Local application data is unavailable.");

        return _fileSystem.Path.Combine(localApplicationData, "MdPdf", "MdPdf.config.json");
    }

    private string GetLinuxConfigPath(string homeDirectory, string? xdgConfigHome)
    {
        if (!string.IsNullOrWhiteSpace(xdgConfigHome))
            return NormalizeLinuxPath(xdgConfigHome, "mdpdf", "MdPdf.config.json");

        if (string.IsNullOrWhiteSpace(homeDirectory))
            throw new InvalidOperationException("Home directory is unavailable.");

        return NormalizeLinuxPath(homeDirectory, ".config", "mdpdf", "MdPdf.config.json");
    }

    private string GetMacConfigPath(string homeDirectory)
    {
        if (string.IsNullOrWhiteSpace(homeDirectory))
            throw new InvalidOperationException("Home directory is unavailable.");

        return NormalizeLinuxPath(
            homeDirectory,
            "Library",
            "Application Support",
            "MdPdf",
            "MdPdf.config.json"
        );
    }

    private string GetWindowsAssetsPath(string? localApplicationData)
    {
        if (string.IsNullOrWhiteSpace(localApplicationData))
            throw new InvalidOperationException("Local application data is unavailable.");

        return _fileSystem.Path.Combine(localApplicationData, "MdPdf", "Assets");
    }

    private string GetLinuxAssetsPath(string homeDirectory, string? xdgDataHome)
    {
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
            return NormalizeLinuxPath(xdgDataHome, "mdpdf", "Assets");

        if (string.IsNullOrWhiteSpace(homeDirectory))
            throw new InvalidOperationException("Home directory is unavailable.");

        return NormalizeLinuxPath(homeDirectory, ".local", "share", "mdpdf", "Assets");
    }

    private string GetMacAssetsPath(string homeDirectory)
    {
        if (string.IsNullOrWhiteSpace(homeDirectory))
            throw new InvalidOperationException("Home directory is unavailable.");

        return NormalizeLinuxPath(
            homeDirectory,
            "Library",
            "Application Support",
            "MdPdf",
            "Assets"
        );
    }

    private static string NormalizeLinuxPath(params string[] segments)
    {
        var normalizedSegments = new string[segments.Length];
        for (var i = 0; i < segments.Length; i++)
            normalizedSegments[i] = segments[i].TrimEnd('/', '\\');

        return string.Join("/", normalizedSegments);
    }
}
