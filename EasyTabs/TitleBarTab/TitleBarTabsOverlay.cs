using System;
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
public partial class TitleBarTabsOverlay : Form
{
    /// <summary>
    /// The showTooltipTimer.
    /// </summary>
    protected Timer? showTooltipTimer;

    /// <summary>All of the parent forms and their overlays so that we don't create duplicate overlays across the application domain.</summary>
    protected internal static Dictionary<TitleBarTabs, TitleBarTabsOverlay?> Parents
    {
        get;
    } = new();

    /// <summary>
    /// Flag used in <see cref="WndProc" /> and <see cref="MouseHookCallback" /> to track whether the user was click/dragging when a particular event
    /// occurred.
    /// </summary>
    protected internal static bool WasDragging
    {
        get;
        set;
    }

    /// <summary>Flag indicating whether or not <see cref="_hookproc" /> has been installed as a hook.</summary>
    protected static bool _hookProcInstalled;

    /// <summary>
    /// The double click interval.
    /// </summary>
    protected static uint _doubleClickInterval = User32.GetDoubleClickTime();

    /// <summary>Flag indicating whether or not the underlying window is active.</summary>
    protected internal bool Active
    {
        get;
        set;
    }

    /// <summary>Flag indicating whether we should draw the titlebar background (i.e. we are in a non-Aero environment).</summary>
    protected internal bool AeroEnabled
    {
        get;
        set;
    }

    /// <summary>
    /// When a tab is torn from the window, this is where we store the areas on all open windows where tabs can be dropped to combine the tab with that
    /// window.
    /// </summary>
    protected internal Tuple<TitleBarTabs, Rectangle>[]? DropAreas
    {
        get;
        set;
    }

    /// <summary>Pointer to the low-level mouse hook callback (<see cref="MouseHookCallback" />).</summary>
    protected internal IntPtr HookId
    {
        get;
        set;
    }

    /// <summary>Delegate of <see cref="MouseHookCallback" />; declared as a member variable to keep it from being garbage collected.</summary>
    protected HOOKPROC? _hookproc;

    /// <summary>Index of the tab, if any, whose close button is being hovered over.</summary>
    protected internal int IsOverCloseButtonForTab
    {
        get;
        set;
    } = -1;

    /// <summary>
    /// Says if is over sizing box.
    /// </summary>
    protected internal bool IsOverSizingBox
    {
        get;
        set;
    }

    /// <summary>
    /// Says if is over add button.
    /// </summary>
    protected internal bool IsOverAddButton
    {
        get;
        set;
    } = true;

    /// <summary>Queue of mouse events reported by <see cref="_hookproc" /> that need to be processed.</summary>
    protected internal BlockingCollection<MouseEvent> MouseEvents
    {
        get;
    } = new();

    /// <summary>Consumer thread for processing events in <see cref="MouseEvents" />.</summary>
    protected internal Thread? MouseEventsThread
    {
        get;
        set;
    }

    /// <summary>Parent form for the overlay.</summary>
    protected internal TitleBarTabs? ParentFormValue
    {
        get;
    }

    /// <summary>
    /// The last left button click ticks.
    /// </summary>
    protected long _lastLeftButtonClickTicks;

    /// <summary>
    /// Says if first click.
    /// </summary>
    protected internal bool FirstClick
    {
        get;
        set;
    } = true;

    /// <summary>
    /// The last two click coordinates.
    /// </summary>
    protected internal Point[] LastTwoClickCoordinates
    {
        get;
    } = new Point[2];

    /// <summary>
    /// Says if parent form is closing.
    /// </summary>
    protected internal bool ParentFormClosing
    {
        get;
        set;
    }

    /// <summary>Blank default constructor to ensure that the overlays are only initialized through <see cref="GetInstance" />.</summary>
    protected TitleBarTabsOverlay()
    {
    }

    /// <summary>Creates the overlay window and attaches it to <paramref name="parentForm" />.</summary>
    /// <param name="parentForm">Parent form that the overlay should be rendered on top of.</param>
    protected TitleBarTabsOverlay(TitleBarTabs? parentForm)
    {
        ParentFormValue = parentForm;

        // We don't want this window visible in the taskbar
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        MinimizeBox = false;
        MaximizeBox = false;
        AeroEnabled = ParentFormValue?.IsCompositionEnabled??false;

        Show(ParentFormValue);
        AttachHandlers();

        showTooltipTimer = new Timer
        {
            AutoReset = false
        };

        showTooltipTimer.Elapsed += ShowTooltipTimerOnElapsed;
    }

