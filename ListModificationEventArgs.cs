#region Revision History
//**********************************************************************//
// CtrlSoft, Copyright ©2001-2007, All rights reserved.
// 
// ListModificationEventArgs.cs
//
// Description:
//   - [TODO: Write the purpose of ListModificationEventArgs.cs.]
//
// Created On: 13/07/2007 07:10:21 PM
// Created By: Igor V. Velikorossov <mailto:igor@ctrlsoft.net> 
//**********************************************************************//

#endregion

using System;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	/// <summary>
	/// Provides data for the <see cref="ListWithEvents{T}.CollectionModified"/> events.
	/// </summary>
	[Serializable]
	public class ListModificationEventArgs : ListRangeEventArgs
	{
		private ListModification modification;


		#region public ListModificationEventArgs(ListModification modification, int startIndex, int count) : base(startIndex, count)
		/// <summary>
		/// Initializes a new instance of the <see cref="ListModificationEventArgs"/> class.
		/// </summary>
		public ListModificationEventArgs(ListModification modification, int startIndex, int count)
			: base(startIndex, count)
		{
			this.modification = modification;
		}
		#endregion // public ListModificationEventArgs


		#region public ListModification Modification
		/// <summary>
		/// Gets the type of list modification.
		/// </summary>
		public ListModification Modification
		{
			get { return this.modification; }
		}
		#endregion // public int ItemIndex
	}
}
