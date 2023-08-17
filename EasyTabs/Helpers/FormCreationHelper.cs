using System;
using System.Threading;
using System.Windows.Forms;

namespace EasyTabs;

/// <summary>
/// FormCreationHelper class.
/// </summary>
public static class FormCreationHelper
{
    /// <summary>
    /// Creates a Form in other thread
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The created form.</returns>
    public static Form CreateFormInOtherThread(string text)
    {
        return CreateFormInOtherThread(text, null, null);
    }

    /// <summary>
    /// Creates a Form in other thread
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="createForm">A function that creates a new Form</param>
    /// <returns>The created form.</returns>
    public static Form CreateFormInOtherThread(string text, Func<Form>? createForm)
    {
        return CreateFormInOtherThread(text, null, createForm);
    }

    /// <summary>
    /// Creates a Form in other thread
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="workWithForm">The action that is called on the created form.</param>
    /// <returns>The created form.</returns>
    public static Form CreateFormInOtherThread(string text, Action<Form>? workWithForm)
    {
        return CreateFormInOtherThread(text, workWithForm, null);
    }

    /// <summary>
    /// Creates a Form in other thread
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="workWithForm">The action that is called on the created form.</param>
    /// <param name="createForm">A function that creates a new Form</param>
    /// <returns>The created form.</returns>
    public static Form CreateFormInOtherThread(string text, Action<Form>? workWithForm, Func<Form>? createForm)
    {
        Form? form = null;
        var thread = new Thread(
            () =>
            {
                form = (createForm ?? new Func<Form>(() => new Form())).Invoke();
                form.TopLevel = false;
                form.Text = text;
                workWithForm?.Invoke(form);
                form.FormBorderStyle = FormBorderStyle.None;
                form.ShowInTaskbar = false;
                form.WindowState = FormWindowState.Minimized;
                Application.Run(form);
            });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        while (form == null)
        {
            Thread.Sleep(10);
        }

        return form;
    }
}