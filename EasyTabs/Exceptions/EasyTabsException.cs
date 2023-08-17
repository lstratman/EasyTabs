using System;
using System.Runtime.Serialization;

namespace EasyTabs;

/// <summary>
/// EasyTabsException class.
/// </summary>
public class EasyTabsException : Exception
{
    /// <summary>
    /// Initializes an EasyTabsException object.
    /// </summary>
    /// <param name="message">The message.</param>
    public EasyTabsException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes an EasyTabsException object.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EasyTabsException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}