using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using EasyTabs;

namespace TestApp
{
    public partial class TabWindow : Form
    {
        private readonly ChromiumWebBrowser webBrowser;

	    protected TitleBarTabs ParentTabs
	    {
		    get
		    {
			    return (ParentForm as TitleBarTabs);
		    }
	    }

        public TabWindow()
        {
            InitializeComponent();

            webBrowser = new ChromiumWebBrowser("about:blank")
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(0, 36),
                MinimumSize = new Size(20, 20),
                Name = "webBrowser",
                Size = new Size(326, 253),
                TabIndex = 6
            };

            Controls.Add(webBrowser);

            webBrowser.TitleChanged += WebBrowser_TitleChanged;
            webBrowser.AddressChanged += WebBrowser_AddressChanged;
            webBrowser.LoadingStateChanged += webBrowser_DocumentCompleted;
        }

        private void WebBrowser_AddressChanged(object sender, AddressChangedEventArgs e)
        {
            Invoke(new Action(() => urlTextBox.Text = e.Address));
        }

        private void WebBrowser_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            Invoke(new Action(() => Text = e.Title));
        }

        void webBrowser_DocumentCompleted(object sender, LoadingStateChangedEventArgs e)
        {
            if (urlTextBox.Text != "about:blank" && !e.IsLoading)
            {
                Uri uri = new Uri(e.Browser.MainFrame.Url);

                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    try
                    {
                        WebRequest webRequest = WebRequest.Create(uri.Scheme + "://" + uri.Host + "/favicon.ico");
                        WebResponse response = webRequest.GetResponse();
                        Stream stream = response.GetResponseStream();

                        if (stream != null)
                        {
                            byte[] buffer = new byte[1024];

                            using (MemoryStream ms = new MemoryStream())
                            {
                                int read;

                                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                    ms.Write(buffer, 0, read);

                                ms.Seek(0, SeekOrigin.Begin);

                                Invoke(new Action(() =>
                                {
                                    Icon = new Icon(ms);

                                    ParentTabs.UpdateThumbnailPreviewIcon(ParentTabs.Tabs.Single(t => t.Content == this));
                                    ParentTabs.RedrawTabs();
                                }));
                            }
                        }
                    }

                    catch
                    {
                        Invoke(new Action(() => Icon = Resources.DefaultIcon));
                    }
                }

                Invoke(new Action(() => Parent.Refresh()));
            }

            else
                Invoke(new Action(() => Icon = Resources.DefaultIcon));
        }

        private void backButton_MouseEnter(object sender, EventArgs e)
        {
            backButton.BackgroundImage = Resources.ButtonHoverBackground;
        }

        private void backButton_MouseLeave(object sender, EventArgs e)
        {
            backButton.BackgroundImage = null;
        }

        private void urlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string fullUrl = urlTextBox.Text;

                if (!Regex.IsMatch(fullUrl, "^[a-zA-Z0-9]+\\://"))
                    fullUrl = "http://" + fullUrl;

                webBrowser.Load(fullUrl);
            }
        }

        private void forwardButton_MouseEnter(object sender, EventArgs e)
        {
            forwardButton.BackgroundImage = Resources.ButtonHoverBackground;
        }

        private void forwardButton_MouseLeave(object sender, EventArgs e)
        {
            forwardButton.BackgroundImage = null;
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            webBrowser.Back();
        }

        private void forwardButton_Click(object sender, EventArgs e)
        {
            webBrowser.Forward();
        }
    }
}
