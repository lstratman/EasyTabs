using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Win32Interop.Enums;
using Win32Interop.Methods;
using Win32Interop.Structs;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	public partial class TornTabForm : Form
	{
		protected bool _hookInstalled = false;

		/// <summary>
		/// Pointer to the low-level mouse hook callback (<see cref="MouseHookCallback"/>).
		/// </summary>
		protected IntPtr _hookId;

		/// <summary>
		/// Delegate of <see cref="MouseHookCallback"/>; declared as a member variable to keep it from being garbage collected.
		/// </summary>
		protected HOOKPROC _hookproc = null;

		public TornTabForm(TitleBarTab tab)
		{
			InitializeComponent();

			Disposed +=TornTabForm_Disposed;

			Bitmap tabContents = tab.GetImage();
			Bitmap tabThumbnail = new Bitmap(tabContents.Width / 2, tabContents.Height / 2);
			Graphics thumbnailGraphics = Graphics.FromImage(tabThumbnail);

			thumbnailGraphics.InterpolationMode = InterpolationMode.High;
			thumbnailGraphics.CompositingQuality = CompositingQuality.HighQuality;
			thumbnailGraphics.SmoothingMode = SmoothingMode.AntiAlias;
			thumbnailGraphics.DrawImage(tabContents, 0, 0, tabThumbnail.Width, tabThumbnail.Height);

			Width = tabThumbnail.Width - 1;
			Height = tabThumbnail.Height - 1;

			_tabThumbnail.Image = tabThumbnail;
		}

		void TornTabForm_Disposed(object sender, EventArgs e)
		{
			User32.UnhookWindowsHookEx(_hookId);
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

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

				Left = cursorPosition.X;
				Top = cursorPosition.Y;
			}

			return User32.CallNextHookEx(_hookId, nCode, wParam, lParam);
		}
	}
}
