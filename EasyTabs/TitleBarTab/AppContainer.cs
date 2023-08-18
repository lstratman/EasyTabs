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

    /// <summary>
    /// Gets the initial content.
    /// </summary>
    /// <typeparam name="TForm">The type of the expected form</typeparam>
    /// <returns>The for as the expected form type contained in first tab</returns>
    public TForm? GetInitialContent<TForm>() where TForm : Form
    {
        return Tabs.Count > 0
            ? Tabs[0]?.Content as TForm
            : null;
    }

    /// <summary>
    /// Adds a tab.
    /// Our First Tab created by default in the Application will have as content the Form
    /// </summary>
    /// <param name="form">The form.</param>
    public void AddTab(Form form)
    {
        var content = form;
        content.ShowInTaskbar = false;
        content.WindowState = FormWindowState.Minimized;
        content.Show();

        Tabs.Add(
            new TitleBarTab(this)
            {
                Content = content
            }
        );
    }

    /// <summary>This method creates a form.</summary>
    /// <param name="text">The tab text.</param>
    public Form? CreateForm(string text)
    {
        var defaultCreateForm = new Form
        {
            Text = text
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
    public override async Task<TitleBarTab?> CreateTab()
    {
        return await CreateTab("New Tab");
    }

    /// <summary>Handle the method CreateTab that allows the user to create a new Tab on your app when clicking</summary>
    /// <param name="text"></param>
    /// <returns>A TitleBarTab object.</returns>
    public override async Task<TitleBarTab?> CreateTab(string text)
    {
        TitleBarTab? titleBarTab = null;
        var content = CreateForm(text);
        if (content == null)
        {
            return null;
        }

        var contentCopy = content;
        if (!await contentCopy.IsCurrentThreadForm())
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
            form.Closed += (s, e) =>
            {
                contentCopy.Invoke(
                    () =>
                    {
                        contentCopy.Close();
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