using CefSharp;
using CefSharp.WinForms;
using EasyTabs;

namespace TestApp
{
	public partial class TestApp : TitleBarTabs
    {
        public TestApp()
        {
            InitializeComponent();

            AeroPeekEnabled = true;
            TabRenderer = new ChromeTabRenderer(this);
            Icon = Resources.DefaultIcon;
            Width = 800;
            Height = 600;
        }

        static TestApp()
        {
            // This is only so that generating a thumbnail for Aero peek works properly:  with GPU acceleration enabled, all you get is a black box
            // when you try to "snapshot" the web browser control.  If you don't plan on using Aero peek, remove this method.
            CefSettings cefSettings = new CefSettings();
            cefSettings.DisableGpuAcceleration();

            Cef.Initialize(cefSettings);
        }

        public override TitleBarTab CreateTab()
        {
            return new TitleBarTab(this)
            {
                Content = new TabWindow
                {
                    Text = "New Tab"
                }
            };
        }
    }
}
