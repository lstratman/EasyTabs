using System.Threading.Tasks;
using CefSharp;
using CefSharp.WinForms;
using EasyTabs;
using EasyTabs.Drawing;

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
        }

        static TestApp()
        {
            // This is only so that generating a thumbnail for Aero peek works properly:  with GPU acceleration enabled, all you get is a black box
            // when you try to "snapshot" the web browser control.  If you don't plan on using Aero peek, remove this method.
            CefSettings cefSettings = new CefSettings();
            cefSettings.DisableGpuAcceleration();

            Cef.Initialize(cefSettings);
        }

        public override Task<TitleBarTab> CreateTab(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                text = "New Tab";
            }
            return new TitleBarTab(this)
                   {
                       Content = new TabWindow
                                 {
                                     Text = text
                                 }
                   }.FromResult();
        }

        public override Task<TitleBarTab> CreateTab()
        {
            return CreateTab(null);
        }
    }
}
