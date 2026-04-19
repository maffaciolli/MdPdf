namespace MdPdf.Library.Runtime;

public interface ICommandRunner
{
    string? Run(string fileName, IEnumerable<string> arguments);
}
