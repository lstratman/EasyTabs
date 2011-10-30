#region Revision History
//**********************************************************************//
// CtrlSoft, Copyright ©2001-2007, All rights reserved.
// 
// ListItemEventArgs.cs
//
// Description:
//   - [TODO: Write the purpose of ListItemEventArgs.cs.]
//
// Created On: 13/07/2007 07:10:21 PM
// Created By: Igor V. Velikorossov <mailto:igor@ctrlsoft.net> 
//**********************************************************************//

#endregion

using System;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	/// <summary>
	/// Provides data for the <see cref="ListWithEvents{T}.ItemAdded"/> events.
	/// </summary>
	[Serializable]
	public class ListItemEventArgs : EventArgs
	{
		private int itemIndex;

		#region public ListItemEventArgs(int itemIndex)
		/// <summary>
		/// Initializes a new instance of the <see cref="ListItemEventArgs"/> class.
		/// </summary>
		public ListItemEventArgs(int itemIndex)
		{
			this.itemIndex = itemIndex;
		}
		#endregion // public ItemChangedEventArgs

		#region public int ItemIndex
		/// <summary>
		/// Gets the index of the item changed.
		/// </summary>
		public int ItemIndex
		{
			get { return itemIndex; }
		}
		#endregion // public int ItemIndex

	}
}
