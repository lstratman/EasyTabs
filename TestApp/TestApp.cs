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
        }

        public override TitleBarTab CreateTab(params object[] args)
        {
            string startUrl = "about:blank";

            if(args != null)
            {
                startUrl = (string)args[0];
            }

            return new TitleBarTab(this)
            {
                Content = new TabWindow(startUrl)
                {
                    Text = "New Tab"
                }
            };
        }
    }
}
