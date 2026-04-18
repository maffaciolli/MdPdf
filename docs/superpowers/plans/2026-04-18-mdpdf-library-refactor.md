# MdPdf Library Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract the command-line parser and markdown-to-PDF converter into a shared `MdPdf.Library` project consumed by both the console app and its tests.

**Architecture:** Keep `Program.cs` in `MdPdf.Console` as the executable entrypoint only. Move reusable parsing and rendering code into `MdPdf.Library`, then reference that library from the console app and test project so both depend on the same implementation.

**Tech Stack:** .NET 10, MSBuild project references, Markdig, PuppeteerSharp, xUnit, Shouldly

---

### Task 1: Create the shared library project

**Files:**
- Create: `src/MdPdf.Library/MdPdf.Library.csproj`
- Create: `src/MdPdf.Library/CommandLineParser.cs`
- Create: `src/MdPdf.Library/MarkdownToPdfConverter.cs`

- [ ] **Step 1: Add the library project files**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Markdig" />
    <PackageReference Include="PuppeteerSharp" />
  </ItemGroup>
</Project>
```

```csharp
using System;
using System.Collections.Generic;

namespace MdPdf.Library;

public static class CommandLineParser
{
    public static ParsedArguments? ParseArguments(string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        bool darkMode = true;
        var positionalArgs = new List<string>();

        foreach (var arg in args)
        {
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

            positionalArgs.Add(arg);
        }

        if (positionalArgs.Count == 0)
        {
            return null;
        }

        return new ParsedArguments(
            positionalArgs[0],
            positionalArgs.Count > 1 ? positionalArgs[1] : null,
            darkMode
        );
    }
}

public readonly record struct ParsedArguments(string Input, string? OutputPath, bool DarkMode);
```

```csharp
using System;
using System.Threading.Tasks;
using Markdig;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace MdPdf.Library;

public static class MarkdownToPdfConverter
{
    public static string BuildHtml(string markdownContent, bool darkMode = false)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        var htmlBody = Markdown.ToHtml(markdownContent, pipeline);
        var markdownStylesheet = darkMode
            ? "https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.8.1/github-markdown-dark.min.css"
            : "https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.8.1/github-markdown-light.min.css";
        var bodyBackground = darkMode ? "#0d1117" : "#ffffff";
        var bodyForeground = darkMode ? "#c9d1d9" : "#24292f";
        var mermaidTheme = darkMode ? "dark" : "default";

        return $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='utf-8'>
            <link rel='stylesheet' href='{markdownStylesheet}'>
            <style>
                body {{
                    box-sizing: border-box;
                    min-width: 200px;
                    max-width: 980px;
                    margin: 0 auto;
                    padding: 40px;
                    background: {bodyBackground};
                    color: {bodyForeground};
                }}

                html {{
                    background: {bodyBackground};
                }}

