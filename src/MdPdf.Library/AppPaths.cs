namespace MdPdf.Library;

public static class AppPaths
{
    public static string GetConfigPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return GetConfigPath(
                "Windows",
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            );
        }

        if (OperatingSystem.IsLinux())
        {
            var homeDirectory =
                Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return GetConfigPath(
                "Linux",
                null,
                homeDirectory,
                Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
            );
        }

        if (OperatingSystem.IsMacOS())
        {
            var homeDirectory =
                Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return GetConfigPath("macOS", null, homeDirectory);
        }

        throw new PlatformNotSupportedException("Unsupported operating system.");
    }

    public static string GetConfigPath(
        string operatingSystem,
        string? localApplicationData,
        string homeDirectory,
        string? xdgConfigHome = null
    )
    {
        if (string.Equals(operatingSystem, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(localApplicationData))
                throw new InvalidOperationException("Local application data is unavailable.");

            return Path.Combine(localApplicationData, "MdPdf", "MdPdf.config.json");
        }

        if (string.Equals(operatingSystem, "Linux", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(xdgConfigHome))
                return NormalizeLinuxConfigPath(xdgConfigHome, "mdpdf", "MdPdf.config.json");

            if (string.IsNullOrWhiteSpace(homeDirectory))
                throw new InvalidOperationException("Home directory is unavailable.");

            return NormalizeLinuxConfigPath(homeDirectory, ".config", "mdpdf", "MdPdf.config.json");
        }

        if (
            string.Equals(operatingSystem, "macOS", StringComparison.OrdinalIgnoreCase)
            || string.Equals(operatingSystem, "MacOS", StringComparison.OrdinalIgnoreCase)
        )
        {
            if (string.IsNullOrWhiteSpace(homeDirectory))
                throw new InvalidOperationException("Home directory is unavailable.");

            return NormalizeLinuxConfigPath(
                homeDirectory,
                "Library",
                "Application Support",
                "MdPdf",
                "MdPdf.config.json"
            );
        }

        throw new PlatformNotSupportedException("Unsupported operating system.");
    }

    private static string NormalizeLinuxConfigPath(params string[] segments)
    {
        var normalizedSegments = new string[segments.Length];
        for (var i = 0; i < segments.Length; i++)
            normalizedSegments[i] = segments[i].TrimEnd('/', '\\');

        return string.Join("/", normalizedSegments);
    }
}
