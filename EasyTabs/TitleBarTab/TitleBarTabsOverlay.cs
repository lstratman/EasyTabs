﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CoreLibrary.Extensions.WaitUtility;
using EasyTabs.Model;
using Win32Interop.Enums;
using Win32Interop.Methods;
using Win32Interop.Structs;
using Timer = System.Timers.Timer;

namespace EasyTabs;

/// <summary>
/// Border-less overlay window that is moved with and rendered on top of the non-client area of a  <see cref="TitleBarTabs" /> instance that's responsible
/// for rendering the actual tab content and responding to click events for those tabs.
/// </summary>
public class TitleBarTabsOverlay : Form
{
    /// <summary>
    /// The showTooltipTimer.
    /// </summary>
    protected Timer? showTooltipTimer;

    /// <summary>All of the parent forms and their overlays so that we don't create duplicate overlays across the application domain.</summary>
    protected static Dictionary<TitleBarTabs, TitleBarTabsOverlay?> _parents = new();

    /// <summary>Tab that has been torn off from this window and is being dragged.</summary>
    protected static TitleBarTab? _tornTab;

    /// <summary>Thumbnail representation of <see cref="_tornTab" /> used when dragging.</summary>
    protected static TornTabForm? _tornTabForm;

    /// <summary>
    /// Flag used in <see cref="WndProc" /> and <see cref="MouseHookCallback" /> to track whether the user was click/dragging when a particular event
    /// occurred.
    /// </summary>
    protected static bool _wasDragging;

    /// <summary>Flag indicating whether or not <see cref="_hookproc" /> has been installed as a hook.</summary>
    protected static bool _hookProcInstalled;

    /// <summary>Semaphore to control access to <see cref="_tornTab" />.</summary>
    protected static object _tornTabLock = new();

    /// <summary>
    /// The double click interval.
    /// </summary>
    protected static uint _doubleClickInterval = User32.GetDoubleClickTime();

    /// <summary>Flag indicating whether or not the underlying window is active.</summary>
    protected bool _active;

    /// <summary>Flag indicating whether we should draw the titlebar background (i.e. we are in a non-Aero environment).</summary>
    protected bool _aeroEnabled;

    /// <summary>
    /// When a tab is torn from the window, this is where we store the areas on all open windows where tabs can be dropped to combine the tab with that
    /// window.
    /// </summary>
    protected Tuple<TitleBarTabs, Rectangle>[]? _dropAreas;

    /// <summary>Pointer to the low-level mouse hook callback (<see cref="MouseHookCallback" />).</summary>
    protected IntPtr _hookId;

    /// <summary>Delegate of <see cref="MouseHookCallback" />; declared as a member variable to keep it from being garbage collected.</summary>
    protected HOOKPROC? _hookproc;

    /// <summary>Index of the tab, if any, whose close button is being hovered over.</summary>
    protected int _isOverCloseButtonForTab = -1;

    /// <summary>
    /// Says if is over sizing box.
    /// </summary>
    protected bool _isOverSizingBox;

    /// <summary>
    /// Says if is over add button.
    /// </summary>
    protected bool _isOverAddButton = true;

    /// <summary>Queue of mouse events reported by <see cref="_hookproc" /> that need to be processed.</summary>
    protected BlockingCollection<MouseEvent> _mouseEvents = new();

    /// <summary>Consumer thread for processing events in <see cref="_mouseEvents" />.</summary>
    protected Thread? _mouseEventsThread;

    /// <summary>Parent form for the overlay.</summary>
    protected TitleBarTabs? _parentForm;

    /// <summary>
    /// The last left button click ticks.
    /// </summary>
    protected long _lastLeftButtonClickTicks;

    /// <summary>
    /// Says if first click.
    /// </summary>
    protected bool _firstClick = true;

    /// <summary>
    /// The last two click coordinates.
    /// </summary>
    protected Point[] _lastTwoClickCoordinates = new Point[2];

    /// <summary>
    /// Says if parent form is closing.
    /// </summary>
    protected bool _parentFormClosing;

    /// <summary>Blank default constructor to ensure that the overlays are only initialized through <see cref="GetInstance" />.</summary>
    protected TitleBarTabsOverlay()
    {
    }

    /// <summary>Creates the overlay window and attaches it to <paramref name="parentForm" />.</summary>
    /// <param name="parentForm">Parent form that the overlay should be rendered on top of.</param>
    protected TitleBarTabsOverlay(TitleBarTabs? parentForm)
    {
        _parentForm = parentForm;

        // We don't want this window visible in the taskbar
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        MinimizeBox = false;
        MaximizeBox = false;
        _aeroEnabled = _parentForm?.IsCompositionEnabled??false;

        Show(_parentForm);
        AttachHandlers();

        showTooltipTimer = new Timer
        {
            AutoReset = false
        };

        showTooltipTimer.Elapsed += ShowTooltipTimer_Elapsed;
    }

    /// <summary>
    /// Makes sure that the window is created with an <see cref="WS_EX.WS_EX_LAYERED" /> flag set so that it can be alpha-blended properly with the content (
    /// <see cref="_parentForm" />) underneath the overlay.
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams createParams = base.CreateParams;
            createParams.ExStyle |= (int)(WS_EX.WS_EX_LAYERED | WS_EX.WS_EX_NOACTIVATE);

