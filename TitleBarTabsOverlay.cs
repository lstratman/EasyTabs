using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    internal class TitleBarTabsOverlay : Form
    {
        protected TitleBarTabs _parentForm;
        protected static Dictionary<TitleBarTabs, TitleBarTabsOverlay> _parents = new Dictionary<TitleBarTabs, TitleBarTabsOverlay>();
        protected ToolTip _tooltip = new ToolTip();
        protected bool _isActivated;
        protected Size _originalMinSize;

        /// <summary>
        ///   State information representing a tab that was clicked during a 
        ///   <see cref = "Win32Messages.WM_LBUTTONDOWN" /> message so that we can respond properly during the 
        ///   <see cref = "Win32Messages.WM_LBUTTONUP" /> message.
        /// </summary>
        protected TitleBarTab _clickedTab;

        public static TitleBarTabsOverlay GetInstance(TitleBarTabs parentForm)
        {
            if (!_parents.ContainsKey(parentForm))
                _parents.Add(parentForm, new TitleBarTabsOverlay(parentForm));
            
            return _parents[parentForm];
        }

        public TitleBarTabsOverlay(TitleBarTabs parentForm)
        {
            _parentForm = parentForm;

            _parentForm.Disposed += _parentForm_Disposed;
            _isActivated = _parentForm.WindowState != FormWindowState.Minimized;
            _originalMinSize = _parentForm.MinimumSize;

            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MinimizeBox = false;
            MaximizeBox = false;

            Show(_parentForm);

            AttachHandlers();
            ToolTip.ShowAlways = true;
        }

        protected void AttachHandlers()
        {
            _parentForm.Deactivate += _parentForm_Deactivate;
            _parentForm.Activated += _parentForm_FormActivated;
            _parentForm.SizeChanged += _parentForm_Refresh;
            _parentForm.VisibleChanged += _parentForm_Refresh;
            _parentForm.Move += _parentForm_Refresh;
            _parentForm.SystemColorsChanged += _parentForm_SystemColorsChanged;
        }

        public ToolTip ToolTip
        {
            get
            {
                return _tooltip;
            }

            set
            {
                _tooltip = value;
            }
        }

        private void _parentForm_SystemColorsChanged(object sender, EventArgs e)
        {
            OnPosition();
        }

        private void _parentForm_Refresh(object sender, EventArgs e)
        {
            if (_parentForm.WindowState == FormWindowState.Minimized)
            {
                _isActivated = false;
                Visible = false;
            }

            else
            {
                _isActivated = true;
                OnPosition();
            }
        }

        protected void OnPosition()
        {
            if (!IsDisposed)
            {
                Top = _parentForm.Top + (_parentForm.WindowState == FormWindowState.Maximized
                          ? SystemInformation.VerticalResizeBorderThickness
                          : SystemInformation.CaptionButtonSize.Height);
                Left = _parentForm.Left + SystemInformation.HorizontalResizeBorderThickness - SystemInformation.BorderSize.Width;
                Width = _parentForm.Width - (SystemInformation.VerticalResizeBorderThickness * 2) + (SystemInformation.BorderSize.Width * 2);

                Render();
            }
        }

        public void Render()
        {
            if (!IsDisposed && _parentForm.TabRenderer != null)
            {
                Height = _parentForm.TabRenderer.TabHeight;

                using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.FillRectangle(new SolidBrush(Color.Transparent), new Rectangle(0, 0, Width, Height));
                    _parentForm.TabRenderer.Render(_parentForm.Tabs, graphics, PointToClient(Cursor.Position));

                    IntPtr screenDc = Win32Interop.GetDC(IntPtr.Zero);
                    IntPtr memDc = Win32Interop.CreateCompatibleDC(screenDc);
                    IntPtr oldBitmap = IntPtr.Zero;
                    IntPtr bitmapHandle = IntPtr.Zero;

                    try
                    {
                        bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
                        oldBitmap = Win32Interop.SelectObject(memDc, bitmapHandle);

                        SIZE size = new SIZE(bitmap.Width, bitmap.Height);
                        POINT pointSource = new POINT(0, 0);
                        POINT topPos = new POINT(Left, Top);
                        BLENDFUNCTION blend = new BLENDFUNCTION
                        {
                            BlendOp = Win32Constants.AC_SRC_OVER,
                            BlendFlags = 0,
                            SourceConstantAlpha = 255,
                            AlphaFormat = Win32Constants.AC_SRC_ALPHA
                        };

                        if (!Win32Interop.UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref pointSource,
                                                            0, ref blend, Win32Constants.ULW_ALPHA))
                        {
                            int error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error, "Error while calling UpdateLayeredWindow().");
                        }
                    }

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

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Win32Messages.WM_NCLBUTTONDOWN:
                case Win32Messages.WM_LBUTTONDOWN:
                    Debug.WriteLine("Button down");

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
                    Debug.WriteLine("Button up");

                    Point relativeCursorPosition2 = new Point(Cursor.Position.X - Location.X,
                                                              Cursor.Position.Y - Location.Y);

                    if (_clickedTab != null)
                    {
                        Rectangle absoluteCloseButtonArea = new Rectangle();

                        if (_clickedTab.ShowCloseButton)
                        {
                            absoluteCloseButtonArea = new Rectangle(_clickedTab.Area.X + _clickedTab.CloseButtonArea.X,
                                                                    _clickedTab.Area.Y + _clickedTab.CloseButtonArea.Y,
                                                                    _clickedTab.CloseButtonArea.Width,
                                                                    _clickedTab.CloseButtonArea.Height);
                        }

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

                    // Otherwise, if the user clicked the add button, call CreateTab to add a new tab to the list
                    // and select it
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

        private void _parentForm_FormActivated(object sender, EventArgs e)
        {
            ToolTip.ShowAlways = true;
        }

        private void _parentForm_Deactivate(object sender, EventArgs e)
        {
            ToolTip.ShowAlways = false;
        }

        private void _parentForm_Disposed(object sender, EventArgs e)
        {
            TitleBarTabs form = (TitleBarTabs)sender;
            
            if (form == null)
                return;

            if (_parents.ContainsKey(form))
                _parents.Remove(form);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= Win32Constants.WS_EX_LAYERED;

                return createParams;
            }
        }
    }
}
