using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	/// <summary>
	/// Borderless overlay window that is moved with and rendered on top of the non-client area of a  <see cref="TitleBarTabs"/> instance that's responsible 
	/// for rendering the actual tab content and responding to click events for those tabs.
	/// </summary>
	internal class TitleBarTabsOverlay : Form
	{
		/// <summary>
		/// All of the parent forms and their overlays so that we don't create duplicate overlays across the application domain.
		/// </summary>
		protected static Dictionary<TitleBarTabs, TitleBarTabsOverlay> _parents = new Dictionary<TitleBarTabs, TitleBarTabsOverlay>();

		/// <summary>
		/// Flag indicating whether or not the underlying window is active.
		/// </summary>
		protected bool _active = false;

		/// <summary>
		/// Flag indicating whether we should draw the titlebar background (i.e. we are in a non-Aero environment).
		/// </summary>
		protected bool _aeroEnabled = false;

		/// <summary>
		/// State information representing a tab that was clicked during a <see cref="Win32Messages.WM_LBUTTONDOWN" /> message so that we can respond properly 
		/// during the <see cref="Win32Messages.WM_LBUTTONUP" /> message.
		/// </summary>
		protected TitleBarTab _clickedTab;

		/// <summary>
		/// Pointer to the low-level mouse hook callback (<see cref="MouseHookCallback"/>).
		/// </summary>
		protected IntPtr _hookId;

		/// <summary>
		/// Delegate of <see cref="MouseHookCallback"/>; declared as a member variable to keep it from being garbage collected.
		/// </summary>
		protected HOOKPROC _hookproc = null;

		/// <summary>
		/// Index of the tab, if any, whose close button is being hovered over.
		/// </summary>
		protected int _isOverCloseButtonForTab = -1;

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
			FormBorderStyle = FormBorderStyle.Sizable;
			MinimizeBox = false;
			MaximizeBox = false;
			_aeroEnabled = _parentForm.IsCompositionEnabled;

			Show(_parentForm);
			AttachHandlers();
		}

		/// <summary>
		/// Makes sure that the window is created with an <see cref="Win32Constants.WS_EX_LAYERED"/> flag set so that it can be alpha-blended properly with the 
		/// content (<see cref="_parentForm"/>) underneath the overlay.
		/// </summary>
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= Win32Constants.WS_EX_LAYERED | Win32Constants.WS_EX_NOACTIVATE;

				return createParams;
			}
		}

		/// <summary>
		/// Primary color for the titlebar background.
		/// </summary>
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

		/// <summary>
		/// Type of theme being used by the OS to render the desktop.
		/// </summary>
		protected DisplayType DisplayType
		{
			get
			{
				if (_aeroEnabled)
					return DisplayType.Aero;

				else if (Application.RenderWithVisualStyles && Environment.OSVersion.Version.Major >= 6)
					return DisplayType.Basic;

				return DisplayType.Classic;
			}
		}

		/// <summary>
		/// Gradient color for the titlebar background.
		/// </summary>
		protected Color TitleBarGradientColor
		{
			get
			{
				return _active
				       	? SystemInformation.IsTitleBarGradientEnabled
				       	  	? SystemColors.GradientActiveCaption
				       	  	: SystemColors.ActiveCaption
				       	: SystemInformation.IsTitleBarGradientEnabled
				       	  	? SystemColors.GradientInactiveCaption
				       	  	: SystemColors.InactiveCaption;
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
		/// Attaches the various event handlers to <see cref="_parentForm"/> so that the overlay is moved in synchronization to <see cref="_parentForm"/>.
		/// </summary>
		protected void AttachHandlers()
		{
			_parentForm.Disposed += _parentForm_Disposed;
			_parentForm.Deactivate += _parentForm_Deactivate;
			_parentForm.Activated += _parentForm_Activated;
			_parentForm.SizeChanged += _parentForm_Refresh;
			_parentForm.Shown += _parentForm_Refresh;
			_parentForm.VisibleChanged += _parentForm_Refresh;
			_parentForm.Move += _parentForm_Refresh;
			_parentForm.SystemColorsChanged += _parentForm_SystemColorsChanged;

			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule)
			{
				_hookproc = MouseHookCallback;
				_hookId = Win32Interop.SetWindowsHookEx(
					Win32Messages.WH_MOUSE_LL, _hookproc, Win32Interop.GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		/// <summary>
		/// Hook callback to process <see cref="Win32Messages.WM_MOUSEMOVE"/> messages to highlight/un-highlight the close button on each tab.
		/// </summary>
		/// <param name="nCode">The message being received.</param>
		/// <param name="wParam">Additional information about the message.</param>
		/// <param name="lParam">Additional information about the message.</param>
		/// <returns>A zero value if the procedure processes the message; a nonzero value if the procedure ignores the message.</returns>
		protected IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && Win32Messages.WM_MOUSEMOVE == (int) wParam)
			{
				MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof (MSLLHOOKSTRUCT));
				Point cursorPosition = new Point(hookStruct.pt.x, hookStruct.pt.y);
				bool reRender = false;

				// If we were over a close button previously, check to see if the cursor is still over that tab's
				// close button; if not, re-render
				if (_isOverCloseButtonForTab != -1 &&
				    (_isOverCloseButtonForTab >= _parentForm.Tabs.Count ||
				     !_parentForm.TabRenderer.IsOverCloseButton(_parentForm.Tabs[_isOverCloseButtonForTab], GetRelativeCursorPosition(cursorPosition))))
				{
					reRender = true;
					_isOverCloseButtonForTab = -1;
				}

				// Otherwise, see if any tabs' close button is being hovered over
				else
				{
					// ReSharper disable ForCanBeConvertedToForeach
					for (int i = 0; i < _parentForm.Tabs.Count; i++)
					// ReSharper restore ForCanBeConvertedToForeach
					{
						if (_parentForm.TabRenderer.IsOverCloseButton(_parentForm.Tabs[i], GetRelativeCursorPosition(cursorPosition)))
						{
							_isOverCloseButtonForTab = i;
							reRender = true;

							break;
						}
					}
				}

				if (reRender)
					Render(cursorPosition, true);
			}

			return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);
		}

		/// <summary>
		/// Draws the titlebar background behind the tabs if Aero glass is not enabled.
		/// </summary>
		/// <param name="graphics">Graphics context with which to draw the background.</param>
		protected virtual void DrawTitleBarBackground(Graphics graphics)
		{
			if (DisplayType == DisplayType.Aero)
				return;

			Rectangle fillArea;

			if (DisplayType == DisplayType.Basic)
				fillArea = new Rectangle(
					new Point(
						1, Top == 0
						   	? SystemInformation.CaptionHeight - 1
						   	: (SystemInformation.CaptionHeight + SystemInformation.VerticalResizeBorderThickness) - (Top - _parentForm.Top) - 1),
					new Size(Width - 2, _parentForm.Padding.Top));

			else
				fillArea = new Rectangle(new Point(1, 0), new Size(Width - 2, Height - 1));

			if (fillArea.Height <= 0)
				return;

			// Adjust the margin so that the gradient stops immediately prior to the control box in the titlebar
			int rightMargin = 3;

			if (_parentForm.ControlBox && _parentForm.MinimizeBox)
				rightMargin += SystemInformation.CaptionButtonSize.Width;

			if (_parentForm.ControlBox && _parentForm.MaximizeBox)
				rightMargin += SystemInformation.CaptionButtonSize.Width;

			if (_parentForm.ControlBox)
				rightMargin += SystemInformation.CaptionButtonSize.Width;

			LinearGradientBrush gradient = new LinearGradientBrush(
				new Point(24, 0), new Point(fillArea.Width - rightMargin + 1, 0), TitleBarColor, TitleBarGradientColor);

			using (BufferedGraphics bufferedGraphics = BufferedGraphicsManager.Current.Allocate(graphics, fillArea))
			{
				bufferedGraphics.Graphics.FillRectangle(new SolidBrush(TitleBarColor), fillArea);
				bufferedGraphics.Graphics.FillRectangle(
					new SolidBrush(TitleBarGradientColor),
					new Rectangle(new Point(fillArea.Location.X + fillArea.Width - rightMargin, fillArea.Location.Y), new Size(rightMargin, fillArea.Height)));
				bufferedGraphics.Graphics.FillRectangle(
					gradient, new Rectangle(fillArea.Location, new Size(fillArea.Width - rightMargin, fillArea.Height)));
				bufferedGraphics.Graphics.FillRectangle(new SolidBrush(TitleBarColor), new Rectangle(fillArea.Location, new Size(24, fillArea.Height)));

				bufferedGraphics.Render(graphics);
			}
		}

		/// <summary>
		/// Event handler that is called when <see cref="_parentForm"/>'s <see cref="Control.SystemColorsChanged"/> event is fired which re-renders the tabs.
		/// </summary>
		/// <param name="sender">Object from which the event originated.</param>
		/// <param name="e">Arguments associated with the event.</param>
		private void _parentForm_SystemColorsChanged(object sender, EventArgs e)
		{
			_aeroEnabled = _parentForm.IsCompositionEnabled;
			OnPosition();
		}

		/// <summary>
		/// Event handler that is called when <see cref="_parentForm"/>'s <see cref="Control.SizeChanged"/>, <see cref="Control.VisibleChanged"/>, or 
		/// <see cref="Control.Move"/> events are fired which re-renders the tabs.
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
		/// Sets the position of the overlay window to match that of <see cref="_parentForm"/> so that it moves in tandem with it.
		/// </summary>
		protected void OnPosition()
		{
			if (!IsDisposed)
			{
				// If the form is in a non-maximized state, we position the tabs below the minimize/maximize/close
				// buttons
				Top = _parentForm.Top + (DisplayType == DisplayType.Classic
				                         	? SystemInformation.VerticalResizeBorderThickness
				                         	: _parentForm.WindowState == FormWindowState.Maximized
				                         	  	? SystemInformation.VerticalResizeBorderThickness
				                         	  	: SystemInformation.CaptionHeight);
				Left = _parentForm.Left + SystemInformation.HorizontalResizeBorderThickness - SystemInformation.BorderSize.Width;
				Width = _parentForm.Width - (SystemInformation.VerticalResizeBorderThickness * 2) + (SystemInformation.BorderSize.Width * 2);
				Height = _parentForm.TabRenderer.TabHeight + (DisplayType == DisplayType.Classic && _parentForm.WindowState != FormWindowState.Maximized
				                                              	? SystemInformation.CaptionButtonSize.Height
				                                              	: 0);

				Render();
			}
		}

		/// <summary>
		/// Renders the tabs and then calls <see cref="Win32Interop.UpdateLayeredWindow"/> to blend the tab content with the underlying window 
		/// (<see cref="_parentForm"/>).
		/// </summary>
		/// <param name="forceRedraw">Flag indicating whether a full render should be forced.</param>
		public void Render(bool forceRedraw = false)
		{
			Render(Cursor.Position, forceRedraw);
		}

		/// <summary>
		/// Renders the tabs and then calls <see cref="Win32Interop.UpdateLayeredWindow"/> to blend the tab content with the underlying window 
		/// (<see cref="_parentForm"/>).
		/// </summary>
		/// <param name="cursorPosition">Current position of the cursor.</param>
		/// <param name="forceRedraw">Flag indicating whether a full render should be forced.</param>
		public void Render(Point cursorPosition, bool forceRedraw = false)
		{
			if (!IsDisposed && _parentForm.TabRenderer != null && _parentForm.WindowState != FormWindowState.Minimized)
			{
				cursorPosition = GetRelativeCursorPosition(cursorPosition);

				using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					DrawTitleBarBackground(graphics);

					// Since classic mode themese draw over the *entire* titlebar, not just the area immediately behind the tabs, we have to offset the tabs
					// when rendering in the window
					Point offset = _parentForm.WindowState != FormWindowState.Maximized && DisplayType == DisplayType.Classic
					               	? new Point(0, SystemInformation.CaptionButtonSize.Height)
					               	: _parentForm.WindowState != FormWindowState.Maximized
					               	  	? new Point(0, SystemInformation.VerticalResizeBorderThickness - SystemInformation.BorderSize.Height)
					               	  	: new Point(0, 0);

					// Render the tabs into the bitmap
					_parentForm.TabRenderer.Render(_parentForm.Tabs, graphics, offset, cursorPosition, forceRedraw);

					// Cut out a hole in the background so that the control box on the underlying window can be shown
					if (DisplayType == DisplayType.Classic && (_parentForm.ControlBox || _parentForm.MaximizeBox || _parentForm.MinimizeBox))
					{
						int boxWidth = 0;

						if (_parentForm.ControlBox)
							boxWidth += SystemInformation.CaptionButtonSize.Width;

						if (_parentForm.MinimizeBox)
							boxWidth += SystemInformation.CaptionButtonSize.Width;

						if (_parentForm.MaximizeBox)
							boxWidth += SystemInformation.CaptionButtonSize.Width;

						CompositingMode oldCompositingMode = graphics.CompositingMode;

						graphics.CompositingMode = CompositingMode.SourceCopy;
						graphics.FillRectangle(
							new SolidBrush(Color.Transparent), Width - boxWidth, 0, boxWidth, SystemInformation.CaptionButtonSize.Height);
						graphics.CompositingMode = oldCompositingMode;
					}

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
						                      		// We want to blend the bitmap's content with the screen content under it
						                      		BlendOp = Win32Constants.AC_SRC_OVER,
						                      		BlendFlags = 0,
						                      		SourceConstantAlpha = 255,
						                      		// We use the bitmap's alpha channel for blending instead of a pre-defined transparency key
						                      		AlphaFormat = Win32Constants.AC_SRC_ALPHA
						                      	};

						// Blend the tab content with the underlying content
						if (!Win32Interop.UpdateLayeredWindow(
							Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, Win32Constants.ULW_ALPHA))
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
		/// Gets the relative location of the cursor within the overlay.
		/// </summary>
		/// <param name="cursorPosition">Cursor position that represents the absolute position of the cursor on the screen.</param>
		/// <returns>The relative location of the cursor within the overlay.</returns>
		protected Point GetRelativeCursorPosition(Point cursorPosition)
		{
			return new Point(cursorPosition.X - Location.X, cursorPosition.Y - Location.Y);
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
					Point relativeCursorPosition = GetRelativeCursorPosition(Cursor.Position);

					// When the user clicks a mouse button, save the tab that the user was over so we can respond properly when the mouse button is released
					_clickedTab = _parentForm.TabRenderer.OverTab(_parentForm.Tabs, relativeCursorPosition);

					// If we were over a tab, set the capture state for the window so that we'll actually receive a WM_LBUTTONUP message
					if (_clickedTab != null || _parentForm.TabRenderer.IsOverAddButton(relativeCursorPosition))
						Win32Interop.SetCapture(m.HWnd);

					else
						_parentForm.ForwardMessage(ref m);

					break;

				case Win32Messages.WM_LBUTTONDBLCLK:
					_parentForm.ForwardMessage(ref m);
					break;

				// We always return HTCAPTION for the hit test message so that the underlying window doesn't have its focus removed
				case Win32Messages.WM_NCHITTEST:
					m.Result = new IntPtr(Win32Constants.HTCAPTION);
					break;

				case Win32Messages.WM_LBUTTONUP:
				case Win32Messages.WM_NCLBUTTONUP:
					Point relativeCursorPosition2 = GetRelativeCursorPosition(Cursor.Position);

					if (_clickedTab != null)
					{
						// If the user clicked the close button, remove the tab from the list
						if (_parentForm.TabRenderer.IsOverCloseButton(_clickedTab, relativeCursorPosition2))
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

					// Otherwise, if the user clicked the add button, call CreateTab to add a new tab to the list and select it
					else if (_parentForm.TabRenderer.IsOverAddButton(relativeCursorPosition2))
					{
						_parentForm.AddNewTab();

						// Release the mouse capture
						Win32Interop.ReleaseCapture();
					}

					else
						_parentForm.ForwardMessage(ref m);

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
		private void _parentForm_Activated(object sender, EventArgs e)
		{
			_active = true;
			Render();
		}

		/// <summary>
		/// Event handler that is called when <see cref="_parentForm"/>'s <see cref="Form.Deactivate"/> event is fired.
		/// </summary>
		/// <param name="sender">Object from which this event originated.</param>
		/// <param name="e">Arguments associated with the event.</param>
		private void _parentForm_Deactivate(object sender, EventArgs e)
		{
			_active = false;
			Render();
		}

		/// <summary>
		/// Event handler that is called when <see cref="_parentForm"/>'s <see cref="Component.Disposed"/> event is fired.
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

			Win32Interop.UnhookWindowsHookEx(_hookId);
		}
	}
}