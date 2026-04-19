using MdPdf.Library.Runtime;

namespace MdPdf.Console.Tests;

internal sealed class TestSystemEnvironment : ISystemEnvironment
{
    private readonly Dictionary<string, string?> _environmentVariables = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<Environment.SpecialFolder, string> _folderPaths = new();

    public bool Windows { get; init; }

    public bool Linux { get; init; }

    public bool MacOS { get; init; }

    public void SetEnvironmentVariable(string name, string? value)
    {
        _environmentVariables[name] = value;
    }

    public void SetFolderPath(Environment.SpecialFolder folder, string path)
    {
        _folderPaths[folder] = path;
    }

    public bool IsWindows()
    {
        return Windows;
    }

    public bool IsLinux()
    {
        return Linux;
    }

    public bool IsMacOS()
    {
        return MacOS;
    }

    public string? GetEnvironmentVariable(string name)
    {
        if (_environmentVariables.TryGetValue(name, out var value))
            return value;

        return null;
    }

    public string GetFolderPath(Environment.SpecialFolder folder)
    {
        if (_folderPaths.TryGetValue(folder, out var value))
            return value;

        return string.Empty;
    }

    public string ExpandEnvironmentVariables(string value)
    {
        return Environment.ExpandEnvironmentVariables(value);
    }
}

internal sealed class TestCommandRunner : ICommandRunner
{
    public Func<string, IEnumerable<string>, string?>? RunHandler { get; set; }

    public string? Run(string fileName, IEnumerable<string> arguments)
    {
        return RunHandler?.Invoke(fileName, arguments);
    }
}

internal sealed class TestWindowsRegistryReader : IWindowsRegistryReader
{
    private readonly Dictionary<(string SubKeyPath, string? ValueName), string?> _values = new();

    public void SetCurrentUserValue(string subKeyPath, string valueName, string? value)
    {
        _values[(subKeyPath, valueName)] = value;
    }

    public void SetClassesRootValue(string subKeyPath, string? valueName, string? value)
    {
        _values[(subKeyPath, valueName)] = value;
    }

    public string? GetCurrentUserValue(string subKeyPath, string valueName)
    {
        _values.TryGetValue((subKeyPath, valueName), out var value);
        return value;
    }

    public string? GetClassesRootValue(string subKeyPath, string? valueName)
    {
        _values.TryGetValue((subKeyPath, valueName), out var value);
        return value;
    }
}

internal sealed class DelegatingAssetDownloader : IAssetDownloader
{
    private readonly Func<Uri, Task<string>> _download;

    public DelegatingAssetDownloader(Func<Uri, Task<string>> download)
    {
        _download = download;
    }

    public Task<string> DownloadAsync(Uri assetUri)
    {
        return _download(assetUri);
    }
}
