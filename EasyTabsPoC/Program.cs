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
        FormLogger.Instance.RunApplicationWithFormLogging(()=>(ApplicationContext)TabbedApplicationHelper.CreateTabbedApplication(() => new TabbedApplicationForm
                                                                              {
                                                                                  Text = "Test Form"
                                                                              }));
        return 0;
    }
}