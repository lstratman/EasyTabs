using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace EasyTabs
{
	/// <summary>Wraps a <see cref="Form" /> instance (<see cref="_content" />), that represents the content that should be displayed within a tab instance.</summary>
	public class TitleBarTab
	{
		/// <summary>Flag indicating whether or not this tab is active.</summary>
		protected bool _active;

		/// <summary>Content that should be displayed within the tab.</summary>
		protected Form _content;

		/// <summary>Parent window that contains this tab.</summary>
		protected TitleBarTabs _parent;

		/// <summary>Default constructor that initializes the various properties.</summary>
		/// <param name="parent">Parent window that contains this tab.</param>
		public TitleBarTab(TitleBarTabs parent)
		{
			ShowCloseButton = true;
			Parent = parent;
		}

		/// <summary>Parent window that contains this tab.</summary>
		public TitleBarTabs Parent
		{
			get
			{
				return _parent;
			}

			internal set
			{
				_parent = value;

				if (_content != null)
				{
					_content.Parent = _parent;
				}
			}
		}

		/// <summary>Flag indicating whether or not we should display the close button for this tab.</summary>
		public bool ShowCloseButton
		{
			get;
			set;
		}

		/// <summary>The caption that's displayed in the tab's title (simply uses the <see cref="Form.Text" /> of
		/// <see cref="Content" />).</summary>
		public string Caption
		{
			get
			{
				return Content.Text;
			}

			set
			{
				Content.Text = value;
			}
		}

		/// <summary>Flag indicating whether or not this tab is active.</summary>
		public bool Active
		{
			get
			{
				return _active;
			}

			internal set
			{
				// When the status of the tab changes, we null out the TabImage property so that it's recreated in the next rendering pass
				_active = value;
				TabImage = null;
				Content.Visible = value;
			}
		}

		/// <summary>The icon that's displayed in the tab's title (simply uses the <see cref="Form.Icon" /> of <see cref="Content" />).</summary>
		public Icon Icon
		{
			get
			{
				return Content.Icon;
			}

			set
			{
				Content.Icon = value;
			}
		}

		/// <summary>The area in which the tab is rendered in the client window.</summary>
		internal Rectangle Area
		{
			get;
			set;
		}

		/// <summary>The area of the close button for this tab in the client window.</summary>
		internal Rectangle CloseButtonArea
		{
			get;
			set;
		}

		/// <summary>Pre-rendered image of the tab's background.</summary>
		internal Bitmap TabImage
		{
			get;
			set;
		}

		/// <summary>The content that should be displayed for this tab.</summary>
		public Form Content
		{
			get
			{
				return _content;
			}

			set
			{
				if (_content != null)
				{
					_content.FormClosing -= Content_Closing;
					_content.TextChanged -= Content_TextChanged;
				}

				_content = value;

				// We set the content form to a non-top-level child of the parent form.
				Content.FormBorderStyle = FormBorderStyle.None;
				Content.TopLevel = false;
				Content.Parent = Parent;
				Content.FormClosing += Content_Closing;
				Content.TextChanged += Content_TextChanged;
			}
		}

		/// <summary>
		/// Called from <see cref="TornTabForm" /> when we need to generate a thumbnail for a tab when it is torn out of its parent window.  We simply call
		/// <see cref="Graphics.CopyFromScreen(System.Drawing.Point,System.Drawing.Point,System.Drawing.Size)" /> to copy the screen contents to a
		/// <see cref="Bitmap" />.
		/// </summary>
		/// <returns>An image of the tab's contents.</returns>
		public virtual Bitmap GetImage()
		{
			Bitmap tabContents = new Bitmap(Content.Size.Width, Content.Size.Height);
			Graphics contentsGraphics = Graphics.FromImage(tabContents);

			contentsGraphics.CopyFromScreen(Content.PointToScreen(Point.Empty).X, Content.PointToScreen(Point.Empty).Y, 0, 0, Content.Size);

			return tabContents;
		}

		/// <summary>Event that is fired when <see cref="Content" />'s <see cref="Form.Closing" /> event is fired.</summary>
		public event CancelEventHandler Closing;

		/// <summary>Event that is fired when <see cref="Content" />'s <see cref="Control.TextChanged" /> event is fired.</summary>
		public event EventHandler TextChanged;

		/// <summary>
		/// Event handler that is invoked when <see cref="Content" />'s <see cref="Control.TextChanged" /> event is fired, which in turn fires this class'
		/// <see cref="TextChanged" /> event.
		/// </summary>
		/// <param name="sender">Object from which this event originated (<see cref="Content" /> in this case).</param>
		/// <param name="e">Arguments associated with the event.</param>
		private void Content_TextChanged(object sender, EventArgs e)
		{
			if (TextChanged != null)
			{
				TextChanged(this, e);
			}
		}

		/// <summary>
		/// Event handler that is invoked when <see cref="Content" />'s <see cref="Form.Closing" /> event is fired, which in turn fires this class'
		/// <see cref="Closing" /> event.
		/// </summary>
		/// <param name="sender">Object from which this event originated (<see cref="Content" /> in this case).</param>
		/// <param name="e">Arguments associated with the event.</param>
		protected void Content_Closing(object sender, CancelEventArgs e)
		{
			if (Closing != null)
			{
				Closing(this, e);
			}
		}

		/// <summary>Unsubscribes the tab from any event handlers that may have been attached to its <see cref="Closing" /> or <see cref="TextChanged" /> events.</summary>
		public void ClearSubscriptions()
		{
			Closing = null;
			TextChanged = null;
		}
	}
}