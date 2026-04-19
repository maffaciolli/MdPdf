namespace MdPdf.Library.Runtime;

public interface IWindowsRegistryReader
{
    string? GetCurrentUserValue(string subKeyPath, string valueName);

    string? GetClassesRootValue(string subKeyPath, string? valueName);
}
