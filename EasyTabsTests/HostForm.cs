using EasyTabs;

namespace EasyTabsTests;

public partial class HostForm : Form
{
    public HostForm()
    {
        InitializeComponent();
    }

    private async void Button1_Click(object sender, EventArgs e)
    {
        await Button1OnClick();
    }

    internal async Task Button1OnClick()
    {
        Form? b = null;
        var thread = new Thread(
            () =>
            {
                b = new Form();
                b.TopLevel = false;
                b.Text = $"Button {DateTime.Now}";
                var control = new Button()
                              {
                                  Text = "Test",
                              };
                b.BackColor = Color.White;
                control.Click += (_, _) =>
                {
                    MessageBox.Show(DateTime.Now.ToString());
                };
                b.Controls.Add(control);
                b.FormBorderStyle = FormBorderStyle.None;
                Application.Run(b);
            });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        while (b == null)
        {
            Thread.Sleep(10);
        }

        var otherThreadForm = b;
        await this.HostFormInParentForm(otherThreadForm);
    }

    private async void button2_Click(object sender, EventArgs e)
    {
        await Button2OnClick();
    }

    public async Task Button2OnClick()
    {
        var form = new Form();
        form.BackColor = Color.Violet;
        form.Show();
        await this.HostFormInParentForm(form);
    }
}