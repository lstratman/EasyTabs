// 1. Use Easy Tabs

using EasyTabs;

namespace WinFormsApp;

public partial class Form1 : Form
{
    // 2. Important: Declare ParentTabs
    protected AppContainer ParentTabs
    {
        get
        {
            return (ParentForm as AppContainer);
        }
    }

    public Form1()
    {
        InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {

    }

    private async void button1_Click(object sender, EventArgs e)
    {
        MessageBox.Show($"Adding tab at: {DateTime.Now}");
        await ParentTabs.AddNewTab();
    }

    private async void button2_Click(object sender, EventArgs e)
    {
        ParentTabs.ReplaceCreateFormHandlersOnce(
            (s, ee) =>
            {
                Form? b = null;
                var thread = new Thread(() =>
                {
                    b = new Form();
                    b.TopLevel = false;
                    b.Text = $"Button {DateTime.Now}";
                    var control = new Button()
                                  {
                                      Text = "Test",
                                  };
                    b.BackColor = Color.White;
                    control.Click += (s, a) => { MessageBox.Show(DateTime.Now.ToString()); };
                    b.Controls.Add(control);
                    b.FormBorderStyle = FormBorderStyle.None;
                    b.ShowInTaskbar = false;
                    b.WindowState = FormWindowState.Minimized;
                    Application.Run(b);
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                while (b == null)
                {
                    Thread.Sleep(10);
                }

                ee.Form = b;
            });
        await ParentTabs.AddNewTab();
    }
}