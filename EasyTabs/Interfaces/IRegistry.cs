namespace EasyTabs;

/// <summary>
/// IRegistry interface.
/// </summary>
public interface IRegistry
{
    /// <summary>
    /// returns LocalMachine Hive.
    /// </summary>
    IRegistryKey? LocalMachine { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    object? GetValue(string key);
}