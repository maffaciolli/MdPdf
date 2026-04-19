using System.Text.Json;

namespace MdPdf.Console.Tests;

public class BrowserConfigTests
{
    [Fact]
    public async Task Must_return_null_when_config_file_does_not_exist()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var configPath = @"C:\config\MdPdf.config.json";

        // Act
        var browserPath = await BrowserConfig.LoadBrowserPathAsync(fileSystem, configPath);

        // Assert
        browserPath.ShouldBeNull();
    }

    [Fact]
    public async Task Must_return_null_when_config_file_is_empty()
    {
        // Arrange
        var configPath = @"C:\config\MdPdf.config.json";
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [configPath] = new(string.Empty) }
        );

        // Act
        var browserPath = await BrowserConfig.LoadBrowserPathAsync(fileSystem, configPath);

        // Assert
        browserPath.ShouldBeNull();
    }

    [Fact]
    public async Task Must_load_browser_path_when_json_config_file_contains_browser_path()
    {
        // Arrange
        var configPath = @"C:\config\MdPdf.config.json";
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                [configPath] = new("""{"browserPath":"C:\\Browsers\\chrome.exe"}"""),
            }
        );

        // Act
        var browserPath = await BrowserConfig.LoadBrowserPathAsync(fileSystem, configPath);

        // Assert
        browserPath.ShouldBe(@"C:\Browsers\chrome.exe");
    }

    [Fact]
    public async Task Must_return_null_when_browser_path_is_missing_from_json()
    {
        // Arrange
        var configPath = @"C:\config\MdPdf.config.json";
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [configPath] = new("""{"theme":"dark"}""") }
        );

        // Act
        var browserPath = await BrowserConfig.LoadBrowserPathAsync(fileSystem, configPath);

        // Assert
        browserPath.ShouldBeNull();
    }

    [Fact]
    public async Task Must_throw_when_config_file_contains_invalid_json()
    {
        // Arrange
        var configPath = @"C:\config\MdPdf.config.json";
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [configPath] = new("{ invalid json") }
        );

        // Act
        var exception = await Should.ThrowAsync<JsonException>(() =>
            BrowserConfig.LoadBrowserPathAsync(fileSystem, configPath)
        );

        // Assert
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task Must_save_browser_path_as_json_when_persisting_config()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var configPath = @"C:\config\MdPdf.config.json";

        // Act
        await BrowserConfig.SaveBrowserPathAsync(fileSystem, configPath, @"C:\Browsers\chrome.exe");

        // Assert
        var content = await fileSystem.File.ReadAllTextAsync(configPath);
        content.ShouldContain("\"browserPath\": \"C:\\\\Browsers\\\\chrome.exe\"");
    }

    [Fact]
    public async Task Must_create_config_directory_when_saving_to_nested_path()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var configPath = @"C:\users\me\.config\MdPdf.config.json";

        // Act
        await BrowserConfig.SaveBrowserPathAsync(fileSystem, configPath, @"C:\Browsers\chrome.exe");

        // Assert
        fileSystem.File.Exists(configPath).ShouldBeTrue();
    }
}
