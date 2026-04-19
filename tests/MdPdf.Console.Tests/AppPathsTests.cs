namespace MdPdf.Console.Tests;

public class AppPathsTests
{
    [Fact]
    public void Must_resolve_windows_config_path_under_local_application_data()
    {
        // Arrange
        var environment = new TestSystemEnvironment { Windows = true };
        environment.SetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            @"C:\Users\mathe\AppData\Local"
        );
        var appPaths = new AppPaths(new MockFileSystem(), environment);

        // Act
        var path = appPaths.GetConfigPath();

        // Assert
        path.ShouldBe(@"C:\Users\mathe\AppData\Local\MdPdf\MdPdf.config.json");
    }

    [Fact]
    public void Must_throw_when_windows_local_application_data_is_unavailable()
    {
        // Arrange
        var environment = new TestSystemEnvironment { Windows = true };
        var appPaths = new AppPaths(new MockFileSystem(), environment);

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => appPaths.GetConfigPath());

        // Assert
        exception.ShouldNotBeNull();
    }

    [Fact]
    public void Must_resolve_linux_config_path_under_xdg_config_home()
    {
        // Arrange
        var environment = new TestSystemEnvironment { Linux = true };
        environment.SetEnvironmentVariable("HOME", "/home/mathe");
        environment.SetEnvironmentVariable("XDG_CONFIG_HOME", "/home/mathe/.config-custom");
        var appPaths = new AppPaths(new MockFileSystem(), environment);

        // Act
        var path = appPaths.GetConfigPath();

        // Assert
        path.ShouldBe("/home/mathe/.config-custom/mdpdf/MdPdf.config.json");
    }

    [Fact]
    public void Must_resolve_linux_config_path_under_home_config_when_xdg_config_home_is_missing()
    {
        // Arrange
        var environment = new TestSystemEnvironment { Linux = true };
        environment.SetEnvironmentVariable("HOME", "/home/mathe");
        var appPaths = new AppPaths(new MockFileSystem(), environment);

        // Act
        var path = appPaths.GetConfigPath();

        // Assert
        path.ShouldBe("/home/mathe/.config/mdpdf/MdPdf.config.json");
    }

    [Fact]
    public void Must_resolve_macos_config_path_under_application_support()
    {
        // Arrange
        var environment = new TestSystemEnvironment { MacOS = true };
        environment.SetEnvironmentVariable("HOME", "/Users/mathe");
        var appPaths = new AppPaths(new MockFileSystem(), environment);

        // Act
        var path = appPaths.GetConfigPath();

        // Assert
        path.ShouldBe("/Users/mathe/Library/Application Support/MdPdf/MdPdf.config.json");
    }

    [Fact]
    public void Must_resolve_windows_assets_path_under_local_application_data()
    {
        // Arrange
        var environment = new TestSystemEnvironment { Windows = true };
        environment.SetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            @"C:\Users\mathe\AppData\Local"
        );
        var appPaths = new AppPaths(new MockFileSystem(), environment);

        // Act
        var path = appPaths.GetAssetsPath();

        // Assert
        path.ShouldBe(@"C:\Users\mathe\AppData\Local\MdPdf\Assets");
    }

    [Fact]
    public void Must_resolve_linux_assets_path_under_xdg_data_home()
    {
        // Arrange
        var environment = new TestSystemEnvironment { Linux = true };
        environment.SetEnvironmentVariable("HOME", "/home/mathe");
        environment.SetEnvironmentVariable("XDG_DATA_HOME", "/home/mathe/.local/share-custom");
        var appPaths = new AppPaths(new MockFileSystem(), environment);

        // Act
        var path = appPaths.GetAssetsPath();

        // Assert
        path.ShouldBe("/home/mathe/.local/share-custom/mdpdf/Assets");
    }

    [Fact]
    public void Must_resolve_linux_assets_path_under_home_local_share_when_xdg_data_home_is_missing()
    {
        // Arrange
        var environment = new TestSystemEnvironment { Linux = true };
        environment.SetEnvironmentVariable("HOME", "/home/mathe");
        var appPaths = new AppPaths(new MockFileSystem(), environment);

        // Act
        var path = appPaths.GetAssetsPath();

        // Assert
        path.ShouldBe("/home/mathe/.local/share/mdpdf/Assets");
    }

    [Fact]
    public void Must_resolve_macos_assets_path_under_application_support()
    {
        // Arrange
        var environment = new TestSystemEnvironment { MacOS = true };
        environment.SetEnvironmentVariable("HOME", "/Users/mathe");
        var appPaths = new AppPaths(new MockFileSystem(), environment);

        // Act
        var path = appPaths.GetAssetsPath();

        // Assert
        path.ShouldBe("/Users/mathe/Library/Application Support/MdPdf/Assets");
    }
}
