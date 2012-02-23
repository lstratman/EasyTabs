using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    /// <summary>
    /// Borderless overlay window that is moved with and rendered on top of the non-client area of a 
    /// <see cref="TitleBarTabs"/> instance that's responsible for rendering the actual tab content and responding to
    /// click events for those tabs.
    /// </summary>
    internal class TitleBarTabsOverlay : Form
    {
        /// <summary>
        /// All of the parent forms and their overlays so that we don't create duplicate overlays across the 
        /// application domain.
        /// </summary>
        protected static Dictionary<TitleBarTabs, TitleBarTabsOverlay> _parents =
            new Dictionary<TitleBarTabs, TitleBarTabsOverlay>();

        /// <summary>
        /// State information representing a tab that was clicked during a <see cref="Win32Messages.WM_LBUTTONDOWN" /> 
        /// message so that we can respond properly during the <see cref="Win32Messages.WM_LBUTTONUP" /> message.
        /// </summary>
        protected TitleBarTab _clickedTab;

        /// <summary>
        /// Parent form for the overlay.
        /// </summary>
        protected TitleBarTabs _parentForm;

        /// <summary>
        /// Blank default constructor to ensure that the overlays are only initialized through 
        /// <see cref="GetInstance"/>.
        /// </summary>
        protected TitleBarTabsOverlay()
        {
        }

        /// <summary>
        /// Creates the overlay window and attaches it to <paramref name="parentForm"/>.
        /// </summary>
        /// <param name="parentForm">Parent form that the overlay should be rendered on top of.</param>
        protected TitleBarTabsOverlay(TitleBarTabs parentForm)
        {
            _parentForm = parentForm;
            
            // We don't want this window visible in the taskbar
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MinimizeBox = false;
            MaximizeBox = false;

            Show(_parentForm);
            AttachHandlers();
        }

        /// <summary>
        /// Makes sure that the window is created with an <see cref="Win32Constants.WS_EX_LAYERED"/> flag set so that
        /// it can be alpha-blended properly with the content (<see cref="_parentForm"/>) underneath the overlay.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= Win32Constants.WS_EX_LAYERED;

                return createParams;
            }
        }

        /// <summary>
        /// Retrieves or creates the overlay for <paramref name="parentForm"/>.
        /// </summary>
        /// <param name="parentForm">Parent form that we are to create the overlay for.</param>
        /// <returns>Newly-created or previously existing overlay for <paramref name="parentForm"/>.</returns>
        public static TitleBarTabsOverlay GetInstance(TitleBarTabs parentForm)
        {
            if (!_parents.ContainsKey(parentForm))
                _parents.Add(parentForm, new TitleBarTabsOverlay(parentForm));

            return _parents[parentForm];
        }

        /// <summary>
        /// Attaches the various event handlers to <see cref="_parentForm"/> so that the overlay is moved in
        /// synchronization to <see cref="_parentForm"/>.
        /// </summary>
        protected void AttachHandlers()
        {
            _parentForm.Disposed += _parentForm_Disposed;
            _parentForm.Deactivate += _parentForm_Deactivate;
            _parentForm.Activated += _parentForm_FormActivated;
            _parentForm.SizeChanged += _parentForm_Refresh;
            _parentForm.Shown += _parentForm_Refresh;
            _parentForm.VisibleChanged += _parentForm_Refresh;
            _parentForm.Move += _parentForm_Refresh;
            _parentForm.SystemColorsChanged += _parentForm_SystemColorsChanged;
        }

        /// <summary>
        /// Event handler that is called when <see cref="_parentForm"/>'s <see cref="Form.SystemColorsChanged"/> event
        /// is fired which re-renders the tabs.
        /// </summary>
        /// <param name="sender">Object from which the event originated.</param>
        /// <param name="e">Arguments associated with the event.</param>
        private void _parentForm_SystemColorsChanged(object sender, EventArgs e)
        {
            OnPosition();
        }

        /// <summary>
        /// Event handler that is called when <see cref="_parentForm"/>'s <see cref="Form.SizeChanged"/>,
        /// <see cref="Form.VisibleChanged"/>, or <see cref="Form.Move"/> events are fired which re-renders the tabs.
        /// </summary>
        /// <param name="sender">Object from which the event originated.</param>
        /// <param name="e">Arguments associated with the event.</param>
        private void _parentForm_Refresh(object sender, EventArgs e)
        {
            if (_parentForm.WindowState == FormWindowState.Minimized)
                Visible = false;

            else
                OnPosition();
        }

        /// <summary>
        /// Sets the position of the overlay window to match that of <see cref="_parentForm"/> so that it moves in
        /// tandem with it.
        /// </summary>
        protected void OnPosition()
        {
            if (!IsDisposed)
            {
                // If the form is in a non-maximized state, we position the tabs below the minimize/maximize/close
                // buttons
                Top = _parentForm.Top + (_parentForm.WindowState == FormWindowState.Maximized
                                             ? SystemInformation.VerticalResizeBorderThickness
                                             : SystemInformation.CaptionButtonSize.Height);
                Left = _parentForm.Left + SystemInformation.HorizontalResizeBorderThickness -
                       SystemInformation.BorderSize.Width;
                Width = _parentForm.Width - (SystemInformation.VerticalResizeBorderThickness * 2) +
                        (SystemInformation.BorderSize.Width * 2);

                Render();
            }
        }

        /// <summary>
        /// Renders the tabs and then calls <see cref="Win32Interop.UpdateLayeredWindow"/> to blend the tab content
        /// with the underlying window (<see cref="_parentForm"/>).
        /// </summary>
        public void Render()
        {
            if (!IsDisposed && _parentForm.TabRenderer != null)
            {
                Height = _parentForm.TabRenderer.TabHeight;

                using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    // Render the tabs into the bitmap
                    graphics.FillRectangle(new SolidBrush(Color.Transparent), new Rectangle(0, 0, Width, Height));
                    _parentForm.TabRenderer.Render(_parentForm.Tabs, graphics, PointToClient(Cursor.Position));

                    IntPtr screenDc = Win32Interop.GetDC(IntPtr.Zero);
                    IntPtr memDc = Win32Interop.CreateCompatibleDC(screenDc);
                    IntPtr oldBitmap = IntPtr.Zero;
                    IntPtr bitmapHandle = IntPtr.Zero;

                    try
                    {
                        // Copy the contents of the bitmap into memDc
                        bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
                        oldBitmap = Win32Interop.SelectObject(memDc, bitmapHandle);

                        SIZE size = new SIZE(bitmap.Width, bitmap.Height);
                        POINT pointSource = new POINT(0, 0);
                        POINT topPos = new POINT(Left, Top);
                        BLENDFUNCTION blend = new BLENDFUNCTION
                                                  {
                                                      // We want to blend the bitmap's content with the screen content
                                                      // under it
                                                      BlendOp = Win32Constants.AC_SRC_OVER,
                                                      BlendFlags = 0,
                                                      SourceConstantAlpha = 255,
                                                      // We use the bitmap's alpha channel for blending instead of a
                                                      // pre-defined transparency key
                                                      AlphaFormat = Win32Constants.AC_SRC_ALPHA
                                                  };

                        // Blend the tab content with the underlying content
                        if (
                            !Win32Interop.UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc,
                                                              ref pointSource,
                                                              0, ref blend, Win32Constants.ULW_ALPHA))
                        {
                            int error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error, "Error while calling UpdateLayeredWindow().");
                        }
                    }

                    // Clean up after ourselves
                    finally
                    {
                        Win32Interop.ReleaseDC(IntPtr.Zero, screenDc);

                        if (bitmapHandle != IntPtr.Zero)
                        {
                            Win32Interop.SelectObject(memDc, oldBitmap);
                            Win32Interop.DeleteObject(bitmapHandle);
                        }

                        Win32Interop.DeleteDC(memDc);
                    }
                }
            }
        }

        /// <summary>
        /// Overrides the message pump for the window so that we can respond to click events on the tabs themselves.
        /// </summary>
        /// <param name="m">Message received by the pump.</param>
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Win32Messages.WM_NCLBUTTONDOWN:
                case Win32Messages.WM_LBUTTONDOWN:
                    Point relativeCursorPosition = new Point(Cursor.Position.X - Location.X,
                                                             Cursor.Position.Y - Location.Y);

                    // When the user clicks a mouse button, save the tab that the user was over so we can respond
                    // properly when the mouse button is released
                    _clickedTab = _parentForm.TabRenderer.OverTab(_parentForm.Tabs, relativeCursorPosition);

                    // If we were over a tab, set the capture state for the window so that we'll actually receive
                    // a WM_LBUTTONUP message
                    if (_clickedTab != null || _parentForm.TabRenderer.IsOverAddButton(relativeCursorPosition))
                        Win32Interop.SetCapture(m.HWnd);

                    break;

                case Win32Messages.WM_LBUTTONUP:
                case Win32Messages.WM_NCLBUTTONUP:
                    Point relativeCursorPosition2 = new Point(Cursor.Position.X - Location.X,
                                                              Cursor.Position.Y - Location.Y);

                    if (_clickedTab != null)
                    {
                        Rectangle absoluteCloseButtonArea = new Rectangle();

                        if (_clickedTab.ShowCloseButton)
                            absoluteCloseButtonArea = new Rectangle(_clickedTab.Area.X + _clickedTab.CloseButtonArea.X,
                                                                    _clickedTab.Area.Y + _clickedTab.CloseButtonArea.Y,
                                                                    _clickedTab.CloseButtonArea.Width,
                                                                    _clickedTab.CloseButtonArea.Height);

                        // If the user clicked the close button, remove the tab from the list
                        if (absoluteCloseButtonArea.Contains(relativeCursorPosition2))
                        {
                            _clickedTab.Content.Close();
                            Render();
                        }

                            // Otherwise, select the tab that was clicked
                        else
                        {
                            _parentForm.ResizeTabContents(_clickedTab);
                            _parentForm.SelectedTabIndex = _parentForm.Tabs.IndexOf(_clickedTab);

                            Render();
                        }

                        // Release the mouse capture
                        Win32Interop.ReleaseCapture();
                    }

                    // Otherwise, if the user clicked the add button, call CreateTab to add a new tab to the list and 
                    // select it
                    else if (_parentForm.TabRenderer.IsOverAddButton(relativeCursorPosition2))
                    {
                        _parentForm.AddNewTab();

                        // Release the mouse capture
                        Win32Interop.ReleaseCapture();
                    }

                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        /// <summary>
        /// Event handler that is called when <see cref="_parentForm"/>'s <see cref="Form.Activated"/> event is fired.
        /// </summary>
        /// <param name="sender">Object from which this event originated.</param>
        /// <param name="e">Arguments associated with the event.</param>
        private void _parentForm_FormActivated(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Event handler that is called when <see cref="_parentForm"/>'s <see cref="Form.Deactivate"/> event is fired.
        /// </summary>
        /// <param name="sender">Object from which this event originated.</param>
        /// <param name="e">Arguments associated with the event.</param>
        private void _parentForm_Deactivate(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Event handler that is called when <see cref="_parentForm"/>'s <see cref="Component.Disposed"/> event is 
        /// fired.
        /// </summary>
        /// <param name="sender">Object from which this event originated.</param>
        /// <param name="e">Arguments associated with the event.</param>
        private void _parentForm_Disposed(object sender, EventArgs e)
        {
            TitleBarTabs form = (TitleBarTabs) sender;

            if (form == null)
                return;

            if (_parents.ContainsKey(form))
                _parents.Remove(form);
        }
    }
}