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
        if (!File.Exists(configPath))
            return null;

        var content = await File.ReadAllTextAsync(configPath);
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var config = JsonSerializer.Deserialize<BrowserConfigFile>(content, JsonOptions);

        return string.IsNullOrWhiteSpace(config?.BrowserPath) ? null : config.BrowserPath;
    }

    public static async Task SaveBrowserPathAsync(string configPath, string browserPath)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var content = JsonSerializer.Serialize(new BrowserConfigFile(browserPath), JsonOptions);

        await File.WriteAllTextAsync(configPath, content);
    }
}

public sealed record BrowserConfigFile(string BrowserPath);
