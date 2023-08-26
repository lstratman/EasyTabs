using EasyTabs;
using EasyTabsTests;
using FormLogging.Logging;

namespace EasyTabsPoC;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        FormLogger.Instance.RunApplicationWithFormLogging(() => TabbedApplicationHelper.CreateTabbedApplication(() => new TabbedApplicationForm
            {
                Text = "Test Form with a title very very long"
            }));
        return 0;
    }
}