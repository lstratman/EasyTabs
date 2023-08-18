using Microsoft.Win32;

namespace EasyTabs.Drawing;

/// <summary>
/// RegistryKeyWrapper class.
/// </summary>
public class RegistryKeyWrapper : IRegistryKey
{
    private readonly RegistryKey? _openSubKey;

    /// <summary>
    /// Creates a RegistryKeyWrapper object.
    /// </summary>
    /// <param name="openSubKey"></param>
    public RegistryKeyWrapper(RegistryKey? openSubKey)
    {
        _openSubKey = openSubKey;
    }

    /// <summary>
    /// Opens a SubKey.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IRegistryKey? OpenSubKey(string name)
    {
        if (_openSubKey != null)
        {
            return new RegistryKeyWrapper(_openSubKey.OpenSubKey(name));
        }

        return null;
    }

    /// <summary>
    /// Gets the Value
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public object? GetValue(string key)
    {
        return _openSubKey?.GetValue(key);
    }
}