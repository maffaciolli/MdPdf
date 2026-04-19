using System.IO.Abstractions;

namespace MdPdf.Library;

public static class MarkdownInputResolver
{
    public static async Task<ResolvedMarkdownInput> ResolveAsync(
        IFileSystem fileSystem,
        string input,
        string? outputPath
    )
    {
        var inputFilePath = await TryGetInputFilePathAsync(fileSystem, input);
        if (inputFilePath is not null)
        {
            var markdownContent = await fileSystem.File.ReadAllTextAsync(inputFilePath);
            var resolvedOutputPath =
                outputPath
                ?? fileSystem.Path.ChangeExtension(
                    fileSystem.Path.GetFullPath(inputFilePath),
                    ".pdf"
                );

            return new ResolvedMarkdownInput(markdownContent, resolvedOutputPath, inputFilePath);
        }

        return new ResolvedMarkdownInput(
            input,
            outputPath
                ?? fileSystem.Path.Combine(
                    fileSystem.Directory.GetCurrentDirectory(),
                    "output.pdf"
                ),
            null
        );
    }

    private static async Task<string?> TryGetInputFilePathAsync(
        IFileSystem fileSystem,
        string input
    )
    {
        try
        {
            await using var _ = fileSystem.FileStream.New(
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
