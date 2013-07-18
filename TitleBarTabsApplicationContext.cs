using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	public class TitleBarTabsApplicationContext : ApplicationContext
	{
		/// <summary>
		/// List of all opened windows.
		/// </summary>
		protected List<TitleBarTabs> _openWindows = new List<TitleBarTabs>();

		/// <summary>
		/// Constructor; provides the arguments to <typeparamref name="T"/> constructor in the form of <paramref name="bookmarkGuids"/> and 
		/// <paramref name="historyGuid"/>.
		/// </summary>
		/// <param name="bookmarkGuids">List of all bookmarks to open by default.</param>
		/// <param name="historyGuid">The history item to open by default.</param>
		public void Start(TitleBarTabs initialFormInstance)
		{
			if (initialFormInstance.IsClosing)
				ExitThread();

			else
			{
				OpenWindow(initialFormInstance);
				initialFormInstance.Show();
			}
		}

		/// <summary>
		/// Adds <paramref name="window"/> to <see cref="_openWindows"/> and attaches event handlers to its <see cref="Form.FormClosed"/> event to keep
		/// track of it.
		/// </summary>
		/// <param name="window">Window that we're opening.</param>
		public void OpenWindow(TitleBarTabs window)
		{
			if (!_openWindows.Contains(window))
			{
				window.ApplicationContext = this;

				_openWindows.Add(window);
				window.FormClosed += window_FormClosed;
			}
		}

		/// <summary>
		/// Handler method that's called when an item in <see cref="_openWindows"/> has its <see cref="Form.FormClosed"/> event invoked.  Removes the 
		/// window from <see cref="_openWindows"/> and, if there are no more windows open, calls <see cref="ApplicationContext.ExitThread"/>.
		/// </summary>
		/// <param name="sender">Object from which this event originated.</param>
		/// <param name="e">Arguments associated with the event.</param>
		protected void window_FormClosed(object sender, FormClosedEventArgs e)
		{
			_openWindows.Remove((TitleBarTabs)sender);

			if (_openWindows.Count == 0)
				ExitThread();
		}
	}
}
