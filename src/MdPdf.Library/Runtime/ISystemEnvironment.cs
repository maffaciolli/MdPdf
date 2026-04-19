namespace MdPdf.Library.Runtime;

public interface ISystemEnvironment
{
    bool IsWindows();

    bool IsLinux();

    bool IsMacOS();

    string? GetEnvironmentVariable(string name);

    string GetFolderPath(Environment.SpecialFolder folder);

    string ExpandEnvironmentVariables(string value);
}
