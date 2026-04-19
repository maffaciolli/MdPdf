using System.IO.Abstractions;

namespace MdPdf.Library;

public sealed class MarkdownInputResolver
{
    private readonly IFileSystem _fileSystem;

    public MarkdownInputResolver(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<ResolvedMarkdownInput> ResolveAsync(string input, string? outputPath)
    {
        var inputFilePath = await TryGetInputFilePathAsync(input);
        if (inputFilePath is not null)
        {
            var markdownContent = await _fileSystem.File.ReadAllTextAsync(inputFilePath);
            var resolvedOutputPath =
                outputPath
                ?? _fileSystem.Path.ChangeExtension(
                    _fileSystem.Path.GetFullPath(inputFilePath),
                    ".pdf"
                );

            return new ResolvedMarkdownInput(markdownContent, resolvedOutputPath, inputFilePath);
        }

        return new ResolvedMarkdownInput(
            input,
            outputPath
                ?? _fileSystem.Path.Combine(
                    _fileSystem.Directory.GetCurrentDirectory(),
                    "output.pdf"
                ),
            null
        );
    }

    private async Task<string?> TryGetInputFilePathAsync(string input)
    {
        try
        {
            await using var _ = _fileSystem.FileStream.New(
                input,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                options: FileOptions.Asynchronous
            );

            return input;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}

public readonly record struct ResolvedMarkdownInput(
    string MarkdownContent,
    string OutputPath,
    string? InputFilePath
);
