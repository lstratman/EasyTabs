using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using EasyTabs.Drawing;
using EasyTabs.Model;

namespace EasyTabs;

/// <summary>AppContainer class.</summary>
public class AppContainer : TitleBarTabs
{
    private readonly IAllEventsHandler<FormEventArgs> allEventsHandlerImplementation;

    /// <summary>Creates an AppContainer object.</summary>
    public AppContainer()
    {
        InitializeComponent();

        AeroPeekEnabled = true;
        allEventsHandlerImplementation = new AllEventsHandler<FormEventArgs>();
        allEventsHandlerImplementation.InternalEventWithDelegates += (_, e) =>
        {
            CreatingForm?.Invoke(this, e);
        };
        TabRenderer = new ChromeTabRenderer(this);
    }

    /// <summary>This method creates a form.</summary>
    public Form? CreateForm()
    {
        var defaultCreateForm = new Form
        {
            Text = "New Tab"
        };
        var formEventArgs = new FormEventArgs(defaultCreateForm);
        allEventsHandlerImplementation.RaiseEventWithDelegates(this, formEventArgs);
        defaultCreateForm = formEventArgs.Form;
        if (defaultCreateForm != null)
        {
            if (!defaultCreateForm.InvokeRequired)
            {
                defaultCreateForm.ShowInTaskbar = false;
                defaultCreateForm.WindowState = FormWindowState.Minimized;
                defaultCreateForm.Show();
            }
        }

        return defaultCreateForm;
    }

    /// <summary>
    /// CreatingForm event.
    /// </summary>
    public event EventHandler<FormEventArgs>? CreatingForm;


    /// <summary>Handle the method CreateTab that allows the user to create a new Tab on your app when clicking</summary>
    /// <returns>A TitleBarTab object.</returns>
    public override async Task<TitleBarTab> CreateTab()
    {
        TitleBarTab? titleBarTab = null;
        var content = CreateForm();
        var contentCopy = content;
        if (contentCopy != null && !await contentCopy.IsCurrentThreadForm())
        {
            var form = new Form();
            contentCopy.TextChanged += (_, _) =>
            {
                var contentText = contentCopy.Text;
                form.Invoke(
                    () =>
                    {
                        form.Text = contentText;
                    });
            };
            await form.HostFormInParentForm(contentCopy);
            content = form;
        }

        titleBarTab = new TitleBarTab(this)
        {
            // The content will be an instance of another Form
            // In our example, we will create a new instance of the Form1

            Content = content
        };
        return titleBarTab;
    }

    /// <summary>
    /// Replaces delegates once.
    /// </summary>
    /// <param name="newDelegate">The the delegate called once.</param>
    public void ReplaceCreateFormHandlersOnce(EventHandler<FormEventArgs> newDelegate)
    {
        allEventsHandlerImplementation.ReplaceEventWithDelegatesHandlersOnce(newDelegate);
    }


}