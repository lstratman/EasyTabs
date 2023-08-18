using System.Threading;

namespace EasyTabs.Model;

/// <summary>
/// ThreadEventArgs class.
/// </summary>
public class ThreadEventArgs : System.EventArgs
{
    /// <summary>
    /// Creates a ThreadEventArgs object.
    /// </summary>
    /// <param name="value">The initial Thread.</param>
    public ThreadEventArgs(Thread? value)
    {
        Thread = value;
    }

    /// <summary>
    /// Creates a ThreadEventArgs object.
    /// </summary>
    public ThreadEventArgs()
    {
    }

    /// <summary>
    /// Gets or sets the Thread value.
    /// </summary>
    public Thread? Thread { get; }
}