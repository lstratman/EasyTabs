using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Win32Interop.Enums;
using Win32Interop.Methods;
using Win32Interop.Structs;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	public class TornTabForm : Form
	{
		protected Point _cursorOffset;

		/// <summary>
		/// Pointer to the low-level mouse hook callback (<see cref="MouseHookCallback" />).
		/// </summary>
		protected IntPtr _hookId;

		protected bool _hookInstalled = false;

		/// <summary>
		/// Delegate of <see cref="MouseHookCallback" />; declared as a member variable to keep it from being garbage collected.
		/// </summary>
		protected HOOKPROC _hookproc = null;

		private bool _initialized;
		private LayeredWindow _layeredWindow;
		protected Bitmap _tabThumbnail;

		public TornTabForm(TitleBarTab tab, BaseTabRenderer tabRenderer)
		{
			_layeredWindow = new LayeredWindow();
			_initialized = false;

			// Set drawing styles
			SetStyle(ControlStyles.DoubleBuffer, true);

			Opacity = 0.70;
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
			BackColor = Color.Fuchsia;
			TransparencyKey = Color.Fuchsia;
			AllowTransparency = true;

			Disposed += TornTabForm_Disposed;

			Bitmap tabContents = tab.GetImage();
			Bitmap contentsAndTab = new Bitmap(tabContents.Width, tabContents.Height + tabRenderer.TabHeight, tabContents.PixelFormat);
			Graphics tabGraphics = Graphics.FromImage(contentsAndTab);

			tabGraphics.DrawImage(tabContents, 0, tabRenderer.TabHeight);

			bool oldShowAddButton = tabRenderer.ShowAddButton;

			tabRenderer.ShowAddButton = false;
			tabRenderer.Render(
				new List<TitleBarTab>
					{
						tab
					}, tabGraphics, new Point(0, 0), new Point(0, 0), true);
			tabRenderer.ShowAddButton = oldShowAddButton;

			_tabThumbnail = new Bitmap(contentsAndTab.Width / 2, contentsAndTab.Height / 2, contentsAndTab.PixelFormat);
			Graphics thumbnailGraphics = Graphics.FromImage(_tabThumbnail);

			thumbnailGraphics.InterpolationMode = InterpolationMode.High;
			thumbnailGraphics.CompositingQuality = CompositingQuality.HighQuality;
			thumbnailGraphics.SmoothingMode = SmoothingMode.AntiAlias;
			thumbnailGraphics.DrawImage(contentsAndTab, 0, 0, _tabThumbnail.Width, _tabThumbnail.Height);

			Width = _tabThumbnail.Width - 1;
			Height = _tabThumbnail.Height - 1;

			_cursorOffset = new Point(tabRenderer.TabContentWidth / 4, tabRenderer.TabHeight / 4);

			SetWindowPosition(Cursor.Position);
		}

		private void TornTabForm_Disposed(object sender, EventArgs e)
		{
			User32.UnhookWindowsHookEx(_hookId);
		}

		/// <summary>
		/// Hook callback to process <see cref="WM.WM_MOUSEMOVE" /> messages to highlight/un-highlight the close button on each tab.
		/// </summary>
		/// <param name="nCode">The message being received.</param>
		/// <param name="wParam">Additional information about the message.</param>
		/// <param name="lParam">Additional information about the message.</param>
		/// <returns>A zero value if the procedure processes the message; a nonzero value if the procedure ignores the message.</returns>
		protected IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && (int) WM.WM_MOUSEMOVE == (int) wParam)
			{
				MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof (MSLLHOOKSTRUCT));
				Point cursorPosition = new Point(hookStruct.pt.x, hookStruct.pt.y);

				SetWindowPosition(cursorPosition);
			}

			return User32.CallNextHookEx(_hookId, nCode, wParam, lParam);
		}

		protected void SetWindowPosition(Point cursorPosition)
		{
			Left = cursorPosition.X - _cursorOffset.X;
			Top = cursorPosition.Y - _cursorOffset.Y;

			UpdateLayeredBackground();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			_initialized = true;

			UpdateLayeredBackground();

			_layeredWindow.Show();
			_layeredWindow.Enabled = false;

			if (!_hookInstalled)
			{
				using (Process curProcess = Process.GetCurrentProcess())
				{
					using (ProcessModule curModule = curProcess.MainModule)
					{
						_hookproc = MouseHookCallback;
						_hookId = User32.SetWindowsHookEx(WH.WH_MOUSE_LL, _hookproc, Kernel32.GetModuleHandleA(curModule.ModuleName), 0);
					}
				}

				_hookInstalled = true;
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			_layeredWindow.Close();
		}

		public void UpdateLayeredBackground()
		{
			if (_tabThumbnail == null || !_initialized)
				return;

			byte opacity = (byte) (Opacity * 255);
			_layeredWindow.UpdateWindow(
				_tabThumbnail, opacity, Width, Height, new POINT
					                                       {
						                                       x = Location.X,
						                                       y = Location.Y
					                                       });
		}
	}

	internal class LayeredWindow : Form
	{
		public LayeredWindow()
		{
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= (int) WS_EX.WS_EX_LAYERED;

				return createParams;
			}
		}

		public void UpdateWindow(Bitmap image, byte opacity, int width, int height, POINT position)
		{
			IntPtr windowHandle = User32.GetWindowDC(Handle);
			IntPtr deviceContextHandle = Gdi32.CreateCompatibleDC(windowHandle);
			IntPtr bitmapHandle = image.GetHbitmap(Color.FromArgb(0));
			IntPtr oldBitmapHandle = Gdi32.SelectObject(deviceContextHandle, bitmapHandle);
			SIZE size = new SIZE
				            {
					            cx = 0,
					            cy = 0
				            };
			POINT destinationPosition = new POINT
				                            {
					                            x = 0,
					                            y = 0
				                            };

			if (width == -1 || height == -1)
			{
				//No width and height specified, use the size of the image
				size.cx = image.Width;
				size.cy = image.Height;
			}
			else
			{
				//Use whichever size is smallest, so that the image will
				//be clipped if necessary
				size.cx = Math.Min(image.Width, width);
				size.cy = Math.Min(image.Height, height);
			}

			BLENDFUNCTION blendFunction = new BLENDFUNCTION();
			blendFunction.BlendOp = Convert.ToByte((int) AC.AC_SRC_OVER);
			blendFunction.SourceConstantAlpha = opacity;
			blendFunction.AlphaFormat = Convert.ToByte((int) AC.AC_SRC_ALPHA);
			blendFunction.BlendFlags = 0;

			User32.UpdateLayeredWindow(Handle, windowHandle, ref position, ref size, deviceContextHandle, ref destinationPosition, 0, ref blendFunction, ULW.ULW_ALPHA);

			Gdi32.SelectObject(deviceContextHandle, oldBitmapHandle);
			Gdi32.DeleteObject(bitmapHandle);
			Gdi32.DeleteDC(deviceContextHandle);
			User32.ReleaseDC(Handle, windowHandle);
		}
	}
}