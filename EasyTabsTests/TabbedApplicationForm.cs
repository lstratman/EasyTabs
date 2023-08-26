// 1. Use Easy Tabs

using System.Diagnostics;
using CoreLibrary.Logging;
using EasyTabs;
using EasyTabs.Model;

namespace EasyTabsTests;

public partial class TabbedApplicationForm : Form
{
    // 2. Important: Declare ParentTabs
    protected TitleBarTabs? ParentTabs => ParentForm as TitleBarTabs;

    public TabbedApplicationForm()
    {
        InitializeComponent();
        Icon = Properties.Resources.tabs;
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
            await ParentTabs.Invoke(
                async () =>
                {
                    ParentTabs.ReplaceCreateFormHandlersOnce(CreateFormInOtherThread!);
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

    private static void WorkWithForm(Form form)
    {
        var control = new Button()
        {
            Text = "Show MessageBox",
        };
        form.BackColor = Color.White;
        control.Click += (_, _) =>
        {
            MessageBox.Show(DateTime.Now.ToString());
        };
        form.Controls.Add(control);
        control = new Button
        {
            Text = "Close",
        };
        control.Top = 100;
        form.BackColor = Color.White;
        control.Click += (_, _) =>
        {
            form.Close();
        };
        form.Controls.Add(control);
    }
}