                .markdown-body {{
                    background: {bodyBackground};
                    color: {bodyForeground};
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

                .markdown-body pre,
                .markdown-body code {{
                    background-color: {(darkMode ? "#161b22" : "#f6f8fa")};
                }}

                .markdown-body table tr {{
                    background-color: {(darkMode ? "#0d1117" : "#ffffff")};
                    border-top: 1px solid {(darkMode ? "#30363d" : "#d0d7de")};
                }}

                .markdown-body table tr:nth-child(2n) {{
                    background-color: {(darkMode ? "#161b22" : "#f6f8fa")};
                }}
            </style>
        </head>
        <body class='markdown-body'>
            {htmlBody}
            <script type='module'>
                import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11.14.0/+esm'
                
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

    public static async Task RenderToSinglePageAsync(
        string markdownContent,
        string outputPath,
        bool darkMode = false
    )
    {
        var fullHtml = BuildHtml(markdownContent, darkMode);

        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        var launchOptions = new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" },
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
}
```

- [ ] **Step 2: Verify the library code is self-contained**

Run: `dotnet build src/MdPdf.Library/MdPdf.Library.csproj`
Expected: restore and build succeed once package sources are available.

- [ ] **Step 3: Commit the new library project**

```bash
git add src/MdPdf.Library
git commit -m "feat: add shared mdpdf library"
```

### Task 2: Rewire the console app and tests

**Files:**
- Modify: `src/MdPdf.Console/MdPdf.Console.csproj`
- Modify: `src/MdPdf.Console/Program.cs`
- Delete: `src/MdPdf.Console/CommandLineParser.cs`
- Delete: `src/MdPdf.Console/MarkdownToPdfConverter.cs`
- Modify: `tests/MdPdf.Console.Tests/MdPdf.Console.Tests.csproj`
- Modify: `tests/MdPdf.Console.Tests/GlobalUsings.cs`
- Modify: `tests/MdPdf.Console.Tests/CommandLineParserTests.cs`
- Modify: `tests/MdPdf.Console.Tests/MarkdownToPdfConverterTests.cs`
- Modify: `MdPdf.slnx`

- [ ] **Step 1: Update project references and namespaces**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference
      Include="CSharpier.MsBuild"
      PrivateAssets="all"
      IncludeAssets="runtime; build; native; contentfiles; analyzers"
    />
    <ProjectReference Include="..\MdPdf.Library\MdPdf.Library.csproj" />
  </ItemGroup>
</Project>
```

```csharp
using System;
using System.IO;
using MdPdf.Library;

var parsedArguments = CommandLineParser.ParseArguments(args);
if (parsedArguments is null)
{
    Console.WriteLine(
        "Usage: mark-to-pdf <markdown-file-or-string> [output-path] [--dark|--light]"
    );
    return;
}

var arguments = parsedArguments.Value;
string input = arguments.Input;
string markdownContent;
string outputPath;
bool darkMode = arguments.DarkMode;

if (File.Exists(input))
{
    markdownContent = await File.ReadAllTextAsync(input);
    outputPath = arguments.OutputPath ?? Path.ChangeExtension(Path.GetFullPath(input), ".pdf");
}
else
{
    markdownContent = input;
    outputPath =
        arguments.OutputPath ?? Path.Combine(Directory.GetCurrentDirectory(), "output.pdf");
}

Console.WriteLine($"Reading from: {(File.Exists(input) ? input : "Raw String")}");
Console.WriteLine($"Theme: {(darkMode ? "Dark" : "Light")}");
Console.WriteLine("Rendering PDF...");

await MarkdownToPdfConverter.RenderToSinglePageAsync(markdownContent, outputPath, darkMode);

Console.WriteLine($"Done! Saved to: {outputPath}");
```

```csharp
global using Xunit;
global using MdPdf.Library;
global using Shouldly;
```

```csharp
using MdPdf.Library;
using Shouldly;

namespace MdPdf.Console.Tests;

public class CommandLineParserTests
{
    [Fact]
    public void Must_default_to_dark_when_no_theme_flag_is_present()
    {
        // Arrange
        string[] args = ["input.md", "out.pdf"];

        // Act
        var result = CommandLineParser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OutputPath.ShouldBe("out.pdf");
        parsed.DarkMode.ShouldBeTrue();
    }

    [Fact]
    public void Must_use_light_theme_when_light_flag_is_present()
    {
        // Arrange
        string[] args = ["input.md", "--light"];

        // Act
        var result = CommandLineParser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OutputPath.ShouldBeNull();
        parsed.DarkMode.ShouldBeFalse();
    }

    [Fact]
    public void Must_use_dark_theme_when_dark_flag_is_present()
    {
        // Arrange
        string[] args = ["input.md", "--dark"];

        // Act
        var result = CommandLineParser.ParseArguments(args);

        // Assert
        result.ShouldNotBeNull();
        var parsed = result!.Value;

        parsed.Input.ShouldBe("input.md");
        parsed.OutputPath.ShouldBeNull();
        parsed.DarkMode.ShouldBeTrue();
    }
}
```

```csharp
using MdPdf.Library;
using Shouldly;

namespace MdPdf.Console.Tests;

public class MarkdownToPdfConverterTests
{
    [Fact]
    public void Must_build_light_theme_html_when_dark_mode_is_false()
    {
        // Arrange
        const string markdown = "Hello";

        // Act
        var html = MarkdownToPdfConverter.BuildHtml(markdown);

        // Assert
        html.ShouldContain("github-markdown-light.min.css");
        html.ShouldContain("theme: 'default'");
        html.ShouldContain(markdown);
    }

    [Fact]
    public void Must_build_dark_theme_html_when_dark_mode_is_true()
    {
        // Arrange
        const string markdown = "Hello";

        // Act
        var html = MarkdownToPdfConverter.BuildHtml(markdown, darkMode: true);

        // Assert
        html.ShouldContain("github-markdown-dark.min.css");
        html.ShouldContain("theme: 'dark'");
    }
}
```

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MdPdf.Library\MdPdf.Library.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Update the solution to include the new project**

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/MdPdf.Console/MdPdf.Console.csproj" />
    <Project Path="src/MdPdf.Library/MdPdf.Library.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/MdPdf.Console.Tests/MdPdf.Console.Tests.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 3: Run the tests/build against the new library boundary**

Run:
`dotnet test tests/MdPdf.Console.Tests/MdPdf.Console.Tests.csproj`

Expected:
The tests compile against `MdPdf.Library` and the current parser and HTML-generation checks still pass.

- [ ] **Step 4: Commit the refactor**

```bash
git add src/MdPdf.Console src/MdPdf.Library tests/MdPdf.Console.Tests MdPdf.slnx
git commit -m "refactor: move shared logic into mdpdf library"
```
