using MdPdf.Library;
using System.IO.Abstractions;

var fileSystem = new FileSystem();
var parsedArguments = CommandLineParser.ParseArguments(args);
if (parsedArguments is null)
{
    Console.WriteLine(
        "Usage: mdpdf <markdown-file-or-string> [output-path] [--dark|--light] [--browser-path <path>] [--save-browser-path]"
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
var savedBrowserPath = await BrowserConfig.LoadBrowserPathAsync(fileSystem, configPath);
var configuredBrowserPath = browserPath ?? savedBrowserPath;
var resolvedInput = await MarkdownInputResolver.ResolveAsync(fileSystem, input, arguments.OutputPath);
markdownContent = resolvedInput.MarkdownContent;
outputPath = resolvedInput.OutputPath;

Console.WriteLine(
    $"Reading from: {(resolvedInput.InputFilePath is not null ? resolvedInput.InputFilePath : "Raw String")}"
);
Console.WriteLine($"Theme: {(darkMode ? "Dark" : "Light")}");
Console.WriteLine("Rendering PDF...");

var resolvedBrowserPath = BrowserPathResolver.ResolveBrowserPath(configuredBrowserPath);
if (saveBrowserPath)
{
    if (resolvedBrowserPath is null)
        throw new InvalidOperationException(
            "No browser executable could be resolved, so there is nothing to save."
        );

    await BrowserConfig.SaveBrowserPathAsync(fileSystem, configPath, resolvedBrowserPath);
    Console.WriteLine($"Saved browser path to: {configPath}");
}

await MarkdownToPdfConverter.RenderToSinglePageAsync(
    markdownContent,
    outputPath,
    darkMode,
    browserPath: resolvedBrowserPath,
    fileSystem: fileSystem
);
Console.WriteLine($"Done! Saved to: {outputPath}");