    /// <summary>
    /// Makes sure that the window is created with an <see cref="WS_EX.WS_EX_LAYERED" /> flag set so that it can be alpha-blended properly with the content (
    /// <see cref="ParentFormValue" />) underneath the overlay.
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
                return Active
                    ? SystemColors.GradientActiveCaption
                    : SystemColors.GradientInactiveCaption;
            }

            return Active
                ? SystemColors.ActiveCaption
                : SystemColors.InactiveCaption;
        }
    }

    /// <summary>Type of theme being used by the OS to render the desktop.</summary>
    protected internal DisplayType DisplayType
    {
        get
        {
            if (AeroEnabled)
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
        Active
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
            User32.GetWindowRect(ParentFormValue?.Handle??IntPtr.Zero, out windowRectangle);

            return new Rectangle(
                windowRectangle.left + SystemInformation.HorizontalResizeBorderThickness, windowRectangle.top + SystemInformation.VerticalResizeBorderThickness,
                ClientRectangle.Width, ParentFormValue?.NonClientAreaHeight??0 - SystemInformation.VerticalResizeBorderThickness);
        }
    }

    /// <summary>Retrieves or creates the overlay for <paramref name="parentForm" />.</summary>
    /// <param name="parentForm">Parent form that we are to create the overlay for.</param>
    /// <returns>Newly-created or previously existing overlay for <paramref name="parentForm" />.</returns>
    public static TitleBarTabsOverlay? GetInstance(TitleBarTabs? parentForm)
    {
        if (parentForm != null && !Parents.ContainsKey(parentForm))
        {
            Parents.Add(parentForm, new TitleBarTabsOverlay(parentForm));
        }

        if (parentForm != null)
        {
            return Parents[parentForm];
        }

        return null;
    }

    /// <summary>
    /// Attaches the various event handlers to <see cref="ParentFormValue" /> so that the overlay is moved in synchronization to
    /// <see cref="ParentFormValue" />.
    /// </summary>
    protected void AttachHandlers()
    {
        FormClosing += TitleBarTabsOverlay_FormClosing;

        if (ParentFormValue != null)
        {
            var onParentFormHelper = new ParentFormHelper(this);
            ParentFormValue.FormClosing += onParentFormHelper.OnParentFormOnFormClosing;
            ParentFormValue.Disposed += onParentFormHelper.OnParentFormOnDisposed;
            ParentFormValue.Deactivate += onParentFormHelper.OnParentFormOnDeactivate;
            ParentFormValue.Activated += onParentFormHelper.OnParentFormOnActivated;
            ParentFormValue.SizeChanged += onParentFormHelper.OnParentFormOnRefresh;
            ParentFormValue.Shown += onParentFormHelper.OnParentFormOnRefresh;
            ParentFormValue.VisibleChanged += onParentFormHelper.OnParentFormOnRefresh;
            ParentFormValue.Move += onParentFormHelper.OnParentFormOnRefresh;
            ParentFormValue.SystemColorsChanged += onParentFormHelper.OnParentFormOnSystemColorsChanged;
        }

        if (_hookproc == null)
        {
            MouseEventsHelper.ProcessMouseEvents(this);

            using Process curProcess = Process.GetCurrentProcess();
            var curProcessMainModule = curProcess.MainModule;
            if (curProcessMainModule == null)
            {
                return;
            }

            using ProcessModule curModule = curProcessMainModule;
            // Install the low level mouse hook that will put events into _mouseEvents
            _hookproc = MouseHookCallback;
            HookId = User32.SetWindowsHookEx(WH.WH_MOUSE_LL, _hookproc, Kernel32.GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private void TitleBarTabsOverlay_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!ParentFormClosing)
        {
            e.Cancel = true;
            ParentFormClosing = true;
            ParentFormValue?.Close();
        }
    }

    internal void HideTooltip()
    {
        showTooltipTimer?.Stop();

        if (ParentFormValue != null && ParentFormValue.InvokeRequired)
        {
            ParentFormValue.Invoke(() =>
            {
                ParentFormValue.Tooltip.Hide(ParentFormValue);
            });
        }

        else
        {
            ParentFormValue?.Tooltip.Hide(ParentFormValue);
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

    private void ShowTooltipTimerOnElapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (!ParentFormValue?.ShowTooltips??true)
        {
            return;
        }

        Point relativeCursorPosition = GetRelativeCursorPosition(Cursor.Position);
        TitleBarTab? hoverTab = ParentFormValue.TabRenderer?.OverTab(ParentFormValue.Tabs, relativeCursorPosition);

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

    internal void StartTooltipTimer()
    {
        if (!(ParentFormValue?.ShowTooltips??false))
        {
            return;
        }

        Point relativeCursorPosition = GetRelativeCursorPosition(Cursor.Position);
        TitleBarTab? hoverTab = ParentFormValue.TabRenderer?.OverTab(ParentFormValue.Tabs, relativeCursorPosition);

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

    /// <summary>Hook callback to process <see cref="WM.WM_MOUSEMOVE" /> messages to highlight/un-highlight the close button on each tab.</summary>
    /// <param name="nCode">The message being received.</param>
    /// <param name="wParam">Additional information about the message.</param>
    /// <param name="lParam">Additional information about the message.</param>
    /// <returns>A zero value if the procedure processes the message; a nonzero value if the procedure ignores the message.</returns>
    protected IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        MouseEvent mouseEvent = new MouseEvent
        {
            NumericCode = nCode,
            WideParam = wParam,
            LongParam = lParam
        };

        if (nCode >= 0 && (int)WM.WM_MOUSEMOVE == (int)wParam)
        {
            mouseEvent.MouseData = (MSLLHOOKSTRUCT?)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
        }

        MouseEvents.Add(mouseEvent);

        if (nCode >= 0 && (int)WM.WM_LBUTTONDOWN == (int)wParam)
        {
            long currentTicks = DateTime.Now.Ticks;

            if (_lastLeftButtonClickTicks > 0 && currentTicks - _lastLeftButtonClickTicks < _doubleClickInterval * 10000)
            {
                MouseEvents.Add(new MouseEvent
                {
                    NumericCode = nCode,
                    WideParam = new IntPtr((int)WM.WM_LBUTTONDBLCLK),
                    LongParam = lParam
                });
            }

            _lastLeftButtonClickTicks = currentTicks;
        }

        return User32.CallNextHookEx(HookId, nCode, wParam, lParam);
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
                        : SystemInformation.CaptionHeight + SystemInformation.VerticalResizeBorderThickness - (Top - ParentFormValue?.Top??0) - 1),
                new Size(Width - 2, ParentFormValue?.Padding.Top??0));
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

        if (ParentFormValue != null && ParentFormValue.ControlBox && ParentFormValue.MinimizeBox)
        {
            rightMargin += SystemInformation.CaptionButtonSize.Width;
        }

        if (ParentFormValue != null && ParentFormValue.ControlBox && ParentFormValue.MaximizeBox)
        {
            rightMargin += SystemInformation.CaptionButtonSize.Width;
        }

        if (ParentFormValue != null && ParentFormValue.ControlBox)
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
    /// Renders the tabs and then calls <see cref="User32.UpdateLayeredWindow" /> to blend the tab content with the underlying window (
    /// <see cref="ParentFormValue" />).
    /// </summary>
    /// <param name="forceRedraw">Flag indicating whether a full render should be forced.</param>
    public void Render(bool forceRedraw = false)
    {
        Render(Cursor.Position, forceRedraw);
    }

    /// <summary>
    /// Renders the tabs and then calls <see cref="User32.UpdateLayeredWindow" /> to blend the tab content with the underlying window (
    /// <see cref="ParentFormValue" />).
    /// </summary>
    /// <param name="cursorPosition">Current position of the cursor.</param>
    /// <param name="forceRedraw">Flag indicating whether a full render should be forced.</param>
    public void Render(Point cursorPosition, bool forceRedraw = false)
    {
        if (!IsDisposed && ParentFormValue?.TabRenderer != null && ParentFormValue.WindowState != FormWindowState.Minimized && ParentFormValue.ClientRectangle.Width > 0)
        {
            cursorPosition = GetRelativeCursorPosition(cursorPosition);

            using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    DrawTitleBarBackground(graphics);

                    // Since classic mode themes draw over the *entire* titlebar, not just the area immediately behind the tabs, we have to offset the tabs
                    // when rendering in the window
                    Point offset = ParentFormValue.WindowState != FormWindowState.Maximized && DisplayType == DisplayType.Classic && !ParentFormValue.TabRenderer.RendersEntireTitleBar
                        ? new Point(0, SystemInformation.CaptionButtonSize.Height)
                        : ParentFormValue.WindowState != FormWindowState.Maximized && !ParentFormValue.TabRenderer.RendersEntireTitleBar
                            ? new Point(0, SystemInformation.VerticalResizeBorderThickness - SystemInformation.BorderSize.Height)
                            : new Point(0, 0);

                    // Render the tabs into the bitmap
                    ParentFormValue.TabRenderer.Render(ParentFormValue.Tabs, graphics, offset, cursorPosition, forceRedraw);

                    // Cut out a hole in the background so that the control box on the underlying window can be shown
                    if (DisplayType == DisplayType.Classic && (ParentFormValue.ControlBox || ParentFormValue.MaximizeBox || ParentFormValue.MinimizeBox))
                    {
                        int boxWidth = 0;

                        if (ParentFormValue.ControlBox)
                        {
                            boxWidth += SystemInformation.CaptionButtonSize.Width;
                        }

                        if (ParentFormValue.MinimizeBox)
                        {
                            boxWidth += SystemInformation.CaptionButtonSize.Width;
                        }

                        if (ParentFormValue.MaximizeBox)
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
                            SourceConstantAlpha = (byte)(ParentFormValue.Opacity * 255),
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
        var formTabRenderer = ParentFormValue?.TabRenderer;
        switch ((WM)m.Msg)
        {
            case WM.WM_SYSCOMMAND:
                if (m.WParam == new IntPtr(0xF030) || m.WParam == new IntPtr(0xF120) || m.WParam == new IntPtr(0xF020))
                {
                    ParentFormValue?.ForwardMessage(ref m);
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
                var parentFormTabRenderer = ParentFormValue?.TabRenderer;
                var parentFormTabs = ParentFormValue?.Tabs;
                if (parentFormTabs != null &&
                    parentFormTabRenderer != null &&
                    parentFormTabRenderer.OverTab(parentFormTabs, relativeCursorPosition) == null &&
                    !parentFormTabRenderer.IsOverAddButton(relativeCursorPosition))
                {
                    ParentFormValue?.ForwardMessage(ref m);
                }

                else
                {
                    // When the user clicks a mouse button, save the tab that the user was over so we can respond properly when the mouse button is released
                    if (ParentFormValue is { TabRenderer: not null })
                    {
                        TitleBarTab? clickedTab = formTabRenderer?.OverTab(ParentFormValue.Tabs, relativeCursorPosition);

                        if (clickedTab != null)
                        {
                            // If the user clicked the close button, remove the tab from the list
                            if (formTabRenderer != null && !formTabRenderer.IsOverCloseButton(clickedTab, relativeCursorPosition))
                            {
                                ParentFormValue.ResizeTabContents(clickedTab);
                                ParentFormValue.SelectedTabIndex = ParentFormValue.Tabs.IndexOf(clickedTab);

                                Render();
                            }

                            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, Cursor.Position.X, Cursor.Position.Y, 0));
                        }
                    }

                    ParentFormValue?.Activate();
                }

                break;

            case WM.WM_LBUTTONDBLCLK:
                ParentFormValue?.ForwardMessage(ref m);
                break;

            // We always return HTCAPTION for the hit test message so that the underlying window doesn't have its focus removed
            case WM.WM_NCHITTEST:
                if (ParentFormValue != null)
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

                if (ParentFormValue != null && formTabRenderer != null &&
                    formTabRenderer.OverTab(ParentFormValue.Tabs, relativeCursorPosition2) == null &&
                    !formTabRenderer.IsOverAddButton(relativeCursorPosition2))
                {
                    ParentFormValue.ForwardMessage(ref m);
                }

                else
                {
                    // When the user clicks a mouse button, save the tab that the user was over so we can respond properly when the mouse button is released
                    if (ParentFormValue != null)
                    {
                        TitleBarTab? clickedTab = formTabRenderer?.OverTab(ParentFormValue.Tabs, relativeCursorPosition2);

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
                                    try
                                    {
                                        Render();
                                    }
                                    catch (Exception e)
                                    {
                                        Trace.WriteLine(e);
                                    }
                                }

                                else
                                {
                                    ParentFormValue.OnTabClicked(
                                        new TitleBarTabEventArgs
                                        {
                                            Tab = clickedTab,
                                            TabIndex = ParentFormValue.SelectedTabIndex,
                                            Action = TabControlAction.Selected,
                                            WasDragging = WasDragging
                                        });
                                }
                            }
                        }

                        // Otherwise, if the user clicked the add button, call CreateTab to add a new tab to the list and select it
                        else if (formTabRenderer != null && formTabRenderer.IsOverAddButton(relativeCursorPosition2))
                        {
                            ParentFormValue.AddNewTab().Wait();
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

    /// <summary>
    /// Makes OnMouseUp internal.
    /// </summary>
    /// <param name="mouseEventArgs">The mouse EventArgs.</param>
    internal void DoOnMouseUp(MouseEventArgs mouseEventArgs)
    {
        OnMouseUp(mouseEventArgs);
    }

    /// <summary>
    /// Makes OnMouseMove internal.
    /// </summary>
    /// <param name="mouseEventArgs">The mouse EventArgs.</param>
    internal void DoOnMouseMove(MouseEventArgs mouseEventArgs)
    {
        OnMouseMove(mouseEventArgs);
    }
}