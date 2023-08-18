using Microsoft.Win32;

namespace EasyTabs.Drawing;

/// <summary>
/// RegistryHelper class.
/// </summary>
public class RegistryHelper : IRegistry
{
    /// <summary>
    /// LocalMachine hive.
    /// </summary>
    public IRegistryKey LocalMachine
    {
        get;
    } = new RegistryKeyWrapper(Registry.LocalMachine);

    /// <summary>
    /// Gets the Value
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public object? GetValue(string key)
    {
        return Registry.LocalMachine.GetValue(key);
    }
}