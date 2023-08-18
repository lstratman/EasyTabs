// 1. Use Easy Tabs

using System.Diagnostics;
using CoreLibrary.Logging;
using EasyTabs;
using EasyTabs.Model;
using WindowsFormsLibrary.Utility;

namespace EasyTabsTests;

public partial class TabbedApplicationForm : Form
{
    // 2. Important: Declare ParentTabs
    protected AppContainer? ParentTabs
    {
        get
        {
            return ParentForm as AppContainer;
        }
    }

    public TabbedApplicationForm()
    {
        InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {

    }

    private async void button1_Click(object sender, EventArgs e)
    {
        await Button1OnClick();
    }

    internal async Task<Form> Button1OnClick()
    {
        await LoggerFactory.Instance.GetLogger().InfoAsync($"Adding tab at: {DateTime.Now} - Initial Tab: {Process.GetCurrentProcess().GetInitialContentText()}", Application.ProductName);
        if (ParentTabs != null)
        {
            await ParentTabs.AddNewTab();
        }

        return this;
    }

    private void button2_Click(object sender, EventArgs e)
    {
        _ = Button2OnClick();
    }

    internal async Task<Form> Button2OnClick()
    {
        if (ParentTabs != null)
        {
            ParentTabs.Invoke(
                async () =>
                {
                    ParentTabs.ReplaceCreateFormHandlersOnce(CreateFormInOtherThread);
                    await ParentTabs.AddNewTab();
                });
        }

        await TaskEx.CompletedTask;
        return this;
    }

    void CreateFormInOtherThread(object s, FormEventArgs e)
    {
        e.Form = FormCreationHelper.Instance.CreateFormInOtherThread($"Button {DateTime.Now} - Initial Tab: {Process.GetCurrentProcess().GetInitialContentText()}", WorkWithForm);
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