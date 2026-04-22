using System.Text;

namespace MdPdf.Console.Tests;

public class ProgramTests
{
    [Fact]
    public async Task Must_print_usage_and_exit_when_help_flag_is_present()
    {
        // Arrange
        var originalOut = global::System.Console.Out;
        var output = new StringWriter(new StringBuilder());
        string[] args = ["input.md", "--help"];

        try
        {
            global::System.Console.SetOut(output);

            // Act
            await Program.Main(args);
        }
        finally
        {
            global::System.Console.SetOut(originalOut);
        }

        // Assert
        output.ToString().ShouldContain("Usage: mdpdf");
        output.ToString().ShouldContain("--portrait|--landscape");
        output.ToString().ShouldNotContain("Rendering PDF...");
    }
}
