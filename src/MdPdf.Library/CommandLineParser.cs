namespace MdPdf.Library;

public sealed class CommandLineParser
{
    public ParsedArguments? ParseArguments(string[] args)
    {
        if (args.Length == 0)
            return null;

        var darkMode = true;
        var landscape = false;
        var saveBrowserPath = false;
        var openPdf = false;
        string? browserPath = null;
        var positionalArgs = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (string.Equals(arg, "--light", StringComparison.OrdinalIgnoreCase))
            {
                darkMode = false;
                continue;
            }

            if (string.Equals(arg, "--dark", StringComparison.OrdinalIgnoreCase))
            {
                darkMode = true;
                continue;
            }

            if (string.Equals(arg, "--portrait", StringComparison.OrdinalIgnoreCase))
            {
                landscape = false;
                continue;
            }

            if (string.Equals(arg, "--landscape", StringComparison.OrdinalIgnoreCase))
            {
                landscape = true;
                continue;
            }

            if (string.Equals(arg, "--browser-path", StringComparison.OrdinalIgnoreCase))
            {
                index++;
                if (index >= args.Length)
                    return null;

                browserPath = args[index];
                continue;
            }

            if (string.Equals(arg, "--save-browser-path", StringComparison.OrdinalIgnoreCase))
            {
                saveBrowserPath = true;
                continue;
            }

            if (string.Equals(arg, "--open", StringComparison.OrdinalIgnoreCase))
            {
                openPdf = true;
                continue;
            }

            positionalArgs.Add(arg);
        }

        if (positionalArgs.Count == 0)
            return null;

        return new ParsedArguments(
            positionalArgs[0],
            positionalArgs.Count > 1 ? positionalArgs[1] : null,
            darkMode,
            landscape,
            browserPath,
            saveBrowserPath,
            openPdf
        );
    }
}

public readonly record struct ParsedArguments(
    string Input,
    string? OutputPath,
    bool DarkMode,
    bool Landscape,
    string? BrowserPath,
    bool SaveBrowserPath,
    bool OpenPdf
);
