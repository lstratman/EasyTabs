using System;
using System.Windows.Forms;
using System.Windows.Threading;
using CoreLibrary.Model.Utility.Wait;
using CoreLibrary.Utility.Wait;
using CoreLibrary.WaitUtility;

namespace EasyTabs;

/// <summary>
/// Extension methods for Func&lt;Form&gt;.
/// </summary>
public static class TabbedApplicationHelper
{
    /// <summary>
    /// Creates a TabbedApplication.
    /// </summary>
    /// <param name="CreateInitialForm">The Func that creates the initial form.</param>
    /// <returns>A TitleBarTabsApplicationContext</returns>
    public static ApplicationContext CreateTabbedApplication(this Func<Form> CreateInitialForm)
    {
        return CreateTabbedApplication(CreateInitialForm, null);
    }

    private static void SetupWaitUtility()
    {
        TaskWaiterContainer.Dispatcher = new DispatcherWrapper(() =>
        {
            DispatcherFrame dispatcherFrame = new DispatcherFrame();
            return new DispatcherFrameWrapper(dispatcherFrame)
                   {
                       GetContinue = () => dispatcherFrame.Continue,
                       SetContinue = value => dispatcherFrame.Continue = value
                   };
        }, (Action<object>)(f => Dispatcher.PushFrame((DispatcherFrame)f)));
        TaskWaiterContainer.TaskWaiter = TaskWaiter.Instance;
    }

    /// <summary>
    /// Creates a TabbedApplication.
    /// </summary>
    /// <param name="CreateInitialForm">The Func that creates the initial form.</param>
    /// <param name="CreateForm">The Func that creates the other forms.</param>
    /// <returns>A TitleBarTabsApplicationContext</returns>
    public static ApplicationContext CreateTabbedApplication(this Func<Form> CreateInitialForm, Func<Form>? CreateForm)
    {
        SetupWaitUtility();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        AppContainer container = new AppContainer();
        container.CreatingForm += (_, e) =>
        {
            if (CreateForm == null)
            {
                CreateForm = CreateInitialForm;
            }

            var eForm = CreateForm();
            e.Form = eForm;
        };

        // Add the initial Tab
        container.AddTab(CreateInitialForm());

        // Set initial tab the first one
        container.SelectedTabIndex = 0;

        // Create tabs and start application
        TitleBarTabsApplicationContext applicationContext = new TitleBarTabsApplicationContext();
        applicationContext.Start(container);
        return applicationContext;
    }
}