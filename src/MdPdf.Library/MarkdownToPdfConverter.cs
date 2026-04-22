using System.IO.Abstractions;
using Markdig;
using MdPdf.Library.Runtime;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace MdPdf.Library;

public sealed class MarkdownToPdfConverter
{
    private const string LIGHT_STYLESHEET_FILE_NAME = "github-markdown-light.min.css";
    private const string DARK_STYLESHEET_FILE_NAME = "github-markdown-dark.min.css";
    private const string MERMAID_SCRIPT_FILE_NAME = "mermaid.min.js";
    private const string LIGHT_STYLESHEET_CDN =
        "https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.8.1/github-markdown-light.min.css";
    private const string DARK_STYLESHEET_CDN =
        "https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.8.1/github-markdown-dark.min.css";
    private const string MERMAID_SCRIPT_CDN =
        "https://cdn.jsdelivr.net/npm/mermaid@11.14.0/dist/mermaid.min.js";

    private readonly IFileSystem _fileSystem;
    private readonly AppPaths _appPaths;
    private readonly IAssetDownloader _assetDownloader;

    public MarkdownToPdfConverter(
        IFileSystem fileSystem,
        AppPaths appPaths,
        IAssetDownloader assetDownloader
    )
    {
        _fileSystem = fileSystem;
        _appPaths = appPaths;
        _assetDownloader = assetDownloader;
    }

    public async Task<string> BuildHtmlAsync(
        string markdownContent,
        bool darkMode = false,
        string? assetsDirectory = null
    )
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        var htmlBody = Markdown.ToHtml(markdownContent, pipeline);
        var mermaidTheme = darkMode ? "dark" : "default";
        var stylesheet = await ResolveStylesheetAsync(darkMode, assetsDirectory);
        var mermaidScript = await ResolveMermaidScriptAsync(assetsDirectory);

        return $@"
        <!DOCTYPE html>
        <html lang='en'>
            <head>
                <meta charset='utf-8'>
                {stylesheet}
                <style>
                    html, body {{
                        margin: 0;
                        padding: 24px;
                        box-sizing: border-box;
                    }}

                    body.markdown-body {{
                        width: 100%;
                    }}

                    .markdown-body .markdown-alert {{
                        margin: 1rem 0;
                    }}

                    .markdown-body .markdown-alert-title {{
                        display: flex;
                        align-items: center;
                        gap: 0.5rem;
                    }}

                    .markdown-body .markdown-alert-note {{
                        background-color: rgba(9, 105, 218, 0.12);
                    }}

                    .markdown-body .markdown-alert-tip {{
                        background-color: rgba(31, 136, 61, 0.12);
                    }}

                    .markdown-body .markdown-alert-important {{
                        background-color: rgba(130, 80, 223, 0.12);
                    }}

                    .markdown-body .markdown-alert-warning {{
                        background-color: rgba(154, 103, 0, 0.12);
                    }}

                    .markdown-body .markdown-alert-caution {{
                        background-color: rgba(207, 34, 46, 0.12);
                    }}
                </style>
            </head>
            <body class='markdown-body'>
                {htmlBody}
                <script>
                    {mermaidScript}
                    async function renderDiagrams() {{
                        let mermaidNodes = document.querySelectorAll('code.language-mermaid');

                        if (mermaidNodes.length > 0) {{
                            mermaidNodes.forEach(code => {{
                                let pre = code.parentElement;
                                let div = document.createElement('div');
                                div.className = 'mermaid';
                                div.textContent = code.textContent;
                                pre.replaceWith(div);
                            }});

                            mermaid.initialize({{ startOnLoad: false, theme: '{mermaidTheme}' }});
                            await mermaid.run({{ querySelector: '.mermaid' }});
                        }}

                        let marker = document.createElement('div');
                        marker.id = 'mermaid-done';
                        document.body.appendChild(marker);
                    }}
                    renderDiagrams();
                </script>
            </body>
        </html>";
    }

    public async Task RenderToSinglePageAsync(
        string markdownContent,
        string outputPath,
        string browserPath,
        bool darkMode = false,
        bool landscape = false,
        string? assetsDirectory = null
    )
    {
        var fullHtml = await BuildHtmlAsync(markdownContent, darkMode, assetsDirectory);

        var launchOptions = new LaunchOptions
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-setuid-sandbox"],
            ExecutablePath = browserPath,
        };

        using var browser = await Puppeteer.LaunchAsync(launchOptions);
        using var page = await browser.NewPageAsync();

        await page.SetViewportAsync(new ViewPortOptions { Width = 980, Height = 1080 });

        await page.SetContentAsync(
            fullHtml,
            new NavigationOptions { WaitUntil = [WaitUntilNavigation.Networkidle0] }
        );

        await page.WaitForSelectorAsync("#mermaid-done");

        var contentHeight = await page.EvaluateExpressionAsync<int>(
            "document.documentElement.offsetHeight"
        );

        var pdfOptions = new PdfOptions
        {
            PrintBackground = true,
            Landscape = landscape,
            Width = "980px",
            Height = $"{contentHeight + 2}px",
            MarginOptions = new MarginOptions
            {
                Top = "0px",
                Bottom = "0px",
                Left = "0px",
                Right = "0px",
            },
        };

        await page.PdfAsync(outputPath, pdfOptions);
    }

    private async Task<string> ResolveStylesheetAsync(bool darkMode, string? assetsDirectory)
    {
        var fileName = darkMode ? DARK_STYLESHEET_FILE_NAME : LIGHT_STYLESHEET_FILE_NAME;
        var stylesheetPath = await EnsureLocalAssetAsync(
            fileName,
            darkMode ? DARK_STYLESHEET_CDN : LIGHT_STYLESHEET_CDN,
            assetsDirectory
        );
        var css = await ReadAllTextAsync(stylesheetPath);
        return $"<style>{css}</style>";
    }

    private async Task<string> ResolveMermaidScriptAsync(string? assetsDirectory)
    {
        var scriptPath = await EnsureLocalAssetAsync(
            MERMAID_SCRIPT_FILE_NAME,
            MERMAID_SCRIPT_CDN,
            assetsDirectory
        );
        return await ReadInlineScriptAsync(scriptPath);
    }

    private async Task<string> EnsureLocalAssetAsync(
        string fileName,
        string assetCdn,
        string? assetsDirectory
    )
    {
        var assetsRoot = assetsDirectory ?? _appPaths.GetAssetsPath();
        _fileSystem.Directory.CreateDirectory(assetsRoot);

        var assetPath = _fileSystem.Path.Combine(assetsRoot, fileName);
        if (await CanOpenForReadAsync(assetPath))
            return assetPath;

        var assetContent = await _assetDownloader.DownloadAsync(new Uri(assetCdn));
        await WriteAllTextAsync(assetPath, assetContent);
        return assetPath;
    }

    private async Task<string> ReadInlineScriptAsync(string scriptPath)
    {
        var scriptContent = await ReadAllTextAsync(scriptPath);
        return scriptContent.Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> ReadAllTextAsync(string path)
    {
        await using var stream = _fileSystem.FileStream.New(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous
        );

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private async Task WriteAllTextAsync(string path, string contents)
    {
        await using var stream = _fileSystem.FileStream.New(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            options: FileOptions.Asynchronous
        );

        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(contents);
        await writer.FlushAsync();
    }

    private async Task<bool> CanOpenForReadAsync(string path)
    {
        try
        {
            await using var _ = _fileSystem.FileStream.New(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                options: FileOptions.Asynchronous
            );

            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
