using Stratman.Windows.Forms.TitleBarTabs;

namespace TestApp
{
    public partial class TestApp : TitleBarTabs
    {
        public TestApp()
        {
            InitializeComponent();

			AeroPeekEnabled = false;

            Tabs.Add(new TitleBarTab(this)
                {
                    Content = new TabWindow
                    {
                        Text = "New Tab"
                    }
                });

            SelectedTabIndex = 0;
            TabRenderer = new ChromeTabRenderer(this);
            Icon = Resources.DefaultIcon;
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
