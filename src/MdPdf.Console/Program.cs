using System.Diagnostics;
using System.IO.Abstractions;
using MdPdf.Library;
using MdPdf.Library.Runtime;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var fileSystem = new FileSystem();
        var environment = new SystemEnvironment();
        var commandRunner = new CommandRunner();
        var windowsRegistryReader = new WindowsRegistryReader();
        var assetDownloader = new HttpAssetDownloader();
        var appPaths = new AppPaths(fileSystem, environment);
        var commandLineParser = new CommandLineParser();
        var browserConfig = new BrowserConfig(fileSystem);
        var markdownInputResolver = new MarkdownInputResolver(fileSystem);
        var browserPathResolver = new BrowserPathResolver(
            fileSystem,
            environment,
            commandRunner,
            windowsRegistryReader
        );
        var markdownToPdfConverter = new MarkdownToPdfConverter(
            fileSystem,
            appPaths,
            assetDownloader
        );

        var parsedArguments = commandLineParser.ParseArguments(args);
        if (parsedArguments is null)
        {
            Console.WriteLine(
                "Usage: mdpdf <markdown-file-or-string> [output-path] [--dark|--light] [--browser-path <path>] [--save-browser-path] [--open]"
            );
            return;
        }

        var arguments = parsedArguments.Value;
        var input = arguments.Input;
        var darkMode = arguments.DarkMode;
        var browserPath = arguments.BrowserPath;
        var saveBrowserPath = arguments.SaveBrowserPath;
        var openPdf = arguments.OpenPdf;
        var configPath = appPaths.GetConfigPath();
        var savedBrowserPath = await browserConfig.LoadBrowserPathAsync(configPath);
        var configuredBrowserPath = browserPath ?? savedBrowserPath;
        var resolvedInput = await markdownInputResolver.ResolveAsync(input, arguments.OutputPath);

        Console.WriteLine(
            $"Reading from: {(resolvedInput.InputFilePath is not null ? resolvedInput.InputFilePath : "Raw String")}"
        );
        Console.WriteLine($"Theme: {(darkMode ? "Dark" : "Light")}");
        Console.WriteLine("Rendering PDF...");

        var resolvedBrowserPath = browserPathResolver.ResolveBrowserPath(configuredBrowserPath);
        if (resolvedBrowserPath is null)
        {
            throw new InvalidOperationException(
                "No browser executable was found. Set MDPDF_BROWSER_PATH or install Chrome, Edge, or Chromium."
            );
        }

        if (saveBrowserPath)
        {
            await browserConfig.SaveBrowserPathAsync(configPath, resolvedBrowserPath);
            Console.WriteLine($"Saved browser path to: {configPath}");
        }

        await markdownToPdfConverter.RenderToSinglePageAsync(
            resolvedInput.MarkdownContent,
            resolvedInput.OutputPath,
            resolvedBrowserPath,
            darkMode
        );

        if (openPdf)
        {
            TryOpenPdf(resolvedInput.OutputPath);
        }

        Console.WriteLine($"Done! Saved to: {resolvedInput.OutputPath}");
    }

    private static void TryOpenPdf(string outputPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = outputPath,
                UseShellExecute = true,
            };

            Process.Start(startInfo);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not open PDF automatically: {exception.Message}");
        }
    }
}
