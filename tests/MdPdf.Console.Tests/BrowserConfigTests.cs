using System.Text.Json;

namespace MdPdf.Console.Tests;

public class BrowserConfigTests
{
    [Fact]
    public async Task Must_return_null_when_config_file_does_not_exist()
    {
        // Arrange
        using var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory.Path, "MdPdf.config.json");

        // Act
        var browserPath = await BrowserConfig.LoadBrowserPathAsync(configPath);

        // Assert
        browserPath.ShouldBeNull();
    }

    [Fact]
    public async Task Must_return_null_when_config_file_is_empty()
    {
        // Arrange
        using var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory.Path, "MdPdf.config.json");
        await File.WriteAllTextAsync(configPath, string.Empty);

        // Act
        var browserPath = await BrowserConfig.LoadBrowserPathAsync(configPath);

        // Assert
        browserPath.ShouldBeNull();
    }

    [Fact]
    public async Task Must_load_browser_path_when_json_config_file_contains_browser_path()
    {
        // Arrange
        using var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory.Path, "MdPdf.config.json");
        await File.WriteAllTextAsync(configPath, """{"browserPath":"C:\\Browsers\\chrome.exe"}""");

        // Act
        var browserPath = await BrowserConfig.LoadBrowserPathAsync(configPath);

        // Assert
        browserPath.ShouldBe(@"C:\Browsers\chrome.exe");
    }

    [Fact]
    public async Task Must_return_null_when_browser_path_is_missing_from_json()
    {
        // Arrange
        using var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory.Path, "MdPdf.config.json");
        await File.WriteAllTextAsync(configPath, """{"theme":"dark"}""");

        // Act
        var browserPath = await BrowserConfig.LoadBrowserPathAsync(configPath);

        // Assert
        browserPath.ShouldBeNull();
    }

    [Fact]
    public async Task Must_throw_when_config_file_contains_invalid_json()
    {
        // Arrange
        using var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory.Path, "MdPdf.config.json");
        await File.WriteAllTextAsync(configPath, "{ invalid json");

        // Act
        var exception = await Should.ThrowAsync<JsonException>(() =>
            BrowserConfig.LoadBrowserPathAsync(configPath)
        );

        // Assert
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task Must_save_browser_path_as_json_when_persisting_config()
    {
        // Arrange
        using var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory.Path, "MdPdf.config.json");

        // Act
        await BrowserConfig.SaveBrowserPathAsync(configPath, @"C:\Browsers\chrome.exe");

        // Assert
        var content = await File.ReadAllTextAsync(configPath);
        content.ShouldContain("\"browserPath\": \"C:\\\\Browsers\\\\chrome.exe\"");
    }

    [Fact]
    public async Task Must_create_config_directory_when_saving_to_nested_path()
    {
        // Arrange
        using var tempDirectory = CreateTempDirectory();
        var configPath = Path.Combine(tempDirectory.Path, "config", "MdPdf.config.json");

        // Act
        await BrowserConfig.SaveBrowserPathAsync(configPath, @"C:\Browsers\chrome.exe");

        // Assert
        File.Exists(configPath).ShouldBeTrue();
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                System.IO.Path.GetRandomFileName()
            );
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }

    private static TempDirectory CreateTempDirectory()
    {
        return new TempDirectory();
    }
}
