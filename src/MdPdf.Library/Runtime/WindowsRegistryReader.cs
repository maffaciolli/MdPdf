using System.Runtime.Versioning;
using Microsoft.Win32;

namespace MdPdf.Library.Runtime;

public sealed class WindowsRegistryReader : IWindowsRegistryReader
{
    [SupportedOSPlatform("windows")]
    public string? GetCurrentUserValue(string subKeyPath, string valueName)
    {
        using var subKey = Registry.CurrentUser.OpenSubKey(subKeyPath);
        return subKey?.GetValue(valueName) as string;
    }

    [SupportedOSPlatform("windows")]
    public string? GetClassesRootValue(string subKeyPath, string? valueName)
    {
        using var subKey = Registry.ClassesRoot.OpenSubKey(subKeyPath);
        return subKey?.GetValue(valueName) as string;
    }
}
