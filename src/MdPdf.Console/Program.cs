using MdPdf.Library;

var parsedArguments = CommandLineParser.ParseArguments(args);
if (parsedArguments is null)
{
    Console.WriteLine(
        "Usage: mark-to-pdf <markdown-file-or-string> [output-path] [--dark|--light] [--browser-path <path>] [--save-browser-path]"
    );
    return;
}

var arguments = parsedArguments.Value;
string input = arguments.Input;
string markdownContent;
string outputPath;
bool darkMode = arguments.DarkMode;
string? browserPath = arguments.BrowserPath;
var saveBrowserPath = arguments.SaveBrowserPath;
var configPath = AppPaths.GetConfigPath();
var savedBrowserPath = await BrowserConfig.LoadBrowserPathAsync(configPath);
var configuredBrowserPath = browserPath ?? savedBrowserPath;
var inputFilePath = await TryGetInputFilePathAsync(input);

if (inputFilePath is not null)
{
    markdownContent = await File.ReadAllTextAsync(inputFilePath);
    outputPath =
        arguments.OutputPath ?? Path.ChangeExtension(Path.GetFullPath(inputFilePath), ".pdf");
}
else
{
    markdownContent = input;
    outputPath =
        arguments.OutputPath ?? Path.Combine(Directory.GetCurrentDirectory(), "output.pdf");
}

Console.WriteLine($"Reading from: {(inputFilePath is not null ? inputFilePath : "Raw String")}");
Console.WriteLine($"Theme: {(darkMode ? "Dark" : "Light")}");
Console.WriteLine("Rendering PDF...");

var resolvedBrowserPath = BrowserPathResolver.ResolveBrowserPath(configuredBrowserPath);
if (saveBrowserPath)
{
    if (resolvedBrowserPath is null)
        throw new InvalidOperationException(
            "No browser executable could be resolved, so there is nothing to save."
        );

    await BrowserConfig.SaveBrowserPathAsync(configPath, resolvedBrowserPath);
    Console.WriteLine($"Saved browser path to: {configPath}");
}

await MarkdownToPdfConverter.RenderToSinglePageAsync(
    markdownContent,
    outputPath,
    darkMode,
    browserPath: resolvedBrowserPath
);

Console.WriteLine($"Done! Saved to: {outputPath}");

static async Task<string?> TryGetInputFilePathAsync(string input)
{
    try
    {
        await using var _ = new FileStream(
            input,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true
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
