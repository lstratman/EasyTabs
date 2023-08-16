using EasyTabs;

namespace WinFormsApp;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.Run(TabbedApplicationHelper.CreateTabbedApplication(() => new Form1
                                                                              {
                                                                                  Text = "Test Form"
                                                                              }));
    }
}