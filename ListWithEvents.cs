using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace EasyTabs
{
	/// <summary>Represents a strongly typed list of objects with events.</summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	[Serializable]
	[DebuggerDisplay("Count = {Count}")]
	public class ListWithEvents<T> : List<T>, IList
	{
		/// <summary>Synchronization root for thread safety.</summary>
		private readonly object _syncRoot = new object();

		/// <summary>Flag indicating whether events are being suppressed during an operation.</summary>
		private bool _suppressEvents;

		/// <summary>Initializes a new instance of the <see cref="ListWithEvents{T}" /> class that is empty and has the default initial capacity.</summary>
		public ListWithEvents()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ListWithEvents{T}" /> class that contains elements copied from the specified collection and has
		/// sufficient capacity to accommodate the number of elements copied.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new list.</param>
		/// <exception cref="ArgumentNullException">The collection is null.</exception>
		public ListWithEvents(IEnumerable<T> collection)
			: base(collection)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ListWithEvents{T}" /> class that is empty and has the specified initial capacity.</summary>
		/// <param name="capacity">The number of elements that the new list can initially store.</param>
		/// <exception cref="ArgumentOutOfRangeException">The capacity is less than 0.</exception>
		public ListWithEvents(int capacity)
			: base(capacity)
		{
		}

		/// <summary>Gets whether the events are currently being suppressed.</summary>
		protected bool EventsSuppressed
		{
			get
			{
				return _suppressEvents;
			}
		}

		/// <summary>Overloads <see cref="List{T}.this" />.</summary>
		public new virtual T this[int index]
		{
			get
			{
				return base[index];
			}
			set
			{
				lock (_syncRoot)
				{
					bool equal = false;

					// ReSharper disable CompareNonConstrainedGenericWithNull
					if (base[index] != null)
					{
						equal = base[index].Equals(value);
					}

					else if (base[index] == null && value == null)
					{
						equal = true;
					}
					// ReSharper restore CompareNonConstrainedGenericWithNull

					if (!equal)
					{
						base[index] = value;
						OnItemModified(new ListItemEventArgs(index));
					}
				}
			}
		}

		/// <summary>Gets an object that can be used to synchronize access to the <see cref="ListWithEvents{T}" />.</summary>
		public object SyncRoot
		{
			get
			{
				return _syncRoot;
			}
		}

		/// <summary>Adds an item to the end of the list.</summary>
		/// <param name="value">Item to add to the list.</param>
		/// <returns>Index of the new item in the list.</returns>
		int IList.Add(object value)
		{
			if (value is T)
			{
				Add((T) value);
				return Count - 1;
			}

			return -1;
		}

		/// <summary>Overloads <see cref="List{T}.Clear" />.</summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public new virtual void Clear()
		{
			lock (_syncRoot)
			{
				base.Clear();
			}

			OnCleared(EventArgs.Empty);
		}

		/// <summary>Overloads <see cref="List{T}.RemoveAt" />.</summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public new virtual void RemoveAt(int index)
		{
			lock (_syncRoot)
			{
				base.RemoveAt(index);
			}

			OnItemRemoved(EventArgs.Empty);
		}

		/// <summary>Occurs whenever the list's content is modified.</summary>
		public event EventHandler<ListModificationEventArgs> CollectionModified;

		/// <summary>Occurs whenever the list is cleared.</summary>
		public event EventHandler Cleared;

		/// <summary>Occurs whenever a new item is added to the list.</summary>
		public event EventHandler<ListItemEventArgs> ItemAdded;

		/// <summary>Occurs whenever a item is modified.</summary>
		public event EventHandler<ListItemEventArgs> ItemModified;

		/// <summary>Occurs whenever an  item is removed from the list.</summary>
		public event EventHandler ItemRemoved;

		/// <summary>Occurs whenever a range of items is added to the list.</summary>
		public event EventHandler<ListRangeEventArgs> RangeAdded;

		/// <summary>Occurs whenever a range of items is removed from the list.</summary>
		public event EventHandler RangeRemoved;

		/// <summary>Overloads <see cref="List{T}.Add" />.</summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public new virtual void Add(T item)
		{
			int count;

			lock (_syncRoot)
			{
				base.Add(item);
				count = Count - 1;
			}

			OnItemAdded(new ListItemEventArgs(count));
		}

		/// <summary>Overloads <see cref="List{T}.AddRange" />.</summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public new virtual void AddRange(IEnumerable<T> collection)
		{
			lock (_syncRoot)
			{
				InsertRange(Count, collection);
			}
		}

		/// <summary>Overloads <see cref="List{T}.Insert" />.</summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public new virtual void Insert(int index, T item)
		{
			lock (_syncRoot)
			{
				base.Insert(index, item);
			}

			OnItemAdded(new ListItemEventArgs(index));
		}

		/// <summary>Overloads <see cref="List{T}.InsertRange" />.</summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public new virtual void InsertRange(int index, IEnumerable<T> collection)
		{
			int count;

			lock (_syncRoot)
			{
				base.InsertRange(index, collection);
				count = Count - index;
			}

			OnRangeAdded(new ListRangeEventArgs(index, count));
		}

		/// <summary>Overloads <see cref="List{T}.Remove" />.</summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public new virtual bool Remove(T item)
		{
			bool result;

			lock (_syncRoot)
			{
				result = base.Remove(item);
			}

			// Raise the event only if the removal was successful
			if (result)
			{
				OnItemRemoved(EventArgs.Empty);
			}

			return result;
		}

		/// <summary>Overloads <see cref="List{T}.RemoveAll" />.</summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public new virtual int RemoveAll(Predicate<T> match)
		{
			int count;

			lock (_syncRoot)
			{
				count = base.RemoveAll(match);
			}

			// Raise the event only if the removal was successful
			if (count > 0)
			{
				OnRangeRemoved(EventArgs.Empty);
			}

			return count;
		}

		/// <summary>Overloads <see cref="List{T}.RemoveRange" />.</summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public new virtual void RemoveRange(int index, int count)
		{
			int listCountOld, listCountNew;

			lock (_syncRoot)
			{
				listCountOld = Count;
				base.RemoveRange(index, count);
				listCountNew = Count;
			}

			// Raise the event only if the removal was successful
			if (listCountOld != listCountNew)
			{
				OnRangeRemoved(EventArgs.Empty);
			}
		}

		/// <summary>Removes the specified list of entries from the collection.</summary>
		/// <param name="collection">Collection to be removed from the list.</param>
		/// <remarks>
		/// This operation employs <see cref="Remove" /> method for removing each individual item which is thread-safe.  However overall operation isn't atomic,
		/// and hence does not guarantee thread-safety.
		/// </remarks>
		public virtual void RemoveRange(List<T> collection)
		{
			// ReSharper disable ForCanBeConvertedToForeach
			for (int i = 0; i < collection.Count; i++)
			{
				// ReSharper restore ForCanBeConvertedToForeach
				Remove(collection[i]);
			}
		}

		/// <summary>Stops raising events until <see cref="ResumeEvents" /> is called.</summary>
		public void SuppressEvents()
		{
			_suppressEvents = true;
		}

		/// <summary>Resumes raising events after <see cref="SuppressEvents" /> call.</summary>
		public void ResumeEvents()
		{
			_suppressEvents = false;
		}

		/// <summary>Raises <see cref="CollectionModified" /> and <see cref="Cleared" /> events.</summary>
		/// <param name="e">An <see cref="EventArgs" /> that contains the event data.</param>
		protected virtual void OnCleared(EventArgs e)
		{
			if (_suppressEvents)
			{
				return;
			}

			if (Cleared != null)
			{
				Cleared(this, e);
			}

			OnCollectionModified(new ListModificationEventArgs(ListModification.Cleared, -1, -1));
		}

		/// <summary>Raises <see cref="CollectionModified" /> events.</summary>
		/// <param name="e">An <see cref="ListModificationEventArgs" /> that contains the event data.</param>
		protected virtual void OnCollectionModified(ListModificationEventArgs e)
		{
			if (_suppressEvents)
			{
				return;
			}

			if (CollectionModified != null)
			{
				CollectionModified(this, e);
			}
		}

		/// <summary>Raises <see cref="CollectionModified" /> and <see cref="ItemAdded" /> events.</summary>
		/// <param name="e">An <see cref="ListItemEventArgs" /> that contains the event data.</param>
		protected virtual void OnItemAdded(ListItemEventArgs e)
		{
			if (_suppressEvents)
			{
				return;
			}

			if (ItemAdded != null)
			{
				ItemAdded(this, e);
			}

			OnCollectionModified(new ListModificationEventArgs(ListModification.ItemAdded, e.ItemIndex, 1));
		}

		/// <summary>Raises <see cref="CollectionModified" /> and <see cref="ItemModified" /> events.</summary>
		/// <param name="e">An <see cref="ListItemEventArgs" /> that contains the event data.</param>
		protected virtual void OnItemModified(ListItemEventArgs e)
		{
			if (_suppressEvents)
			{
				return;
			}

			if (ItemModified != null)
			{
				ItemModified(this, e);
			}

			OnCollectionModified(new ListModificationEventArgs(ListModification.ItemModified, e.ItemIndex, 1));
		}

		/// <summary>Raises <see cref="CollectionModified" /> and <see cref="ItemRemoved" /> events.</summary>
		/// <param name="e">An <see cref="EventArgs" /> that contains the event data.</param>
		protected virtual void OnItemRemoved(EventArgs e)
		{
			if (_suppressEvents)
			{
				return;
			}

			if (ItemRemoved != null)
			{
				ItemRemoved(this, e);
			}

			OnCollectionModified(new ListModificationEventArgs(ListModification.ItemRemoved, -1, 1));
		}

		/// <summary>Raises <see cref="CollectionModified" /> and <see cref="RangeAdded" /> events.</summary>
		/// <param name="e">An <see cref="ListRangeEventArgs" /> that contains the event data.</param>
		protected virtual void OnRangeAdded(ListRangeEventArgs e)
		{
			if (_suppressEvents)
			{
				return;
			}

			if (RangeAdded != null)
			{
				RangeAdded(this, e);
			}

			OnCollectionModified(new ListModificationEventArgs(ListModification.RangeAdded, e.StartIndex, e.Count));
		}

		/// <summary>Raises <see cref="CollectionModified" /> and <see cref="RangeRemoved" /> events.</summary>
		/// <param name="e">An <see cref="EventArgs" /> that contains the event data.</param>
		protected virtual void OnRangeRemoved(EventArgs e)
		{
			if (_suppressEvents)
			{
				return;
			}

			if (RangeRemoved != null)
			{
				RangeRemoved(this, e);
			}

			OnCollectionModified(new ListModificationEventArgs(ListModification.RangeRemoved, -1, -1));
		}
	}
}