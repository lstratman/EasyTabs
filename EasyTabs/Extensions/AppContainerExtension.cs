using System.Windows.Forms;

namespace EasyTabs;

/// <summary>
/// Extension methods for AppContainer.
/// </summary>
public static class AppContainerExtension
{
    /// <summary>
    /// Adds a tab.
    /// Our First Tab created by default in the Application will have as content the Form
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="form">The form.</param>
    public static void AddTab(this AppContainer? container, Form form)
    {
        var content = form;
        content.ShowInTaskbar = false;
        content.WindowState = FormWindowState.Minimized;
        content.Show();
        if (container != null)
        {
            container.Tabs.Add(
                new TitleBarTab(container)
                {
                    Content = content
                }
            );
        }
    }
}