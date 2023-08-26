using System;
using System.ComponentModel;
using System.Windows.Forms;
using EasyTabs.Model;
using Win32Interop.Methods;

namespace EasyTabs;

internal class ParentFormHelper
{
    private readonly TitleBarTabsOverlay _parent;

    public ParentFormHelper(TitleBarTabsOverlay parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Event handler that is called when <see cref="_parent.ParentFormValue" /> is in the process of closing.  This uninstalls <see cref="_hookproc" /> from the low-
    /// level hooks list and stops the consumer thread that processes those events.
    /// </summary>
    /// <param name="sender">Object from which this event originated, <see cref="_parent.ParentFormValue" /> in this case.</param>
    /// <param name="e">Arguments associated with this event.</param>
    public void OnParentFormOnFormClosing(object? sender, CancelEventArgs e)
    {
        if (e.Cancel)
        {
            _parent.ParentFormClosing = false;
            return;
        }

        TitleBarTabs? form = (TitleBarTabs?)sender;

        if (form == null)
        {
            return;
        }

        _parent.ParentFormClosing = true;

        if (TitleBarTabsOverlay.Parents.ContainsKey(form))
        {
            TitleBarTabsOverlay.Parents.Remove(form);
        }

        // Uninstall the mouse hook
        User32.UnhookWindowsHookEx(_parent.HookId);

        // Kill the mouse events processing thread
        _parent.MouseEvents.CompleteAdding();
        _parent.MouseEventsThread?.Interrupt();
    }

    /// <summary>
    /// Event handler that is called when <see cref="_parent.ParentFormValue" />'s <see cref="Control.SystemColorsChanged" /> event is fired which re-renders
    /// the tabs.
    /// </summary>
    /// <param name="sender">Object from which the event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    public void OnParentFormOnSystemColorsChanged(object? sender, EventArgs e)
    {
        if (_parent.ParentFormValue != null)
        {
            _parent.AeroEnabled = _parent.ParentFormValue.IsCompositionEnabled;
        }

        OnPosition();
    }

    /// <summary>
    /// Event handler that is called when <see cref="_parent.ParentFormValue" />'s <see cref="Control.SizeChanged" />, <see cref="Control.VisibleChanged" />, or
    /// <see cref="Control.Move" /> events are fired which re-renders the tabs.
    /// </summary>
    /// <param name="sender">Object from which the event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    public void OnParentFormOnRefresh(object? sender, EventArgs e)
    {
        if (_parent.ParentFormValue != null && _parent.ParentFormValue.WindowState == FormWindowState.Minimized)
        {
            _parent.Visible = false;
        }

        else
        {
            OnPosition();
        }
    }

    /// <summary>Event handler that is called when <see cref="_parent.ParentFormValue" />'s <see cref="Form.Activated" /> event is fired.</summary>
    /// <param name="sender">Object from which this event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    public void OnParentFormOnActivated(object? sender, EventArgs e)
    {
        _parent.Active = true;
        _parent.Render();
    }

    /// <summary>Event handler that is called when <see cref="_parent.ParentFormValue" />'s <see cref="Form.Deactivate" /> event is fired.</summary>
    /// <param name="sender">Object from which this event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    public void OnParentFormOnDeactivate(object? sender, EventArgs e)
    {
        _parent.Active = false;
        _parent.Render();
    }

    /// <summary>Event handler that is called when <see cref="_parent.ParentFormValue" />'s <see cref="Component.Disposed" /> event is fired.</summary>
    /// <param name="sender">Object from which this event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    public void OnParentFormOnDisposed(object? sender, EventArgs e)
    {
    }

    /// <summary>Sets the position of the overlay window to match that of <see cref="_parent.ParentFormValue" /> so that it moves in tandem with it.</summary>
    protected void OnPosition()
    {
        if (!_parent.IsDisposed)
        {
            // 92 is SM_CXPADDEDBORDER, which returns the amount of extra border padding around captioned windows
            int borderPadding = _parent.DisplayType == DisplayType.Classic
                ? 0
                : User32.GetSystemMetrics(92);

            // If the form is in a non-maximized state, we position the tabs below the minimize/maximize/close
            // buttons
            if (_parent.ParentFormValue != null)
            {
                _parent.Top = _parent.ParentFormValue.Top + (_parent.DisplayType == DisplayType.Classic
                    ? SystemInformation.VerticalResizeBorderThickness
                    : _parent.ParentFormValue.WindowState == FormWindowState.Maximized
                        ? SystemInformation.VerticalResizeBorderThickness + borderPadding
                        : _parent.ParentFormValue.TabRenderer != null && _parent.ParentFormValue.TabRenderer.RendersEntireTitleBar
                            ? _parent.ParentFormValue.TabRenderer.IsWindows10
                                ? SystemInformation.BorderSize.Width
                                : 0
                            : borderPadding);
                _parent.Left = _parent.ParentFormValue.Left + SystemInformation.HorizontalResizeBorderThickness - (_parent.ParentFormValue.TabRenderer != null && _parent.ParentFormValue.TabRenderer.IsWindows10
                    ? 0
                    : SystemInformation.BorderSize.Width) + borderPadding;
                _parent.Width = _parent.ParentFormValue.Width - (SystemInformation.VerticalResizeBorderThickness + borderPadding) * 2 + (_parent.ParentFormValue.TabRenderer != null && _parent.ParentFormValue.TabRenderer.IsWindows10
                    ? 0
                    : SystemInformation.BorderSize.Width * 2);
                if (_parent.ParentFormValue.TabRenderer != null)
                {
                    _parent.Height = _parent.ParentFormValue.TabRenderer.TabHeight + (_parent.DisplayType == DisplayType.Classic && _parent.ParentFormValue.WindowState != FormWindowState.Maximized &&
                                                                                      !_parent.ParentFormValue.TabRenderer.RendersEntireTitleBar
                        ? SystemInformation.CaptionButtonSize.Height
                        : _parent.ParentFormValue.TabRenderer.IsWindows10
                            ? -1 * SystemInformation.BorderSize.Width
                            : _parent.ParentFormValue.WindowState != FormWindowState.Maximized
                                ? borderPadding
                                : 0);
                }
            }

            _parent.Render();
        }
    }


}