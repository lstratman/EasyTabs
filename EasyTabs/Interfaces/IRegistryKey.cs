namespace EasyTabs;

public interface IRegistryKey
{
    IRegistryKey? OpenSubKey(string name);
    object? GetValue(string name);
}