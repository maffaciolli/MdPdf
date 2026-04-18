namespace MdPdf.Console.Tests;

public class AppPathsTests
{
    [Fact]
    public void Must_resolve_windows_config_path_under_local_application_data()
    {
        // Act
        var path = AppPaths.GetConfigPath(
            "Windows",
            @"C:\Users\mathe\AppData\Local",
            @"C:\Users\mathe",
            null
        );

        // Assert
        path.ShouldBe(@"C:\Users\mathe\AppData\Local\MdPdf\MdPdf.config.json");
    }

    [Fact]
    public void Must_throw_when_windows_local_application_data_is_unavailable()
    {
        // Act
        var exception = Should.Throw<InvalidOperationException>(() =>
            AppPaths.GetConfigPath("Windows", null, @"C:\Users\mathe")
        );

        // Assert
        exception.ShouldNotBeNull();
    }

    [Fact]
    public void Must_resolve_linux_config_path_under_xdg_config_home()
    {
        // Act
        var path = AppPaths.GetConfigPath(
            "Linux",
            null,
            "/home/mathe",
            "/home/mathe/.config-custom"
        );

        // Assert
        path.ShouldBe("/home/mathe/.config-custom/mdpdf/MdPdf.config.json");
    }

    [Fact]
    public void Must_resolve_linux_config_path_under_home_config_when_xdg_config_home_is_missing()
    {
        // Act
        var path = AppPaths.GetConfigPath("Linux", null, "/home/mathe");

        // Assert
        path.ShouldBe("/home/mathe/.config/mdpdf/MdPdf.config.json");
    }

    [Fact]
    public void Must_resolve_macos_config_path_under_application_support()
    {
        // Act
        var path = AppPaths.GetConfigPath("macOS", null, "/Users/mathe", null);

        // Assert
        path.ShouldBe("/Users/mathe/Library/Application Support/MdPdf/MdPdf.config.json");
    }

    [Fact]
    public void Must_resolve_windows_assets_path_under_local_application_data()
    {
        // Act
        var path = AppPaths.GetAssetsPath(
            "Windows",
            @"C:\Users\mathe\AppData\Local",
            @"C:\Users\mathe"
        );

        // Assert
        path.ShouldBe(@"C:\Users\mathe\AppData\Local\MdPdf\Assets");
    }

    [Fact]
    public void Must_resolve_linux_assets_path_under_xdg_data_home()
    {
        // Act
        var path = AppPaths.GetAssetsPath(
            "Linux",
            null,
            "/home/mathe",
            xdgDataHome: "/home/mathe/.local/share-custom"
        );

        // Assert
        path.ShouldBe("/home/mathe/.local/share-custom/mdpdf/Assets");
    }

    [Fact]
    public void Must_resolve_linux_assets_path_under_home_local_share_when_xdg_data_home_is_missing()
    {
        // Act
        var path = AppPaths.GetAssetsPath("Linux", null, "/home/mathe");

        // Assert
        path.ShouldBe("/home/mathe/.local/share/mdpdf/Assets");
    }

    [Fact]
    public void Must_resolve_macos_assets_path_under_application_support()
    {
        // Act
        var path = AppPaths.GetAssetsPath("macOS", null, "/Users/mathe");

        // Assert
        path.ShouldBe("/Users/mathe/Library/Application Support/MdPdf/Assets");
    }
}
