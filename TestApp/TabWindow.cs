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
        private class NewTabLifespanHandler : ILifeSpanHandler
        {
            private TitleBarTabs _parentForm;

            public NewTabLifespanHandler(TitleBarTabs parentForm)
            {
                _parentForm = parentForm;
            }

            public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
            {
                return true;
            }

            public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
            {
            }

            public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
            {
            }

            public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
            {
                _parentForm.BeginInvoke(new Action(() =>
                {
                    _parentForm.AddNewTab(targetUrl);
                }));

                // Cancel popup creation and open the targetUrl in a new tab.
                // This only works for GET URLs, those created via JavaScript
                // and those that require POST data cannot be used in this fashion.


                newBrowser = null;
                return true;
            }
        }

        public readonly ChromiumWebBrowser WebBrowser;
        private bool faviconLoaded = false;

	    protected TitleBarTabs ParentTabs
	    {
		    get
		    {
			    return (ParentForm as TitleBarTabs);
		    }
	    }

        public TabWindow(string startUrl)
        {
            InitializeComponent();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            WebBrowser = new ChromiumWebBrowser(startUrl)
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(0, 38),
                MinimumSize = new Size(20, 20),
                Name = "webBrowser",
                Size = new Size(326, 251),
                TabIndex = 6,
                LifeSpanHandler = new NewTabLifespanHandler(ParentTabs)
            };

            Controls.Add(WebBrowser);

            WebBrowser.TitleChanged += WebBrowser_TitleChanged;
            WebBrowser.AddressChanged += WebBrowser_AddressChanged;
            WebBrowser.LoadingStateChanged += webBrowser_DocumentCompleted;
        }

        private void WebBrowser_AddressChanged(object sender, AddressChangedEventArgs e)
        {
            Invoke(new Action(() => urlTextBox.Text = e.Address));

            if (e.Address != "about.blank" && !e.Address.StartsWith("data:") && !faviconLoaded)

            {
                Uri uri = new Uri(e.Address);

                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    try
                    {
                        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri.Scheme + "://" + uri.Host + "/favicon.ico");
                        webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36";
                        webRequest.KeepAlive = false;
                        webRequest.AllowAutoRedirect = true;

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
                faviconLoaded = true;
            }
        }

        private void WebBrowser_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            Invoke(new Action(() => Text = e.Title));
        }

        void webBrowser_DocumentCompleted(object sender, LoadingStateChangedEventArgs e)
        {
            if (urlTextBox.Text == "about:blank")
            {
                Invoke(new Action(() => Icon = Resources.DefaultIcon));
            }
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

                faviconLoaded = false;
                WebBrowser.Load(fullUrl);
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
            WebBrowser.Back();
        }

        private void forwardButton_Click(object sender, EventArgs e)
        {
            WebBrowser.Forward();
        }

        private void TabWindow_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
    }
}
