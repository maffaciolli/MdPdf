namespace MdPdf.Console.Tests;

public class MarkdownToPdfConverterTests
{
    [Fact]
    public async Task Must_render_markdown_content_when_building_html()
    {
        // Arrange
        const string MARKDOWN = "**bold**";
        using var assetsDirectory = CreateAssetsDirectory();
        await WriteStylesheetAsync(
            assetsDirectory.Path,
            "github-markdown-light.min.css",
            ".markdown-body { }"
        );
        await WriteScriptAsync(
            assetsDirectory.Path,
            "mermaid.min.js",
            "window.mermaid = { initialize() {}, run: async () => {} };"
        );

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            assetsDirectory: assetsDirectory.Path
        );

        // Assert
        html.ShouldContain("<strong>bold</strong>");
        html.ShouldNotContain(MARKDOWN);
        html.ShouldContain("window.mermaid = { initialize() {}, run: async () => {} };");
        html.ShouldNotContain("import mermaid");
    }

    [Fact]
    public async Task Must_save_light_stylesheet_to_assets_directory_when_file_is_missing()
    {
        // Arrange
        const string MARKDOWN = "Hello";
        using var assetsDirectory = CreateAssetsDirectory();

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            assetsDirectory: assetsDirectory.Path,
            assetDownloader: async uri =>
            {
                if (uri.AbsoluteUri.Contains("mermaid.min.js"))
                    return "window.mermaid = { initialize() {}, run: async () => {} };";

                return ".markdown-body { background: #ffffff; }";
            }
        );

        // Assert
        var stylesheetPath = Path.Combine(assetsDirectory.Path, "github-markdown-light.min.css");

        File.Exists(stylesheetPath).ShouldBeTrue();
        File.ReadAllText(stylesheetPath).ShouldBe(".markdown-body { background: #ffffff; }");
        html.ShouldContain("<style>");
        html.ShouldContain(".markdown-body { background: #ffffff; }");
    }

    [Fact]
    public async Task Must_reuse_cached_stylesheet_when_asset_already_exists()
    {
        // Arrange
        const string MARKDOWN = "Hello";
        using var assetsDirectory = CreateAssetsDirectory();
        await WriteStylesheetAsync(
            assetsDirectory.Path,
            "github-markdown-light.min.css",
            ".markdown-body { background: #ffffff; }"
        );
        await WriteScriptAsync(
            assetsDirectory.Path,
            "mermaid.min.js",
            "window.mermaid = { initialize() {}, run: async () => {} };"
        );
        var stylesheetPath = Path.Combine(assetsDirectory.Path, "github-markdown-light.min.css");
        var downloaderCalled = false;

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            assetsDirectory: assetsDirectory.Path,
            assetDownloader: _ =>
            {
                downloaderCalled = true;
                return Task.FromException<string>(
                    new InvalidOperationException("Downloader should not be called")
                );
            }
        );

        // Assert
        downloaderCalled.ShouldBeFalse();
        File.ReadAllText(stylesheetPath).ShouldBe(".markdown-body { background: #ffffff; }");
        html.ShouldContain(".markdown-body { background: #ffffff; }");
    }

    [Fact]
    public async Task Must_save_dark_stylesheet_to_assets_directory_when_file_is_missing()
    {
        // Arrange
        const string MARKDOWN = "Hello";
        using var assetsDirectory = CreateAssetsDirectory();

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            darkMode: true,
            assetsDirectory: assetsDirectory.Path,
            assetDownloader: async uri =>
            {
                if (uri.AbsoluteUri.Contains("mermaid.min.js"))
                    return "window.mermaid = { initialize() {}, run: async () => {} };";

                return ".markdown-body { background: #0d1117; }";
            }
        );

        // Assert
        var stylesheetPath = Path.Combine(assetsDirectory.Path, "github-markdown-dark.min.css");

        File.Exists(stylesheetPath).ShouldBeTrue();
        File.ReadAllText(stylesheetPath).ShouldBe(".markdown-body { background: #0d1117; }");
        html.ShouldContain("<style>");
        html.ShouldContain(".markdown-body { background: #0d1117; }");
    }

    [Fact]
    public async Task Must_reuse_cached_mermaid_script_when_asset_already_exists()
    {
        // Arrange
        const string MARKDOWN = "```mermaid\nflowchart TD\nA-->B\n```";
        using var assetsDirectory = CreateAssetsDirectory();
        await WriteStylesheetAsync(
            assetsDirectory.Path,
            "github-markdown-light.min.css",
            ".markdown-body { }"
        );
        await WriteScriptAsync(
            assetsDirectory.Path,
            "mermaid.min.js",
            "window.mermaid = { initialize() {}, run: async () => {} };"
        );
        var scriptPath = Path.Combine(assetsDirectory.Path, "mermaid.min.js");
        var downloaderCalled = false;

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            assetsDirectory: assetsDirectory.Path,
            assetDownloader: _ =>
            {
                downloaderCalled = true;
                return Task.FromException<string>(
                    new InvalidOperationException("Downloader should not be called")
                );
            }
        );

        // Assert
        downloaderCalled.ShouldBeFalse();
        File.ReadAllText(scriptPath)
            .ShouldBe("window.mermaid = { initialize() {}, run: async () => {} };");
        html.ShouldContain("window.mermaid = { initialize() {}, run: async () => {} };");
    }

    [Fact]
    public async Task Must_build_light_theme_html_when_dark_mode_is_false()
    {
        // Arrange
        const string MARKDOWN = "Hello";
        using var assetsDirectory = CreateAssetsDirectory();
        await WriteStylesheetAsync(
            assetsDirectory.Path,
            "github-markdown-light.min.css",
            ".markdown-body { }"
        );
        await WriteScriptAsync(
            assetsDirectory.Path,
            "mermaid.min.js",
            "window.mermaid = { initialize() {}, run: async () => {} };"
        );

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            assetsDirectory: assetsDirectory.Path
        );

        // Assert
        html.ShouldContain("theme: 'default'");
        html.ShouldContain(MARKDOWN);
    }

    [Fact]
    public async Task Must_include_page_padding_styles_when_building_html()
    {
        // Arrange
        const string MARKDOWN = "Hello";
        using var assetsDirectory = CreateAssetsDirectory();
        await WriteStylesheetAsync(
            assetsDirectory.Path,
            "github-markdown-light.min.css",
            ".markdown-body { }"
        );
        await WriteScriptAsync(
            assetsDirectory.Path,
            "mermaid.min.js",
            "window.mermaid = { initialize() {}, run: async () => {} };"
        );

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            assetsDirectory: assetsDirectory.Path
        );

        // Assert
        html.ShouldContain("padding: 24px;");
        html.ShouldContain("box-sizing: border-box;");
        html.ShouldContain("body.markdown-body");
    }

    [Fact]
    public async Task Must_include_github_alert_background_styles_when_building_html()
    {
        // Arrange
        const string MARKDOWN = "> [!TIP]\n> Example";
        using var assetsDirectory = CreateAssetsDirectory();
        await WriteStylesheetAsync(
            assetsDirectory.Path,
            "github-markdown-light.min.css",
            ".markdown-body { }"
        );
        await WriteScriptAsync(
            assetsDirectory.Path,
            "mermaid.min.js",
            "window.mermaid = { initialize() {}, run: async () => {} };"
        );

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            assetsDirectory: assetsDirectory.Path
        );

        // Assert
        html.ShouldContain(".markdown-body .markdown-alert-tip");
        html.ShouldContain("background-color: rgba(31, 136, 61, 0.12);");
        html.ShouldContain(".markdown-body .markdown-alert-title");
    }

    [Fact]
    public async Task Must_escape_script_closing_tags_when_inlining_mermaid()
    {
        // Arrange
        using var assetsDirectory = CreateAssetsDirectory();
        const string MARKDOWN = "```mermaid\nflowchart TD\nA-->B\n```";

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            assetsDirectory: assetsDirectory.Path,
            assetDownloader: uri =>
            {
                if (uri.AbsoluteUri.Contains("github-markdown"))
                    return Task.FromResult(".markdown-body { }");

                return Task.FromResult("window.mermaid = { x: '</script>' };");
            }
        );

        // Assert
        html.ShouldContain("<\\/script>");
        html.ShouldNotContain("window.mermaid = { x: '</script>' };");
    }

    [Fact]
    public async Task Must_build_dark_theme_html_when_dark_mode_is_true()
    {
        // Arrange
        const string MARKDOWN = "Hello";
        using var assetsDirectory = CreateAssetsDirectory();
        await WriteStylesheetAsync(
            assetsDirectory.Path,
            "github-markdown-dark.min.css",
            ".markdown-body { }"
        );
        await WriteScriptAsync(
            assetsDirectory.Path,
            "mermaid.min.js",
            "window.mermaid = { initialize() {}, run: async () => {} };"
        );

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            darkMode: true,
            assetsDirectory: assetsDirectory.Path
        );

        // Assert
        html.ShouldContain("theme: 'dark'");
    }

    [Fact]
    public async Task Must_surface_download_errors_when_asset_download_fails()
    {
        // Arrange
        const string MARKDOWN = "Hello";
        using var assetsDirectory = CreateAssetsDirectory();

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            MarkdownToPdfConverter.BuildHtmlAsync(
                MARKDOWN,
                assetsDirectory: assetsDirectory.Path,
                assetDownloader: _ =>
                    Task.FromException<string>(new InvalidOperationException("download failed"))
            )
        );

        // Assert
        exception.Message.ShouldBe("download failed");
    }

    [Fact]
    public async Task Must_save_mermaid_script_to_assets_directory_when_file_is_missing()
    {
        // Arrange
        const string MARKDOWN = "```mermaid\nflowchart TD\nA-->B\n```";
        using var assetsDirectory = CreateAssetsDirectory();

        // Act
        var html = await MarkdownToPdfConverter.BuildHtmlAsync(
            MARKDOWN,
            assetsDirectory: assetsDirectory.Path,
            assetDownloader: async uri =>
            {
                if (uri.AbsoluteUri.Contains("mermaid.min.js"))
                    return "window.mermaid = { initialize() {}, run: async () => {} };";

                return ".markdown-body { }";
            }
        );

        // Assert
        var scriptPath = Path.Combine(assetsDirectory.Path, "mermaid.min.js");

        File.Exists(scriptPath).ShouldBeTrue();
        File.ReadAllText(scriptPath)
            .ShouldBe("window.mermaid = { initialize() {}, run: async () => {} };");
        html.ShouldContain("window.mermaid = { initialize() {}, run: async () => {} };");
    }

    private sealed class TempAssetsDirectory : IDisposable
    {
        public string Path { get; }

        public TempAssetsDirectory()
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

    private static TempAssetsDirectory CreateAssetsDirectory()
    {
        return new TempAssetsDirectory();
    }

    private static async Task WriteStylesheetAsync(
        string assetsDirectory,
        string fileName,
        string css
    )
    {
        await File.WriteAllTextAsync(Path.Combine(assetsDirectory, fileName), css);
    }

    private static async Task WriteScriptAsync(string assetsDirectory, string fileName, string js)
    {
        await File.WriteAllTextAsync(Path.Combine(assetsDirectory, fileName), js);
    }
}
