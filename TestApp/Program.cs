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
	        
			testApp.Tabs.Add(
		        new TitleBarTab(testApp)
			        {
				        Content = new TabWindow
					                  {
						                  Text = "New Tab"
					                  }
			        });
			testApp.SelectedTabIndex = 0;

			TitleBarTabsApplicationContext applicationContext = new TitleBarTabsApplicationContext();
			applicationContext.Start(testApp);

            Application.Run(applicationContext);
        }
    }
}
