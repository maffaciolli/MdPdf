namespace MdPdf.Library.Runtime;

public sealed class SystemEnvironment : ISystemEnvironment
{
    public bool IsWindows()
    {
        return OperatingSystem.IsWindows();
    }

    public bool IsLinux()
    {
        return OperatingSystem.IsLinux();
    }

    public bool IsMacOS()
    {
        return OperatingSystem.IsMacOS();
    }

    public string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

    public string GetFolderPath(Environment.SpecialFolder folder)
    {
        return Environment.GetFolderPath(folder);
    }

    public string ExpandEnvironmentVariables(string value)
    {
        return Environment.ExpandEnvironmentVariables(value);
    }
}
