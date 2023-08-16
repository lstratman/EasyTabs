using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyTabs;

/// <summary>
/// Extension methods for form.
/// </summary>
public static class FormExtension
{

    /// <summary>
    /// Hosts a form in a parent form.
    /// </summary>
    /// <param name="parentForm">The parent form.</param>
    /// <param name="getOtherThreadForm">A function that returns the other thread form</param>
    public static async Task HostFormInParentForm(this Form parentForm, Func<Form> getOtherThreadForm)
    {
        var tuple = GetForm(getOtherThreadForm);

        if (await tuple.Form.IsCurrentThreadForm())
        {
            tuple.Form.TopLevel = false;
            tuple.Form.Dock = DockStyle.Fill;
            tuple.Form.FormBorderStyle = FormBorderStyle.None;
            parentForm.Closed += (_, _) =>
            {
                tuple.Form.Dispose();
            };
            parentForm.Controls.Add(tuple.Form);
            tuple.Form.BringToFront();
            return;
        }

        while (!parentForm.IsHandleCreated)
        {
            await TaskEx.Delay(10);
        }
        parentForm.Invoke(
            () =>
            {
                SetParent(tuple.Handle, parentForm.Handle); // Set the process parent window to the window we want
                SetWindowPos(
                    tuple.Handle, 0, 0, 0, parentForm.Width, parentForm.Height,
                    0x0001 | 0x0040); // Place the window in the top left of the parent window without resizing it
                var clientSizeWidth = parentForm.ClientSize.Width;
                var clientSizeHeight = parentForm.ClientSize.Height;
                tuple.Form.Invoke(
                    () =>
                    {
                        tuple.Form.WindowState= FormWindowState.Normal;
                        tuple.Form.Width = clientSizeWidth;
                        tuple.Form.Height = clientSizeHeight;
                    });
                parentForm.Closed += (_, _) =>
                {
                    tuple.Form.Invoke(
                        () =>
                        {
                            tuple.Form.Dispose();
                        });
                };
                parentForm.SizeChanged += (_, _) =>
                {
                    var width = parentForm.ClientSize.Width;
                    var height = parentForm.ClientSize.Height;
                    tuple.Form.Invoke(
                        () =>
                        {
                            tuple.Form.Width = width;
                            tuple.Form.Height = height;
                        });
                };
            });
    }

    /// <summary>
    /// Hosts a form in a parent form.
    /// </summary>
    /// <param name="parentForm">The parent form.</param>
    /// <param name="form">The form to be hosted.</param>
    public static Task HostFormInParentForm(this Form parentForm, Form form)
    {
        return HostFormInParentForm(parentForm, () => form);
    }

    /// <summary>
    /// Checks if the control was created in current thread or not.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <returns>The information telling if the control was created in current thread or not.</returns>
    public static async Task<bool> IsCurrentThreadForm(this Control control)
    {
        var currentThread = Thread.CurrentThread;
        Thread? controlThread = null;
        while (!control.IsHandleCreated)
        {
            await TaskEx.Delay(10);
        }
        control.Invoke(
            () =>
            {
                controlThread = Thread.CurrentThread;
            });
        return currentThread == controlThread;
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

    private static (IntPtr Handle, Form Form) GetForm(Func<Form> getOtherThreadForm)
    {
        var otherThreadForm = getOtherThreadForm();

        while (otherThreadForm == null || !otherThreadForm.IsHandleCreated)
        {
            Thread.Sleep(10);
        }

        bool flag = true;
        otherThreadForm.Invoke(
            () =>
            {
                flag = otherThreadForm.Handle == IntPtr.Zero;
            });
        while (flag)
        {
            Thread.Sleep(10);
            otherThreadForm.Invoke(
                () =>
                {
                    flag = otherThreadForm.Handle == IntPtr.Zero;
                });
        }

        IntPtr handle = IntPtr.Zero;
        otherThreadForm.Invoke(
            () =>
            {
                handle = otherThreadForm.Handle;
            });
        return (handle, otherThreadForm);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
}