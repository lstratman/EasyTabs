// 1. Use Easy Tabs

using EasyTabs;
using EasyTabs.Model;

namespace WinFormsApp;

public partial class Form1 : Form
{
    // 2. Important: Declare ParentTabs
    protected AppContainer ParentTabs
    {
        get
        {
            return ParentForm as AppContainer;
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

    private void button2_Click(object sender, EventArgs e)
    {
        ParentTabs.Invoke(
            async () =>
            {
                ParentTabs.ReplaceCreateFormHandlersOnce(CreateFormInOtherThread);
                await ParentTabs.AddNewTab();
            });
    }

    void CreateFormInOtherThread(object s, FormEventArgs e)
    {
        e.Form = FormCreationHelper.CreateFormInOtherThread($"Button {DateTime.Now}", WorkWithForm);
    }

    private static void WorkWithForm(Form b)
    {
        var control = new Button()
        {
            Text = "Test",
        };
        b.BackColor = Color.White;
        control.Click += (s, a) =>
        {
            MessageBox.Show(DateTime.Now.ToString());
        };
        b.Controls.Add(control);
    }
}