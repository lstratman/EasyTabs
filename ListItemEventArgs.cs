using System;

namespace EasyTabs
{
	/// <summary>Provides data for the <see cref="ListWithEvents{T}.ItemAdded" /> events.</summary>
	[Serializable]
	public class ListItemEventArgs : EventArgs
	{
		/// <summary>Index of the item being changed.</summary>
		private readonly int _itemIndex;

		/// <summary>Initializes a new instance of the <see cref="ListItemEventArgs" /> class.</summary>
		/// <param name="itemIndex">Index of the item being changed.</param>
		public ListItemEventArgs(int itemIndex)
		{
			_itemIndex = itemIndex;
		}

		/// <summary>Gets the index of the item changed.</summary>
		public int ItemIndex
		{
			get
			{
				return _itemIndex;
			}
		}
	}
}