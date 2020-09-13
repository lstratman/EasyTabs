using System;
using System.Windows.Forms;
using EasyTabs;

namespace TestApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            TestApp testApp = new TestApp();

            //Display Welcome To EasyTabs in the default browser
            testApp.AddNewTab("data:text/html,%3Ch1%3EWelcome%20to%20EasyTabs!%3C%2Fh1%3E");
            testApp.SelectedTabIndex = 0;

            TitleBarTabsApplicationContext applicationContext = new TitleBarTabsApplicationContext();
            applicationContext.Start(testApp);

            Application.Run(applicationContext);
        }
    }
}
