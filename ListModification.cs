#region Revision History
//**********************************************************************//
// CtrlSoft, Copyright ©2001-2007, All rights reserved.
// 
// ListModification.cs
//
// Description:
//   - [TODO: Write the purpose of ListModification.cs.]
//
// Created On: 13/07/2007 07:10:21 PM
// Created By: Igor V. Velikorossov <mailto:igor@ctrlsoft.net> 
//**********************************************************************//

#endregion

using System;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	/// <summary>
	/// List of possible <see cref="ListWithEvents{T}"/> modifications.
	/// </summary>
	public enum ListModification
	{
		/// <summary>
		/// The list has been cleared.
		/// </summary>
		Cleared			= 0,
		/// <summary>
		/// A new item has been added.
		/// </summary>
		ItemAdded,
		/// <summary>
		/// An item has been modified.
		/// </summary>
		ItemModified,
		/// <summary>
		/// An item has been removed.
		/// </summary>
		ItemRemoved,
		/// <summary>
		/// A range of items has been added.
		/// </summary>
		RangeAdded,
		/// <summary>
		/// A range of items has been removed.
		/// </summary>
		RangeRemoved
	}
}
