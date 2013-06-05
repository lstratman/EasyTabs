using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Win32Interop.Enums;
using Win32Interop.Methods;
using Win32Interop.Structs;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	public class TornTabForm : Form
	{
		protected bool _hookInstalled = false;

		/// <summary>
		/// Pointer to the low-level mouse hook callback (<see cref="MouseHookCallback"/>).
		/// </summary>
		protected IntPtr _hookId;

		protected Bitmap contents;

		/// <summary>
		/// Delegate of <see cref="MouseHookCallback"/>; declared as a member variable to keep it from being garbage collected.
		/// </summary>
		protected HOOKPROC _hookproc = null;

		protected Point _cursorOffset;

		public TornTabForm(TitleBarTab tab, BaseTabRenderer tabRenderer)
		{
			m_layeredWnd = new LayeredWindow();
			m_initialised = false;

			// Set drawing styles
			this.SetStyle(ControlStyles.DoubleBuffer, true);

			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
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

			Bitmap tabThumbnail = new Bitmap(contentsAndTab.Width / 2, contentsAndTab.Height / 2, contentsAndTab.PixelFormat);
			Graphics thumbnailGraphics = Graphics.FromImage(tabThumbnail);

			thumbnailGraphics.InterpolationMode = InterpolationMode.High;
			thumbnailGraphics.CompositingQuality = CompositingQuality.HighQuality;
			thumbnailGraphics.SmoothingMode = SmoothingMode.AntiAlias;
			thumbnailGraphics.DrawImage(contentsAndTab, 0, 0, tabThumbnail.Width, tabThumbnail.Height);

			Width = tabThumbnail.Width - 1;
			Height = tabThumbnail.Height - 1;

			contents = tabThumbnail;
			_cursorOffset = new Point(tabRenderer.TabContentWidth / 4, tabRenderer.TabHeight / 4);

			SetWindowPosition(Cursor.Position);
		}

		void TornTabForm_Disposed(object sender, EventArgs e)
		{
			User32.UnhookWindowsHookEx(_hookId);
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			Opacity = 0.70;

			if (!_hookInstalled)
			{
				using (Process curProcess = Process.GetCurrentProcess())
				using (ProcessModule curModule = curProcess.MainModule)
				{
					_hookproc = MouseHookCallback;
					_hookId = User32.SetWindowsHookEx(WH.WH_MOUSE_LL, _hookproc, Kernel32.GetModuleHandleA(curModule.ModuleName), 0);
				}

				_hookInstalled = true;
			}
		}

		/// <summary>
		/// Hook callback to process <see cref="WM.WM_MOUSEMOVE"/> messages to highlight/un-highlight the close button on each tab.
		/// </summary>
		/// <param name="nCode">The message being received.</param>
		/// <param name="wParam">Additional information about the message.</param>
		/// <param name="lParam">Additional information about the message.</param>
		/// <returns>A zero value if the procedure processes the message; a nonzero value if the procedure ignores the message.</returns>
		protected IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && (int)WM.WM_MOUSEMOVE == (int)wParam)
			{
				MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
				Point cursorPosition = new Point(hookStruct.pt.x, hookStruct.pt.y);

				SetWindowPosition(cursorPosition);
			}

			return User32.CallNextHookEx(_hookId, nCode, wParam, lParam);
		}

		protected void SetWindowPosition(Point cursorPosition)
		{
			Left = cursorPosition.X - _cursorOffset.X;
			Top = cursorPosition.Y - _cursorOffset.Y;

			m_layeredWnd.LayeredPos = new Point(Left, Top);
			updateLayeredBackground(Width, Height);

			//byte _opacity = (byte)(this.Opacity * 255);
			//UpdateWindow(m_background, _opacity, Width, Height, new Point(Left, Top));
		}

		#region Properties
		public void UpdateLayeredBackground()
		{
			updateLayeredBackground(this.ClientSize.Width, this.ClientSize.Height);
		}
		#endregion

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			//Set the transparency key to make the back of the form transparent
			//INTERESTING NOTE: If you use Fuchsia as the transparent colour then
			// it will be not only visually transparent but also transparent to 
			// mouse clicks. If you use any other colour then you will be able to
			// see through it, but you'll still get your mouse events
			this.BackColor = Color.Fuchsia;
			this.TransparencyKey = Color.Fuchsia;
			this.AllowTransparency = true;

			if (!this.DesignMode)
			{
				//Disable the form so that it cannot receive focus
				//We need to do this so that the form will not get focuse
				// by any means and then be positioned above our main form
				m_initialised = true;
				updateLayeredBackground(this.ClientSize.Width, this.ClientSize.Height, new Point(Left, Top));
				m_layeredWnd.Show();

				m_layeredWnd.Enabled = false;
			}
		}

		private void updateLayeredBackground(int width, int height)
		{
			updateLayeredBackground(width, height, m_layeredWnd.LayeredPos);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			m_layeredWnd.Close();
		}

		private void updateLayeredBackground(int width, int height, Point pos)
		{
			if (this.DesignMode || contents == null || !m_initialised)
				return;

			width = contents.Width;
			height = contents.Height;

			

			byte _opacity = (byte)(this.Opacity * 255);
			m_layeredWnd.UpdateWindow(contents, _opacity, width, height, new POINT
				                                                             {
					                                                             x = pos.X,
																				 y = pos.Y
				                                                             });
		}
		private LayeredWindow m_layeredWnd;
		private bool m_initialised;
	}

	class LayeredWindow : Form
	{
		private Rectangle m_rect;

		public Point LayeredPos
		{
			get { return m_rect.Location; }
			set { m_rect.Location = value; }
		}

		public Size LayeredSize
		{
			get { return m_rect.Size; }
		}

		public LayeredWindow()
		{
			//We need to set this before the window is created, otherwise we
			//have to reset thw window styles using SetWindowLong because
			//the window will no longer be drawn
			this.ShowInTaskbar = false;

			this.FormBorderStyle = FormBorderStyle.None;
		}

		public void UpdateWindow(Bitmap image, byte opacity, int width, int height, POINT pos)
		{
			IntPtr hdcWindow = User32.GetWindowDC(this.Handle);
			IntPtr hDC = Gdi32.CreateCompatibleDC(hdcWindow);
			IntPtr hBitmap = image.GetHbitmap(Color.FromArgb(0));
			IntPtr hOld = Gdi32.SelectObject(hDC, hBitmap);
			SIZE size = new SIZE
				            {
					            cx = 0,
					            cy = 0
				            };
			POINT zero = new POINT
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
			m_rect.Size = new Size(size.cx, size.cy);
			m_rect.Location = new Point(pos.x, pos.y);

			BLENDFUNCTION blend = new BLENDFUNCTION();
			blend.BlendOp = Convert.ToByte((int)AC.AC_SRC_OVER);
			blend.SourceConstantAlpha = opacity;
			blend.AlphaFormat = Convert.ToByte((int)AC.AC_SRC_ALPHA);
			blend.BlendFlags = 0;

			User32.UpdateLayeredWindow(this.Handle, hdcWindow, ref pos, ref size, hDC, ref zero, 0, ref blend, ULW.ULW_ALPHA);

			Gdi32.SelectObject(hDC, hOld);
			Gdi32.DeleteObject(hBitmap);
			Gdi32.DeleteDC(hDC);
			User32.ReleaseDC(this.Handle, hdcWindow);
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= (int)WS_EX.WS_EX_LAYERED;
				return cp;
			}
		}
	}
}
