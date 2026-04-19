using System.IO.Abstractions;
using System.Text.Json;

namespace MdPdf.Library;

public static class BrowserConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static async Task<string?> LoadBrowserPathAsync(string configPath)
    {
        return await LoadBrowserPathAsync(new FileSystem(), configPath);
    }

    public static async Task<string?> LoadBrowserPathAsync(
        IFileSystem fileSystem,
        string configPath
    )
    {
        if (!fileSystem.File.Exists(configPath))
            return null;

        var content = await fileSystem.File.ReadAllTextAsync(configPath);
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var config = JsonSerializer.Deserialize<BrowserConfigFile>(content, JsonOptions);

        return string.IsNullOrWhiteSpace(config?.BrowserPath) ? null : config.BrowserPath;
    }

    public static async Task SaveBrowserPathAsync(string configPath, string browserPath)
    {
        await SaveBrowserPathAsync(new FileSystem(), configPath, browserPath);
    }

    public static async Task SaveBrowserPathAsync(
        IFileSystem fileSystem,
        string configPath,
        string browserPath
    )
    {
        var directory = fileSystem.Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory))
            fileSystem.Directory.CreateDirectory(directory);

        var content = JsonSerializer.Serialize(new BrowserConfigFile(browserPath), JsonOptions);

        await fileSystem.File.WriteAllTextAsync(configPath, content);
    }
}

public sealed record BrowserConfigFile(string BrowserPath);