            return createParams;
        }
    }

    /// <summary>Primary color for the titlebar background.</summary>
    protected Color TitleBarColor
    {
        get
        {
            if (Application.RenderWithVisualStyles && Environment.OSVersion.Version.Major >= 6)
            {
                return _active
                    ? SystemColors.GradientActiveCaption
                    : SystemColors.GradientInactiveCaption;
            }

            return _active
                ? SystemColors.ActiveCaption
                : SystemColors.InactiveCaption;
        }
    }

    /// <summary>Type of theme being used by the OS to render the desktop.</summary>
    protected DisplayType DisplayType
    {
        get
        {
            if (_aeroEnabled)
            {
                return DisplayType.Aero;
            }

            if (Application.RenderWithVisualStyles && Environment.OSVersion.Version.Major >= 6)
            {
                return DisplayType.Basic;
            }

            return DisplayType.Classic;
        }
    }

    /// <summary>Gradient color for the titlebar background.</summary>
    protected Color TitleBarGradientColor =>
        _active
            ? SystemInformation.IsTitleBarGradientEnabled
                ? SystemColors.GradientActiveCaption
                : SystemColors.ActiveCaption
            : SystemInformation.IsTitleBarGradientEnabled
                ? SystemColors.GradientInactiveCaption
                : SystemColors.InactiveCaption;

    /// <summary>Screen area in which tabs can be dragged to and dropped for this window.</summary>
    public Rectangle TabDropArea
    {
        get
        {
            RECT windowRectangle;
            User32.GetWindowRect(_parentForm?.Handle??IntPtr.Zero, out windowRectangle);

            return new Rectangle(
                windowRectangle.left + SystemInformation.HorizontalResizeBorderThickness, windowRectangle.top + SystemInformation.VerticalResizeBorderThickness,
                ClientRectangle.Width, _parentForm?.NonClientAreaHeight??0 - SystemInformation.VerticalResizeBorderThickness);
        }
    }

    /// <summary>Retrieves or creates the overlay for <paramref name="parentForm" />.</summary>
    /// <param name="parentForm">Parent form that we are to create the overlay for.</param>
    /// <returns>Newly-created or previously existing overlay for <paramref name="parentForm" />.</returns>
    public static TitleBarTabsOverlay? GetInstance(TitleBarTabs? parentForm)
    {
        if (parentForm != null && !_parents.ContainsKey(parentForm))
        {
            _parents.Add(parentForm, new TitleBarTabsOverlay(parentForm));
        }

        if (parentForm != null)
        {
            return _parents[parentForm];
        }

        return null;
    }

    /// <summary>
    /// Attaches the various event handlers to <see cref="_parentForm" /> so that the overlay is moved in synchronization to
    /// <see cref="_parentForm" />.
    /// </summary>
    protected void AttachHandlers()
    {
        FormClosing += TitleBarTabsOverlay_FormClosing;

        if (_parentForm != null)
        {
            _parentForm.FormClosing += _parentForm_FormClosing;
            _parentForm.Disposed += _parentForm_Disposed;
            _parentForm.Deactivate += _parentForm_Deactivate;
            _parentForm.Activated += _parentForm_Activated;
            _parentForm.SizeChanged += _parentForm_Refresh;
            _parentForm.Shown += _parentForm_Refresh;
            _parentForm.VisibleChanged += _parentForm_Refresh;
            _parentForm.Move += _parentForm_Refresh;
            _parentForm.SystemColorsChanged += _parentForm_SystemColorsChanged;
        }

        if (_hookproc == null)
        {
            // Spin up a consumer thread to process mouse events from _mouseEvents
            _mouseEventsThread = new Thread(InterpretMouseEvents)
            {
                Name = "Low level mouse hooks processing thread",
                Priority = ThreadPriority.Highest
            };
            _mouseEventsThread.Start();

            using Process curProcess = Process.GetCurrentProcess();
            var curProcessMainModule = curProcess.MainModule;
            if (curProcessMainModule == null)
            {
                return;
            }

            using ProcessModule curModule = curProcessMainModule;
            // Install the low level mouse hook that will put events into _mouseEvents
            _hookproc = MouseHookCallback;
            _hookId = User32.SetWindowsHookEx(WH.WH_MOUSE_LL, _hookproc, Kernel32.GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private void TitleBarTabsOverlay_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_parentFormClosing)
        {
            e.Cancel = true;
            _parentFormClosing = true;
            _parentForm?.Close();
        }
    }

    /// <summary>
    /// Event handler that is called when <see cref="_parentForm" /> is in the process of closing.  This uninstalls <see cref="_hookproc" /> from the low-
    /// level hooks list and stops the consumer thread that processes those events.
    /// </summary>
    /// <param name="sender">Object from which this event originated, <see cref="_parentForm" /> in this case.</param>
    /// <param name="e">Arguments associated with this event.</param>
    private void _parentForm_FormClosing(object? sender, CancelEventArgs e)
    {
        if (e.Cancel)
        {
            _parentFormClosing = false;
            return;
        }

        TitleBarTabs? form = (TitleBarTabs?)sender;

        if (form == null)
        {
            return;
        }

        _parentFormClosing = true;

        if (_parents.ContainsKey(form))
        {
            _parents.Remove(form);
        }

        // Uninstall the mouse hook
        User32.UnhookWindowsHookEx(_hookId);

        // Kill the mouse events processing thread
        _mouseEvents.CompleteAdding();
        _mouseEventsThread?.Interrupt();
    }

    private void HideTooltip()
    {
        showTooltipTimer?.Stop();

        if (_parentForm != null && _parentForm.InvokeRequired)
        {
            _parentForm.Invoke(() =>
            {
                _parentForm.Tooltip.Hide(_parentForm);
            });
        }

        else
        {
            _parentForm?.Tooltip.Hide(_parentForm);
        }
    }

    private void ShowTooltip(TitleBarTabs? tabsForm, string caption)
    {
        Point tooltipLocation = new Point(Cursor.Position.X + 7, Cursor.Position.Y + 55);
        if (tabsForm != null)
        {
            tabsForm.Tooltip.Show(caption, tabsForm, tabsForm.PointToClient(tooltipLocation), tabsForm.Tooltip.AutoPopDelay);
        }
    }

    private void ShowTooltipTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (!_parentForm?.ShowTooltips??true)
        {
            return;
        }

        Point relativeCursorPosition = GetRelativeCursorPosition(Cursor.Position);
        TitleBarTab? hoverTab = _parentForm.TabRenderer?.OverTab(_parentForm.Tabs, relativeCursorPosition);

        if (hoverTab != null)
        {
            TitleBarTabs? hoverTabForm = hoverTab.Parent;

            if (hoverTabForm?.InvokeRequired??false)
            {
                hoverTabForm.Invoke(() =>
                {
                    ShowTooltip(hoverTabForm, hoverTab.Caption);
                });
            }

            else
            {
                ShowTooltip(hoverTabForm, hoverTab.Caption);
            }
        }
    }

    private void StartTooltipTimer()
    {
        if (!(_parentForm?.ShowTooltips??false))
        {
            return;
        }

        Point relativeCursorPosition = GetRelativeCursorPosition(Cursor.Position);
        TitleBarTab? hoverTab = _parentForm.TabRenderer?.OverTab(_parentForm.Tabs, relativeCursorPosition);

        if (hoverTab != null)
        {
            if (showTooltipTimer != null)
            {
                if (hoverTab.Parent != null)
                {
                    showTooltipTimer.Interval = hoverTab.Parent.Tooltip.AutomaticDelay;
                }

                showTooltipTimer.Start();
            }
        }
    }

    /// <summary>Consumer method that processes mouse events in <see cref="_mouseEvents" /> that are recorded by <see cref="MouseHookCallback" />.</summary>
    protected void InterpretMouseEvents()
    {
        foreach (MouseEvent mouseEvent in _mouseEvents.GetConsumingEnumerable())
        {
            int nCode = mouseEvent.nCode;
            IntPtr wParam = mouseEvent.wParam;
            MSLLHOOKSTRUCT? hookStruct = mouseEvent.MouseData;

            if (nCode >= 0 && (int)WM.WM_MOUSEMOVE == (int)wParam)
            {
                HideTooltip();

                // ReSharper disable PossibleInvalidOperationException
                if (hookStruct != null)
                {
                    Point cursorPosition = new Point(hookStruct.Value.pt.x, hookStruct.Value.pt.y);
                    // ReSharper restore PossibleInvalidOperationException
                    bool reRender = false;

                    if (_tornTab != null && _dropAreas != null)
                    {
                        // ReSharper disable ForCanBeConvertedToForeach
                        for (int i = 0; i < _dropAreas.Length; i++)
                            // ReSharper restore ForCanBeConvertedToForeach
                        {
                            // If the cursor is within the drop area, combine the tab for the window that belongs to that drop area
                            if (_dropAreas[i].Item2.Contains(cursorPosition))
                            {
                                TitleBarTab? tabToCombine = null;

                                lock (_tornTabLock)
                                {
                                    if (_tornTab != null)
                                    {
                                        tabToCombine = _tornTab;
                                        _tornTab = null;
                                    }
                                }

                                if (tabToCombine != null)
                                {
                                    int i1 = i;

                                    // In all cases where we need to affect the UI, we call Invoke so that those changes are made on the main UI thread since
                                    // we are on a separate processing thread in this case
                                    Invoke(
                                        () =>
                                        {
                                            _dropAreas[i1].Item1.TabRenderer?.CombineTab(tabToCombine, cursorPosition);

                                            tabToCombine = null;
                                            _tornTabForm?.Close();
                                            _tornTabForm = null;

                                            if ((_parentForm?.Tabs?.Count??0) == 0)
                                            {
                                                _parentForm?.Close();
                                            }
                                        });
                                }
                            }
                        }
                    }

                    else
                    {
                        var parentFormTabRenderer = _parentForm?.TabRenderer;
                        if (!parentFormTabRenderer?.IsTabRepositioning??false)
                        {
                            StartTooltipTimer();

                            Point relativeCursorPosition = GetRelativeCursorPosition(cursorPosition);

                            // If we were over a close button previously, check to see if the cursor is still over that tab's
                            // close button; if not, re-render
                            if (_parentForm != null &&
                                _isOverCloseButtonForTab != -1 &&
                                (_isOverCloseButtonForTab >= (_parentForm?.Tabs?.Count??0) ||
                                 !(_parentForm?.TabRenderer?.IsOverCloseButton(_parentForm.Tabs?[_isOverCloseButtonForTab], relativeCursorPosition)??true)))
                            {
                                reRender = true;
                                _isOverCloseButtonForTab = -1;
                            }

                            // Otherwise, see if any tabs' close button is being hovered over
                            else
                            {
                                // ReSharper disable ForCanBeConvertedToForeach
                                for (int i = 0; i < (_parentForm?.Tabs?.Count??0); i++)
                                    // ReSharper restore ForCanBeConvertedToForeach
                                {
                                    if (_parentForm != null && _parentForm.TabRenderer != null && _parentForm.TabRenderer.IsOverCloseButton(_parentForm.Tabs?[i], relativeCursorPosition))
                                    {
                                        _isOverCloseButtonForTab = i;
                                        reRender = true;

                                        break;
                                    }
                                }
                            }

                            if (_parentForm != null && _parentForm.TabRenderer != null && _isOverCloseButtonForTab == -1 && _parentForm.TabRenderer.RendersEntireTitleBar)
                            {
                                if (_parentForm.TabRenderer.IsOverSizingBox(relativeCursorPosition))
                                {
                                    _isOverSizingBox = true;
                                    reRender = true;
                                }

                                else if (_isOverSizingBox)
                                {
                                    _isOverSizingBox = false;
                                    reRender = true;
                                }
                            }

                            if (_parentForm != null && _parentForm.TabRenderer != null && _parentForm.TabRenderer.IsOverAddButton(relativeCursorPosition))
                            {
                                _isOverAddButton = true;
                                reRender = true;
                            }

                            else if (_isOverAddButton)
                            {
                                _isOverAddButton = false;
                                reRender = true;
                            }
                        }

                        else
                        {
                            Invoke(
                                () =>
                                {
                                    _wasDragging = true;

                                    // When determining if a tab has been torn from the window while dragging, we take the drop area for this window and inflate it by the
                                    // TabTearDragDistance setting
                                    Rectangle dragArea = TabDropArea;
                                    if (_parentForm?.TabRenderer != null)
                                    {
                                        dragArea.Inflate(_parentForm.TabRenderer.TabTearDragDistance, _parentForm.TabRenderer.TabTearDragDistance);

                                        // If the cursor is outside the tear area, tear it away from the current window
                                        if (!dragArea.Contains(cursorPosition) && _tornTab == null)
                                        {
                                            lock (_tornTabLock)
                                            {
                                                if (_tornTab == null)
                                                {
                                                    _parentForm.TabRenderer.IsTabRepositioning = false;

                                                    // Clear the event handler subscriptions from the tab and then create a thumbnail representation of it to use when dragging
                                                    _tornTab = _parentForm.SelectedTab;
                                                    _tornTab?.ClearSubscriptions();
                                                    _tornTabForm = new TornTabForm(_tornTab, _parentForm.TabRenderer);
                                                }
                                            }

                                            if (_tornTab != null)
                                            {
                                                _parentForm.SelectedTabIndex = _parentForm.Tabs != null && _parentForm.SelectedTabIndex == _parentForm.Tabs.Count - 1
                                                    ? _parentForm.SelectedTabIndex - 1
                                                    : _parentForm.SelectedTabIndex + 1;
                                                _parentForm.Tabs?.Remove(_tornTab);

                                                // If this tab was the only tab in the window, hide the parent window
                                                if (_parentForm.Tabs != null && _parentForm.Tabs.Count == 0)
                                                {
                                                    _parentForm.Hide();
                                                }

                                                _tornTabForm?.Show();
                                                _dropAreas = (from window in _parentForm.ApplicationContext?.OpenWindows.Where(w => w.Tabs.Count > 0)
                                                    select new Tuple<TitleBarTabs, Rectangle>(window, window.TabDropArea)).ToArray();
                                            }
                                        }
                                    }
                                });
                        }
                    }

                    Invoke(() => OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, cursorPosition.X, cursorPosition.Y, 0)));

                    if (_parentForm?.TabRenderer != null && _parentForm.TabRenderer.IsTabRepositioning)
                    {
                        reRender = true;
                    }

                    if (reRender)
                    {
                        Invoke(() => Render(cursorPosition, true));
                    }
                }
            }

            else if (nCode >= 0 && (int)WM.WM_LBUTTONDBLCLK == (int)wParam)
            {
                if (DesktopBounds.Contains(_lastTwoClickCoordinates[0]) && DesktopBounds.Contains(_lastTwoClickCoordinates[1]))
                {
                    Invoke(() =>
                    {
                        if (_parentForm != null)
                        {
                            _parentForm.WindowState = _parentForm.WindowState == FormWindowState.Maximized
                                ? FormWindowState.Normal
                                : FormWindowState.Maximized;
                        }
                    });
                }
            }

            else if (nCode >= 0 && (int)WM.WM_LBUTTONDOWN == (int)wParam)
            {
                if (!_firstClick)
                {
                    _lastTwoClickCoordinates[1] = _lastTwoClickCoordinates[0];
                }

                _lastTwoClickCoordinates[0] = Cursor.Position;

                _firstClick = false;
                _wasDragging = false;
            }

            else if (nCode >= 0 && (int)WM.WM_LBUTTONUP == (int)wParam)
            {
                // If we released the mouse button while we were dragging a torn tab, put that tab into a new window
                if (_tornTab != null)
                {
                    TitleBarTab? tabToRelease = null;

                    lock (_tornTabLock)
                    {
                        if (_tornTab != null)
                        {
                            tabToRelease = _tornTab;
                            _tornTab = null;
                        }
                    }

                    if (tabToRelease != null)
                    {
                        Invoke(
                            () =>
                            {
                                var type = _parentForm?.GetType();
                                if (type != null)
                                {
                                    TitleBarTabs? newWindow = (TitleBarTabs?)Activator.CreateInstance(type);

                                    // Set the initial window position and state properly
                                    if (newWindow?.WindowState == FormWindowState.Maximized)
                                    {
                                        Screen screen = Screen.AllScreens.First(s => s.WorkingArea.Contains(Cursor.Position));

                                        newWindow.StartPosition = FormStartPosition.Manual;
                                        newWindow.WindowState = FormWindowState.Normal;
                                        newWindow.Left = screen.WorkingArea.Left;
                                        newWindow.Top = screen.WorkingArea.Top;
                                        newWindow.Width = screen.WorkingArea.Width;
                                        newWindow.Height = screen.WorkingArea.Height;
                                    }

                                    else
                                    {
                                        if (newWindow != null)
                                        {
                                            newWindow.Left = Cursor.Position.X;
                                            newWindow.Top = Cursor.Position.Y;
                                        }
                                    }

                                    tabToRelease.Parent = newWindow;
                                    _parentForm?.ApplicationContext?.OpenWindow(newWindow);

                                    if (newWindow != null)
                                    {
                                        newWindow.Show();
                                        newWindow.Tabs.Add(tabToRelease);
                                        newWindow.SelectedTabIndex = 0;
                                        newWindow.ResizeTabContents();
                                    }
                                }

                                _tornTabForm?.Close();
                                _tornTabForm = null;

                                if (_parentForm?.Tabs?.Count == 0)
                                {
                                    _parentForm.Close();
                                }
                            });
                    }
                }

                Invoke(() =>
                {
                    try
                    {
                        OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, Cursor.Position.X, Cursor.Position.Y, 0));
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Trace.Write(e);
                    }
                });
            }
        }
    }

    /// <summary>Hook callback to process <see cref="WM.WM_MOUSEMOVE" /> messages to highlight/un-highlight the close button on each tab.</summary>
    /// <param name="nCode">The message being received.</param>
    /// <param name="wParam">Additional information about the message.</param>
    /// <param name="lParam">Additional information about the message.</param>
    /// <returns>A zero value if the procedure processes the message; a nonzero value if the procedure ignores the message.</returns>
    protected IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        MouseEvent mouseEvent = new MouseEvent
        {
            nCode = nCode,
            wParam = wParam,
            lParam = lParam
        };

        if (nCode >= 0 && (int)WM.WM_MOUSEMOVE == (int)wParam)
        {
            mouseEvent.MouseData = (MSLLHOOKSTRUCT?)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
        }

        _mouseEvents.Add(mouseEvent);

        if (nCode >= 0 && (int)WM.WM_LBUTTONDOWN == (int)wParam)
        {
            long currentTicks = DateTime.Now.Ticks;

            if (_lastLeftButtonClickTicks > 0 && currentTicks - _lastLeftButtonClickTicks < _doubleClickInterval * 10000)
            {
                _mouseEvents.Add(new MouseEvent
                {
                    nCode = nCode,
                    wParam = new IntPtr((int)WM.WM_LBUTTONDBLCLK),
                    lParam = lParam
                });
            }

            _lastLeftButtonClickTicks = currentTicks;
        }

        return User32.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    /// <summary>Draws the titlebar background behind the tabs if Aero glass is not enabled.</summary>
    /// <param name="graphics">Graphics context with which to draw the background.</param>
    protected virtual void DrawTitleBarBackground(Graphics graphics)
    {
        if (DisplayType == DisplayType.Aero)
        {
            return;
        }

        Rectangle fillArea;

        if (DisplayType == DisplayType.Basic)
        {
            fillArea = new Rectangle(
                new Point(
                    1, Top == 0
                        ? SystemInformation.CaptionHeight - 1
                        : SystemInformation.CaptionHeight + SystemInformation.VerticalResizeBorderThickness - (Top - _parentForm?.Top??0) - 1),
                new Size(Width - 2, _parentForm?.Padding.Top??0));
        }

        else
        {
            fillArea = new Rectangle(new Point(1, 0), new Size(Width - 2, Height - 1));
        }

        if (fillArea.Height <= 0)
        {
            return;
        }

        // Adjust the margin so that the gradient stops immediately prior to the control box in the titlebar
        int rightMargin = 3;

        if (_parentForm != null && _parentForm.ControlBox && _parentForm.MinimizeBox)
        {
            rightMargin += SystemInformation.CaptionButtonSize.Width;
        }

        if (_parentForm != null && _parentForm.ControlBox && _parentForm.MaximizeBox)
        {
            rightMargin += SystemInformation.CaptionButtonSize.Width;
        }

        if (_parentForm != null && _parentForm.ControlBox)
        {
            rightMargin += SystemInformation.CaptionButtonSize.Width;
        }

        LinearGradientBrush gradient = new LinearGradientBrush(
            new Point(24, 0), new Point(fillArea.Width - rightMargin + 1, 0), TitleBarColor, TitleBarGradientColor);

        using BufferedGraphics bufferedGraphics = BufferedGraphicsManager.Current.Allocate(graphics, fillArea);
        bufferedGraphics.Graphics.FillRectangle(new SolidBrush(TitleBarColor), fillArea);
        bufferedGraphics.Graphics.FillRectangle(
            new SolidBrush(TitleBarGradientColor),
            new Rectangle(new Point(fillArea.Location.X + fillArea.Width - rightMargin, fillArea.Location.Y), new Size(rightMargin, fillArea.Height)));
        bufferedGraphics.Graphics.FillRectangle(
            gradient, new Rectangle(fillArea.Location, new Size(fillArea.Width - rightMargin, fillArea.Height)));
        bufferedGraphics.Graphics.FillRectangle(new SolidBrush(TitleBarColor), new Rectangle(fillArea.Location, new Size(24, fillArea.Height)));

        bufferedGraphics.Render(graphics);
    }

    /// <summary>
    /// Event handler that is called when <see cref="_parentForm" />'s <see cref="Control.SystemColorsChanged" /> event is fired which re-renders
    /// the tabs.
    /// </summary>
    /// <param name="sender">Object from which the event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    private void _parentForm_SystemColorsChanged(object? sender, EventArgs e)
    {
        if (_parentForm != null)
        {
            _aeroEnabled = _parentForm.IsCompositionEnabled;
        }

        OnPosition();
    }

    /// <summary>
    /// Event handler that is called when <see cref="_parentForm" />'s <see cref="Control.SizeChanged" />, <see cref="Control.VisibleChanged" />, or
    /// <see cref="Control.Move" /> events are fired which re-renders the tabs.
    /// </summary>
    /// <param name="sender">Object from which the event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    private void _parentForm_Refresh(object? sender, EventArgs e)
    {
        if (_parentForm != null && _parentForm.WindowState == FormWindowState.Minimized)
        {
            Visible = false;
        }

        else
        {
            OnPosition();
        }
    }

    /// <summary>Sets the position of the overlay window to match that of <see cref="_parentForm" /> so that it moves in tandem with it.</summary>
    protected void OnPosition()
    {
        if (!IsDisposed)
        {
            // 92 is SM_CXPADDEDBORDER, which returns the amount of extra border padding around captioned windows
            int borderPadding = DisplayType == DisplayType.Classic
                ? 0
                : User32.GetSystemMetrics(92);

            // If the form is in a non-maximized state, we position the tabs below the minimize/maximize/close
            // buttons
            if (_parentForm != null)
            {
                Top = _parentForm.Top + (DisplayType == DisplayType.Classic
                    ? SystemInformation.VerticalResizeBorderThickness
                    : _parentForm.WindowState == FormWindowState.Maximized
                        ? SystemInformation.VerticalResizeBorderThickness + borderPadding
                        : _parentForm.TabRenderer != null && _parentForm.TabRenderer.RendersEntireTitleBar
                            ? _parentForm.TabRenderer.IsWindows10
                                ? SystemInformation.BorderSize.Width
                                : 0
                            : borderPadding);
                Left = _parentForm.Left + SystemInformation.HorizontalResizeBorderThickness - (_parentForm.TabRenderer != null && _parentForm.TabRenderer.IsWindows10
                    ? 0
                    : SystemInformation.BorderSize.Width) + borderPadding;
                Width = _parentForm.Width - (SystemInformation.VerticalResizeBorderThickness + borderPadding) * 2 + (_parentForm.TabRenderer != null && _parentForm.TabRenderer.IsWindows10
                    ? 0
                    : SystemInformation.BorderSize.Width * 2);
                if (_parentForm.TabRenderer != null)
                {
                    Height = _parentForm.TabRenderer.TabHeight + (DisplayType == DisplayType.Classic && _parentForm.WindowState != FormWindowState.Maximized &&
                                                                  !_parentForm.TabRenderer.RendersEntireTitleBar
                        ? SystemInformation.CaptionButtonSize.Height
                        : _parentForm.TabRenderer.IsWindows10
                            ? -1 * SystemInformation.BorderSize.Width
                            : _parentForm.WindowState != FormWindowState.Maximized
                                ? borderPadding
                                : 0);
                }
            }

            Render();
        }
    }

    /// <summary>
    /// Renders the tabs and then calls <see cref="User32.UpdateLayeredWindow" /> to blend the tab content with the underlying window (
    /// <see cref="_parentForm" />).
    /// </summary>
    /// <param name="forceRedraw">Flag indicating whether a full render should be forced.</param>
    public void Render(bool forceRedraw = false)
    {
        Render(Cursor.Position, forceRedraw);
    }

    /// <summary>
    /// Renders the tabs and then calls <see cref="User32.UpdateLayeredWindow" /> to blend the tab content with the underlying window (
    /// <see cref="_parentForm" />).
    /// </summary>
    /// <param name="cursorPosition">Current position of the cursor.</param>
    /// <param name="forceRedraw">Flag indicating whether a full render should be forced.</param>
    public void Render(Point cursorPosition, bool forceRedraw = false)
    {
        if (!IsDisposed && _parentForm?.TabRenderer != null && _parentForm.WindowState != FormWindowState.Minimized && _parentForm.ClientRectangle.Width > 0)
        {
            cursorPosition = GetRelativeCursorPosition(cursorPosition);

            using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    DrawTitleBarBackground(graphics);

                    // Since classic mode themes draw over the *entire* titlebar, not just the area immediately behind the tabs, we have to offset the tabs
                    // when rendering in the window
                    Point offset = _parentForm.WindowState != FormWindowState.Maximized && DisplayType == DisplayType.Classic && !_parentForm.TabRenderer.RendersEntireTitleBar
                        ? new Point(0, SystemInformation.CaptionButtonSize.Height)
                        : _parentForm.WindowState != FormWindowState.Maximized && !_parentForm.TabRenderer.RendersEntireTitleBar
                            ? new Point(0, SystemInformation.VerticalResizeBorderThickness - SystemInformation.BorderSize.Height)
                            : new Point(0, 0);

                    // Render the tabs into the bitmap
                    _parentForm.TabRenderer.Render(_parentForm.Tabs, graphics, offset, cursorPosition, forceRedraw);

                    // Cut out a hole in the background so that the control box on the underlying window can be shown
                    if (DisplayType == DisplayType.Classic && (_parentForm.ControlBox || _parentForm.MaximizeBox || _parentForm.MinimizeBox))
                    {
                        int boxWidth = 0;

                        if (_parentForm.ControlBox)
                        {
                            boxWidth += SystemInformation.CaptionButtonSize.Width;
                        }

                        if (_parentForm.MinimizeBox)
                        {
                            boxWidth += SystemInformation.CaptionButtonSize.Width;
                        }

                        if (_parentForm.MaximizeBox)
                        {
                            boxWidth += SystemInformation.CaptionButtonSize.Width;
                        }

                        CompositingMode oldCompositingMode = graphics.CompositingMode;

                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.FillRectangle(
                            new SolidBrush(Color.Transparent), Width - boxWidth, 0, boxWidth, SystemInformation.CaptionButtonSize.Height);
                        graphics.CompositingMode = oldCompositingMode;
                    }

                    IntPtr screenDc = User32.GetDC(IntPtr.Zero);
                    IntPtr memDc = Gdi32.CreateCompatibleDC(screenDc);
                    IntPtr oldBitmap = IntPtr.Zero;
                    IntPtr bitmapHandle = IntPtr.Zero;

                    try
                    {
                        // Copy the contents of the bitmap into memDc
                        bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
                        oldBitmap = Gdi32.SelectObject(memDc, bitmapHandle);

                        SIZE size = new SIZE
                        {
                            cx = bitmap.Width,
                            cy = bitmap.Height
                        };

                        POINT pointSource = new POINT
                        {
                            x = 0,
                            y = 0
                        };
                        POINT topPos = new POINT
                        {
                            x = Left,
                            y = Top
                        };
                        BLENDFUNCTION blend = new BLENDFUNCTION
                        {
                            // We want to blend the bitmap's content with the screen content under it
                            BlendOp = Convert.ToByte((int)AC.AC_SRC_OVER),
                            BlendFlags = 0,
                            // Follow the parent forms' opacity level
                            SourceConstantAlpha = (byte)(_parentForm.Opacity * 255),
                            // We use the bitmap's alpha channel for blending instead of a pre-defined transparency key
                            AlphaFormat = Convert.ToByte((int)AC.AC_SRC_ALPHA)
                        };

                        // Blend the tab content with the underlying content
                        if (!User32.UpdateLayeredWindow(
                                Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, ULW.ULW_ALPHA))
                        {
                            int error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error, "Error while calling UpdateLayeredWindow().");
                        }
                    }

                    // Clean up after ourselves
                    finally
                    {
                        User32.ReleaseDC(IntPtr.Zero, screenDc);

                        if (bitmapHandle != IntPtr.Zero)
                        {
                            Gdi32.SelectObject(memDc, oldBitmap);
                            Gdi32.DeleteObject(bitmapHandle);
                        }

                        Gdi32.DeleteDC(memDc);
                    }
                }
            }
        }
    }

    /// <summary>Gets the relative location of the cursor within the overlay.</summary>
    /// <param name="cursorPosition">Cursor position that represents the absolute position of the cursor on the screen.</param>
    /// <returns>The relative location of the cursor within the overlay.</returns>
    public Point GetRelativeCursorPosition(Point cursorPosition)
    {
        return new Point(cursorPosition.X - Location.X, cursorPosition.Y - Location.Y);
    }

    /// <summary>Overrides the message pump for the window so that we can respond to click events on the tabs themselves.</summary>
    /// <param name="m">Message received by the pump.</param>
    protected override void WndProc(ref Message m)
    {
        var formTabRenderer = _parentForm?.TabRenderer;
        switch ((WM)m.Msg)
        {
            case WM.WM_SYSCOMMAND:
                if (m.WParam == new IntPtr(0xF030) || m.WParam == new IntPtr(0xF120) || m.WParam == new IntPtr(0xF020))
                {
                    _parentForm?.ForwardMessage(ref m);
                }

                else
                {
                    base.WndProc(ref m);
                }

                break;

            case WM.WM_NCLBUTTONDOWN:
            case WM.WM_LBUTTONDOWN:
                Point relativeCursorPosition = GetRelativeCursorPosition(Cursor.Position);

                // If we were over a tab, set the capture state for the window so that we'll actually receive a WM_LBUTTONUP message
                var parentFormTabRenderer = _parentForm?.TabRenderer;
                var parentFormTabs = _parentForm?.Tabs;
                if (parentFormTabs != null &&
                    parentFormTabRenderer != null &&
                    parentFormTabRenderer.OverTab(parentFormTabs, relativeCursorPosition) == null &&
                    !parentFormTabRenderer.IsOverAddButton(relativeCursorPosition))
                {
                    _parentForm?.ForwardMessage(ref m);
                }

                else
                {
                    // When the user clicks a mouse button, save the tab that the user was over so we can respond properly when the mouse button is released
                    if (_parentForm is { TabRenderer: not null })
                    {
                        TitleBarTab? clickedTab = formTabRenderer?.OverTab(_parentForm.Tabs, relativeCursorPosition);

                        if (clickedTab != null)
                        {
                            // If the user clicked the close button, remove the tab from the list
                            if (formTabRenderer != null && !formTabRenderer.IsOverCloseButton(clickedTab, relativeCursorPosition))
                            {
                                _parentForm.ResizeTabContents(clickedTab);
                                _parentForm.SelectedTabIndex = _parentForm.Tabs.IndexOf(clickedTab);

                                Render();
                            }

                            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, Cursor.Position.X, Cursor.Position.Y, 0));
                        }
                    }

                    _parentForm?.Activate();
                }

                break;

            case WM.WM_LBUTTONDBLCLK:
                _parentForm?.ForwardMessage(ref m);
                break;

            // We always return HTCAPTION for the hit test message so that the underlying window doesn't have its focus removed
            case WM.WM_NCHITTEST:
                if (_parentForm != null)
                {
                    if (formTabRenderer != null)
                    {
                        m.Result = new IntPtr((int)formTabRenderer.NonClientHitTest(m, GetRelativeCursorPosition(Cursor.Position)));
                    }
                }

                break;

            case WM.WM_LBUTTONUP:
            case WM.WM_NCLBUTTONUP:
            case WM.WM_MBUTTONUP:
            case WM.WM_NCMBUTTONUP:
                Point relativeCursorPosition2 = GetRelativeCursorPosition(Cursor.Position);

                if (_parentForm != null && formTabRenderer != null &&
                    formTabRenderer.OverTab(_parentForm.Tabs, relativeCursorPosition2) == null &&
                    !formTabRenderer.IsOverAddButton(relativeCursorPosition2))
                {
                    _parentForm.ForwardMessage(ref m);
                }

                else
                {
                    // When the user clicks a mouse button, save the tab that the user was over so we can respond properly when the mouse button is released
                    if (_parentForm != null)
                    {
                        TitleBarTab? clickedTab = formTabRenderer?.OverTab(_parentForm.Tabs, relativeCursorPosition2);

                        if (clickedTab != null)
                        {
                            // If the user clicks the middle button/scroll wheel over a tab, close it
                            if ((WM)m.Msg == WM.WM_MBUTTONUP || (WM)m.Msg == WM.WM_NCMBUTTONUP)
                            {
                                clickedTab.Content?.Close();
                                Render();
                            }

                            else
                            {
                                // If the user clicked the close button, remove the tab from the list
                                if (formTabRenderer != null && formTabRenderer.IsOverCloseButton(clickedTab, relativeCursorPosition2))
                                {
                                    clickedTab.Content?.Close();
                                    Render();
                                }

                                else
                                {
                                    _parentForm.OnTabClicked(
                                        new TitleBarTabEventArgs
                                        {
                                            Tab = clickedTab,
                                            TabIndex = _parentForm.SelectedTabIndex,
                                            Action = TabControlAction.Selected,
                                            WasDragging = _wasDragging
                                        });
                                }
                            }
                        }

                        // Otherwise, if the user clicked the add button, call CreateTab to add a new tab to the list and select it
                        else if (formTabRenderer != null && formTabRenderer.IsOverAddButton(relativeCursorPosition2))
                        {
                            _parentForm.AddNewTab().WaitForTask();
                        }
                    }

                    if ((WM)m.Msg == WM.WM_LBUTTONUP || (WM)m.Msg == WM.WM_NCLBUTTONUP)
                    {
                        OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, Cursor.Position.X, Cursor.Position.Y, 0));
                    }
                }

                break;

            default:
                base.WndProc(ref m);
                break;
        }
    }

    /// <summary>Event handler that is called when <see cref="_parentForm" />'s <see cref="Form.Activated" /> event is fired.</summary>
    /// <param name="sender">Object from which this event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    private void _parentForm_Activated(object? sender, EventArgs e)
    {
        _active = true;
        Render();
    }

    /// <summary>Event handler that is called when <see cref="_parentForm" />'s <see cref="Form.Deactivate" /> event is fired.</summary>
    /// <param name="sender">Object from which this event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    private void _parentForm_Deactivate(object? sender, EventArgs e)
    {
        _active = false;
        Render();
    }

    /// <summary>Event handler that is called when <see cref="_parentForm" />'s <see cref="Component.Disposed" /> event is fired.</summary>
    /// <param name="sender">Object from which this event originated.</param>
    /// <param name="e">Arguments associated with the event.</param>
    private void _parentForm_Disposed(object? sender, EventArgs e)
    {
    }

    /// <summary>
    /// Contains information on mouse events captured by <see cref="MouseHookCallback" /> and processed by
    /// <see cref="InterpretMouseEvents" />.
    /// </summary>
    protected class MouseEvent
    {
        /// <summary>Code for the event.</summary>
        // ReSharper disable InconsistentNaming
        public int nCode
        {
            get;
            set;
        }

        /// <summary>wParam value associated with the event.</summary>
        public IntPtr wParam
        {
            get;
            set;
        }

        /// <summary>lParam value associated with the event.</summary>
        public IntPtr lParam
        {
            get;
            set;
        }

        // ReSharper restore InconsistentNaming

        /// <summary>Data associated with the mouse event.</summary>
        public MSLLHOOKSTRUCT? MouseData
        {
            get;
            set;
        }
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        // 
        // TitleBarTabsOverlay
        // 
        ClientSize = new Size(284, 261);
        Name = "TitleBarTabsOverlay";
        ResumeLayout(false);

    }
}