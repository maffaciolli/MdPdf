namespace MdPdf.Console.Tests;

public class MarkdownInputResolverTests
{
    [Fact]
    public async Task Must_read_markdown_from_file_when_input_path_exists()
    {
        // Arrange
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData> { [@"C:\docs\input.md"] = new("# Title") },
            @"C:\work"
        );

        // Act
        var result = await MarkdownInputResolver.ResolveAsync(
            fileSystem,
            @"C:\docs\input.md",
            outputPath: null
        );

        // Assert
        result.InputFilePath.ShouldBe(@"C:\docs\input.md");
        result.MarkdownContent.ShouldBe("# Title");
        result.OutputPath.ShouldBe(@"C:\docs\input.pdf");
    }

    [Fact]
    public async Task Must_treat_input_as_raw_markdown_when_file_cannot_be_opened()
    {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), @"C:\work");

        // Act
        var result = await MarkdownInputResolver.ResolveAsync(
            fileSystem,
            "# Inline",
            outputPath: null
        );

        // Assert
        result.InputFilePath.ShouldBeNull();
        result.MarkdownContent.ShouldBe("# Inline");
        result.OutputPath.ShouldBe(@"C:\work\output.pdf");
    }
}
