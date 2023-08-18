using EasyTabs;
using FormLogging.Logging;

namespace EasyTabsTests;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    public static int MyMain(string[] args)
    {
        FormLogger.Instance.RunApplicationWithFormLogging(()=>(ApplicationContext)TabbedApplicationHelper.CreateTabbedApplication(() => new TabbedApplicationForm
                                                                              {
                                                                                  Text = "Test Form"
                                                                              }));
        return 0;
    }
}