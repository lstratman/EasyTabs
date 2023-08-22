using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace EasyTabs;

/// <summary>
/// Extension methods for Func&lt;Form&gt;.
/// </summary>
public static class TabbedApplicationHelper
{
    /// <summary>
    /// Creates a TabbedApplication.
    /// </summary>
    /// <param name="createInitialForm">The Func that creates the initial form.</param>
    /// <returns>A TitleBarTabsApplicationContext</returns>
    public static ApplicationContext CreateTabbedApplication(this Func<Form> createInitialForm)
    {
        return CreateTabbedApplication(createInitialForm, null);
    }

    /// <summary>
    /// Creates a TabbedApplication.
    /// </summary>
    /// <param name="createInitialForm">The Func that creates the initial form.</param>
    /// <param name="initialize">Initializes the form when created.</param>
    /// <returns>A TitleBarTabsApplicationContext</returns>
    public static ApplicationContext CreateTabbedApplication<T>(this Func<Form> createInitialForm, Func<T?, Task>? initialize)
        where T:Form
    {
        return CreateTabbedApplication(createInitialForm, null, initialize);
    }

    /// <summary>
    /// Creates a TabbedApplication.
    /// </summary>
    /// <param name="createInitialForm">The Func that creates the initial form.</param>
    /// <param name="createForm">The Func that creates the other forms.</param>
    /// <returns>A TitleBarTabsApplicationContext</returns>
    public static ApplicationContext CreateTabbedApplication(this Func<Form> createInitialForm, Func<Form>? createForm)
    {
        return CreateTabbedApplication(createInitialForm, createForm, null);
    }

    /// <summary>
    /// Creates a TabbedApplication.
    /// </summary>
    /// <param name="createInitialForm">The Func that creates the initial form.</param>
    /// <param name="createForm">The Func that creates the other forms.</param>
    /// <param name="initialize">Initializes the form when created.</param>
    /// <returns>A TitleBarTabsApplicationContext</returns>
    public static ApplicationContext CreateTabbedApplication(Func<Form> createInitialForm, Func<Form>? createForm, Func<Form?, Task>? initialize)
    {
        return CreateTabbedApplication<Form>(createInitialForm, createForm, initialize);
    }

    /// <summary>
    /// Creates a TabbedApplication.
    /// </summary>
    /// <param name="createInitialForm">The Func that creates the initial form.</param>
    /// <param name="createForm">The Func that creates the other forms.</param>
    /// <param name="initialize">Initializes the form when created.</param>
    /// <returns>A TitleBarTabsApplicationContext</returns>
    public static ApplicationContext CreateTabbedApplication<T>(Func<Form> createInitialForm, Func<Form>? createForm, Func<T?, Task>? initialize)
        where T : Form
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        createForm ??= createInitialForm;

        TitleBarTabs container = new TitleBarTabs();
        container.CreatingForm += async (_, e) =>
        {
            var eForm = createForm();
            e.Form = eForm;
            if (initialize != null)
            {
                await initialize.Invoke(eForm as T);
            }
        };

        // Add the initial Tab
        container.AddTab(createInitialForm());

        // Set initial tab the first one
        container.SelectedTabIndex = 0;

        // Create tabs and start application
        TitleBarTabsApplicationContext applicationContext = new TitleBarTabsApplicationContext();
        applicationContext.Start(container);
        return applicationContext;
    }
}