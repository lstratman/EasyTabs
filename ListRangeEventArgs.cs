#region Revision History
//**********************************************************************//
// CtrlSoft, Copyright ©2001-2007, All rights reserved.
// 
// ListRangeEventArgs.cs
//
// Description:
//   - [TODO: Write the purpose of ListRangeEventArgs.cs.]
//
// Created On: 13/07/2007 07:10:21 PM
// Created By: Igor V. Velikorossov <mailto:igor@ctrlsoft.net> 
//**********************************************************************//

#endregion

using System;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	/// <summary>
	/// Provides data for the <see cref="ListWithEvents{T}.RangeAdded"/> events.
	/// </summary>
	[Serializable]
	public class ListRangeEventArgs : EventArgs
	{
		private int startIndex, count;

		#region public ListRangeEventArgs(int startIndex, int count)
		/// <summary>
		/// Initializes a new instance of the <see cref="ListRangeEventArgs"/> class.
		/// </summary>
		public ListRangeEventArgs(int startIndex, int count)
		{
			this.startIndex = startIndex;
			this.count = count;
		}
		#endregion // public ItemChangedEventArgs

		#region public int StartIndex
		/// <summary>
		/// Gets the index of the first item in the range.
		/// </summary>
		public int StartIndex
		{
			get { return this.startIndex; }
		}
		#endregion // public int ItemIndex

		#region public int Count
		/// <summary>
		/// Gets the number of items in the range.
		/// </summary>
		public int Count
		{
			get { return this.count; }
		}
		#endregion // public int ItemIndex

	}
}
