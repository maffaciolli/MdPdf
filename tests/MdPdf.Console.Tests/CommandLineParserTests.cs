namespace MdPdf.Console.Tests;

public class CommandLineParserTests
{
    private readonly CommandLineParser _parser = new();

    [Fact]
    public void Must_return_null_when_no_arguments_are_passed()
    {
        // Arrange
        string[] args = [];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Must_default_to_dark_when_no_theme_flag_is_present()
    {
        // Arrange
        string[] args = ["input.md", "out.pdf"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OutputPath.ShouldBe("out.pdf");
        parsed.DarkMode.ShouldBeTrue();
    }

    [Fact]
    public void Must_use_light_theme_when_light_flag_is_present()
    {
        // Arrange
        string[] args = ["input.md", "--light"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OutputPath.ShouldBeNull();
        parsed.DarkMode.ShouldBeFalse();
    }

    [Fact]
    public void Must_use_dark_theme_when_dark_flag_is_present()
    {
        // Arrange
        string[] args = ["input.md", "--dark"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OutputPath.ShouldBeNull();
        parsed.DarkMode.ShouldBeTrue();
    }

    [Fact]
    public void Must_use_the_last_theme_flag_when_both_flags_are_passed()
    {
        // Arrange
        string[] args = ["input.md", "--light", "--dark"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OutputPath.ShouldBeNull();
        parsed.DarkMode.ShouldBeTrue();
    }

    [Fact]
    public void Must_ignore_flag_casing_when_parsing_theme_flags()
    {
        // Arrange
        string[] args = ["input.md", "--LIGHT"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OutputPath.ShouldBeNull();
        parsed.DarkMode.ShouldBeFalse();
    }

    [Fact]
    public void Must_return_null_when_only_theme_flags_are_passed()
    {
        // Arrange
        string[] args = ["--dark", "--light"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Must_parse_browser_path_when_browser_path_flag_is_present()
    {
        // Arrange
        string[] args = ["input.md", "--browser-path", @"C:\Chrome\chrome.exe"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OutputPath.ShouldBeNull();
        parsed.DarkMode.ShouldBeTrue();
        parsed.BrowserPath.ShouldBe(@"C:\Chrome\chrome.exe");
    }

    [Fact]
    public void Must_parse_browser_path_when_browser_path_flag_casing_differs()
    {
        // Arrange
        string[] args = ["input.md", "--BROWSER-PATH", @"C:\Chrome\chrome.exe"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.BrowserPath.ShouldBe(@"C:\Chrome\chrome.exe");
    }

    [Fact]
    public void Must_return_null_when_browser_path_flag_has_no_value()
    {
        // Arrange
        string[] args = ["input.md", "--browser-path"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Must_use_the_last_browser_path_when_browser_path_flag_is_passed_multiple_times()
    {
        // Arrange
        string[] args =
        [
            "input.md",
            "--browser-path",
            @"C:\Chrome\chrome.exe",
            "--browser-path",
            @"C:\Edge\msedge.exe",
        ];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.BrowserPath.ShouldBe(@"C:\Edge\msedge.exe");
    }

    [Fact]
    public void Must_treat_browser_path_value_as_literal_when_it_starts_with_double_dash()
    {
        // Arrange
        string[] args = ["input.md", "--browser-path", "--custom-browser"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.BrowserPath.ShouldBe("--custom-browser");
    }

    [Fact]
    public void Must_parse_save_browser_path_flag_when_present()
    {
        // Arrange
        string[] args = ["input.md", "--save-browser-path"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.SaveBrowserPath.ShouldBeTrue();
    }

    [Fact]
    public void Must_parse_open_flag_when_present()
    {
        // Arrange
        string[] args = ["input.md", "--open"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OpenPdf.ShouldBeTrue();
    }

    [Fact]
    public void Must_return_null_when_save_browser_path_flag_is_passed_without_input()
    {
        // Arrange
        string[] args = ["--save-browser-path"];

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        result.ShouldBeNull();
    }
}
