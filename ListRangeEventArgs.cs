using System;

namespace EasyTabs
{
	/// <summary>Provides data for the <see cref="ListWithEvents{T}.RangeAdded" /> events.</summary>
	[Serializable]
	public class ListRangeEventArgs : EventArgs
	{
		/// <summary>Number of items in the range.</summary>
		private readonly int _count;

		/// <summary>Index of the first item in the range.</summary>
		private readonly int _startIndex;

		/// <summary>Initializes a new instance of the <see cref="ListRangeEventArgs" /> class.</summary>
		/// <param name="startIndex">Index of the first item in the range.</param>
		/// <param name="count">Number of items in the range.</param>
		public ListRangeEventArgs(int startIndex, int count)
		{
			_startIndex = startIndex;
			_count = count;
		}

		/// <summary>Gets the index of the first item in the range.</summary>
		public int StartIndex
		{
			get
			{
				return _startIndex;
			}
		}

		/// <summary>Gets the number of items in the range.</summary>
		public int Count
		{
			get
			{
				return _count;
			}
		}
	}
}