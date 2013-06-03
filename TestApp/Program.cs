using System;
using System.Windows.Forms;
using Stratman.Windows.Forms.TitleBarTabs;

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

            Application.Run(testApp);
        }
    }
}
