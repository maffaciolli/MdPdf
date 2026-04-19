namespace MdPdf.Console.Tests;

public class BrowserPathResolverTests
{
    [Fact]
    public void Must_return_explicit_browser_path_when_configured_path_exists()
    {
        // Arrange
        using var browserFile = CreateBrowserFile();
        var resolver = CreateResolver(new FileSystem());

        // Act
        var resolvedPath = resolver.ResolveBrowserPath(browserFile.Path);

        // Assert
        resolvedPath.ShouldBe(browserFile.Path);
    }

    [Fact]
    public void Must_return_first_available_browser_path_when_no_override_is_present()
    {
        // Arrange
        using var firstBrowserFile = CreateBrowserFile();
        using var secondBrowserFile = CreateBrowserFile();
        var candidatePaths = new[] { firstBrowserFile.Path, secondBrowserFile.Path };
        var resolver = CreateResolver(new FileSystem());

        // Act
        var resolvedPath = resolver.ResolveBrowserPath(candidatePaths: candidatePaths);

        // Assert
        resolvedPath.ShouldBe(firstBrowserFile.Path);
    }

    [Fact]
    public void Must_return_null_when_no_browser_paths_are_available()
    {
        // Arrange
        var candidatePaths = new[] { "missing-1.exe", "missing-2.exe" };
        var resolver = CreateResolver(new FileSystem());

        // Act
        var resolvedPath = resolver.ResolveBrowserPath(candidatePaths: candidatePaths);

        // Assert
        resolvedPath.ShouldBeNull();
    }

    [Fact]
    public void Must_throw_when_configured_browser_path_does_not_exist()
    {
        // Arrange
        const string browserPath = @"C:\missing\chrome.exe";
        var resolver = CreateResolver(new FileSystem());

        // Act
        var exception = Should.Throw<InvalidOperationException>(() =>
            resolver.ResolveBrowserPath(browserPath)
        );

        // Assert
        exception.Message.ShouldContain(browserPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Must_return_null_when_browser_command_is_empty(string? command)
    {
        // Arrange
        var resolver = CreateResolver(new FileSystem());

        // Act
        var executablePath = resolver.ExtractExecutablePathFromCommand(command);

        // Assert
        executablePath.ShouldBeNull();
    }

    [Theory]
    [InlineData(
        "\"C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe\" -- \"%1\"",
        @"C:\Program Files\Google\Chrome\Application\chrome.exe"
    )]
    [InlineData(
        "\"C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe\" --single-argument %1",
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    )]
    [InlineData(@"C:\Chromium\chrome.exe %1", @"C:\Chromium\chrome.exe")]
    public void Must_extract_executable_path_from_browser_command(
        string command,
        string expectedPath
    )
    {
        // Arrange
        var resolver = CreateResolver(new FileSystem());

        // Act
        var executablePath = resolver.ExtractExecutablePathFromCommand(command);

        // Assert
        executablePath.ShouldBe(expectedPath);
    }

    [Theory]
    [InlineData("%LOCALAPPDATA%\\Google\\Chrome\\Application\\chrome.exe -- \"%1\"")]
    [InlineData("\"%ProgramFiles%\\Google\\Chrome\\Application\\chrome.exe\" -- \"%1\"")]
    public void Must_expand_environment_variables_when_extracting_windows_executable_path(
        string command
    )
    {
        // Arrange
        var resolver = CreateResolver(new FileSystem());

        // Act
        var executablePath = resolver.ExtractExecutablePathFromCommand(command);

        // Assert
        executablePath.ShouldNotBeNull();
        executablePath.ShouldNotContain("%");
        executablePath.ShouldContain("chrome.exe");
    }

    [Theory]
    [InlineData("/usr/bin/google-chrome-stable %U", "/usr/bin/google-chrome-stable")]
    [InlineData("/snap/bin/chromium --profile-directory=Default %u", "/snap/bin/chromium")]
    [InlineData(
        "\"/opt/google/chrome/google-chrome\" --app=%U",
        "/opt/google/chrome/google-chrome"
    )]
    public void Must_extract_executable_path_from_linux_command(string command, string expectedPath)
    {
        // Arrange
        var resolver = CreateResolver(new FileSystem());

        // Act
        var executablePath = resolver.ExtractExecutablePathFromLinuxCommand(command, _ => true);

        // Assert
        executablePath.ShouldBe(expectedPath);
    }

    [Fact]
    public void Must_return_null_when_linux_command_cannot_be_resolved()
    {
        // Arrange
        var resolver = CreateResolver(new FileSystem());

        // Act
        var executablePath = resolver.ExtractExecutablePathFromLinuxCommand(
            "missing-browser %U",
            _ => false
        );

        // Assert
        executablePath.ShouldBeNull();
    }

    [Fact]
    public void Must_resolve_linux_command_using_path_when_command_is_on_path()
    {
        // Arrange
        using var browserFile = CreateBrowserFile("google-chrome-stable");
        var environment = new TestSystemEnvironment { Linux = true };
        environment.SetEnvironmentVariable(
            "PATH",
            Path.GetDirectoryName(browserFile.Path) ?? string.Empty
        );
        var resolver = CreateResolver(new FileSystem(), environment);

        // Act
        var executablePath = resolver.ExtractExecutablePathFromLinuxCommand(
            "google-chrome-stable %U",
            File.Exists
        );

        // Assert
        executablePath.ShouldBe(browserFile.Path);
    }

    [Fact]
    public void Must_ignore_whitespace_only_configured_browser_path()
    {
        // Arrange
        using var browserFile = CreateBrowserFile();
        var candidatePaths = new[] { browserFile.Path };
        var resolver = CreateResolver(new FileSystem());

        // Act
        var resolvedPath = resolver.ResolveBrowserPath("   ", candidatePaths);

        // Assert
        resolvedPath.ShouldBe(browserFile.Path);
    }

    private static BrowserPathResolver CreateResolver(
        IFileSystem fileSystem,
        TestSystemEnvironment? environment = null
    )
    {
        environment ??= new TestSystemEnvironment();
        return new BrowserPathResolver(
            fileSystem,
            environment,
            new TestCommandRunner(),
            new TestWindowsRegistryReader()
        );
    }

    private sealed class TemporaryBrowserFile : IDisposable
    {
        public string Path { get; }

        public TemporaryBrowserFile(string fileName = "browser.exe")
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
            File.WriteAllText(Path, string.Empty);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }

    private static TemporaryBrowserFile CreateBrowserFile()
    {
        return new TemporaryBrowserFile();
    }

    private static TemporaryBrowserFile CreateBrowserFile(string fileName)
    {
        return new TemporaryBrowserFile(fileName);
    }
}
