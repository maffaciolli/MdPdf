using System.IO.Abstractions;
using System.Text.Json;

namespace MdPdf.Library;

public sealed class BrowserConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly IFileSystem _fileSystem;

    public BrowserConfig(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<string?> LoadBrowserPathAsync(string configPath)
    {
        if (!_fileSystem.File.Exists(configPath))
            return null;

        var content = await _fileSystem.File.ReadAllTextAsync(configPath);
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var config = JsonSerializer.Deserialize<BrowserConfigFile>(content, JsonOptions);

        return string.IsNullOrWhiteSpace(config?.BrowserPath) ? null : config.BrowserPath;
    }

    public async Task SaveBrowserPathAsync(string configPath, string browserPath)
    {
        var directory = _fileSystem.Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory))
            _fileSystem.Directory.CreateDirectory(directory);

        var content = JsonSerializer.Serialize(new BrowserConfigFile(browserPath), JsonOptions);

        await _fileSystem.File.WriteAllTextAsync(configPath, content);
    }
}

public sealed record BrowserConfigFile(string BrowserPath);
