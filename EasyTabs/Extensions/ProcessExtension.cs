using System.Diagnostics;
using System.Windows.Forms;

namespace EasyTabs;

/// <summary>
/// ProcessExtension class.
/// </summary>
public static class ProcessExtension
{

    /// <summary>
    /// Gets the AppContainer of a process
    /// </summary>
    /// <param name="process">The process.</param>
    /// <returns>The AppContainer of a process</returns>
    public static AppContainer? GetAppContainer(this Process process)
    {
        var myHandle = process.MainWindowHandle;
        var fromHandle = Control.FromHandle(myHandle) as AppContainer;
        return fromHandle;
    }

    /// <summary>
    /// Gets the initial content text of the AppContainer of a process
    /// </summary>
    /// <param name="process">The process.</param>
    /// <returns>the main window text</returns>
    public static string GetInitialContentText(this Process process)
    {
        var initialContent = process.GetAppContainer()?.GetInitialContent<Form>();
        string? initialContentText = null;
        if (initialContent != null)
        {
            initialContentText = (string?)initialContent.Invoke(() => initialContent.Text);
        }
        return initialContentText ?? string.Empty;
    }

}