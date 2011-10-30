#region Revision History
//**********************************************************************//
// CtrlSoft, Copyright ©2001-2007, All rights reserved.
// 
// ListWithEvents.cs
//
// Description:
//   - [TODO: Write the purpose of ListWithEvents.cs.]
//
// Created On: 08/04/2006 07:10:21 PM
// Created By: Igor V. Velikorossov <mailto:igor@ctrlsoft.net> 
//**********************************************************************//
// 
// Updated On: 16/11/2006 09:47:37 PM
// By: Igor V. Velikorossov <mailto:igor@ctrlsoft.net>
//   - Added CollectionModified event.
//
//**********************************************************************//
// 
// Updated On: 01/06/2007 09:47:37 PM
// By: Igor V. Velikorossov <mailto:igor@ctrlsoft.net>
//   - Added OnCollectionModified method.
//	 - Removed custom event handlers and replaced with EventHandler<T> defs
//
//**********************************************************************//
// 
// Updated On: 13/07/2007 09:47:37 PM
// By: Igor V. Velikorossov <mailto:igor@ctrlsoft.net>
//   - Added thread safety and SyncRoot property
//   - Marked all shadowed methods as virtual, so any descendants can override them.
//   - Added explicit implementation of IList.Add() method to ensure proper 
//		member addition through a collection editor UI which performs Add operation 
//		through the interface.
//
//**********************************************************************//

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	/// <summary>
	/// Represents a strongly typed list of objects with events. 
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	[Serializable, DebuggerDisplay("Count = {Count}")]
	public class ListWithEvents<T> : List<T>, IList
	{
		private bool	suppressEvents;
		private object	syncRoot		= new object();

		/// <summary>
		/// Occurs whenever the list's content is modified.
		/// </summary>
		public event EventHandler<ListModificationEventArgs>	CollectionModified;
		/// <summary>
		/// Occurs whenever the list is cleared.
		/// </summary>
		public event EventHandler						Cleared;
		/// <summary>
		/// Occurs whenever a new item is added to the list.
		/// </summary>
		public event EventHandler<ListItemEventArgs>	ItemAdded;
		/// <summary>
		/// Occurs whenever a item is modified.
		/// </summary>
		public event EventHandler<ListItemEventArgs>	ItemModified;
		/// <summary>
		/// Occurs whenever an  item is removed from the list.
		/// </summary>
		public event EventHandler						ItemRemoved;
		/// <summary>
		/// Occurs whenever a range of items is added to the list.
		/// </summary>
		public event EventHandler<ListRangeEventArgs>	RangeAdded;
		/// <summary>
		/// Occurs whenever a range of items is removed from the list.
		/// </summary>
		public event EventHandler						RangeRemoved;


		#region public ListWithEvents()
		/// <summary>
		/// Initializes a new instance of the <see cref="ListWithEvents{T}"/> class
		/// that is empty and has the default initial capacity.
		/// </summary>
		public ListWithEvents()
			: base()
		{
		}
		#endregion // public ListWithEvents()

		#region public ListWithEvents(IEnumerable<T> collection) : base(collection)
		/// <summary>
		/// Initializes a new instance of the <see cref="ListWithEvents{T}"/> class 
		/// that contains elements copied from the specified collection and has sufficient
		/// capacity to accommodate the number of elements copied.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new list.</param>
		/// <exception cref="ArgumentNullException">The collection is null.</exception>
		public ListWithEvents(IEnumerable<T> collection)
			: base(collection)
		{
		}
		#endregion // public ListWithEvents(IEnumerable<T> collection)

		#region public ListWithEvents(int capacity) : base(capacity)
		/// <summary>
		/// Initializes a new instance of the <see cref="ListWithEvents{T}"/> class
		/// that is empty and has the specified initial capacity.
		/// </summary>
		/// <param name="capacity">The number of elements that the new list can initially store.</param>
		/// <exception cref="ArgumentOutOfRangeException">The capacity is less than 0.</exception>
		public ListWithEvents(int capacity) 
			: base(capacity)
		{
		}
		#endregion // public ListWithEvents(int capacity)


		#region protected bool EventsSuppressed
		/// <summary>
		/// Gets wthether the events are currently being suppressed.
		/// </summary>
		protected bool EventsSuppressed
		{
			get { return this.suppressEvents; }
		}
		#endregion // protected bool EventsSuppressed


		#region public virtual new T this[int index]
		/// <summary>
		/// Overloads <see cref="List{T}.this"/>.
		/// </summary>
		public virtual new T this[int index]
		{
			get { return base[index]; }
			set
			{
				lock (this.syncRoot)
				{
					bool equal = false;
					if (base[index] != null)
					{
						equal = base[index].Equals(value);
					}
					else if (base[index] == null && value == null)
					{
						equal = true;
					}

					if (!equal)
					{
						base[index] = value;
						this.OnItemModified(new ListItemEventArgs(index));
					}
				}
			}
		}
		#endregion // public virtual new T this[int index]

		#region public object SyncRoot
		/// <summary>
		/// Gets an object that can be used to synchronize access to the <see cref="ListWithEvents{T}"/>. 
		/// </summary>
		public object SyncRoot
		{
			get { return this.syncRoot; }
		}
		#endregion // public object SyncRoot


		#region public virtual new void Add(T item)
		/// <summary>
		/// Overloads <see cref="List{T}.Add"/>.
		/// </summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public virtual new void Add(T item)
		{
			int count;
			lock (this.syncRoot)
			{
				base.Add(item);
				count = base.Count - 1;
			}
			this.OnItemAdded(new ListItemEventArgs(count));
		}
		#endregion // public virtual new void Add(T item)

		#region public virtual new void AddRange(IEnumerable<T> collection)
		/// <summary>
		/// Overloads <see cref="List{T}.AddRange"/>.
		/// </summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public virtual new void AddRange(IEnumerable<T> collection)
		{
			lock (this.syncRoot)
			{
				this.InsertRange(base.Count, collection);
			}
		}
		#endregion // public virtual new void AddRange(IEnumerable<T> collection)

		#region public virtual new void Clear()
		/// <summary>
		/// Overloads <see cref="List{T}.Clear"/>.
		/// </summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public virtual new void Clear()
		{
			lock (this.syncRoot)
			{
				base.Clear();
			}
			this.OnCleared(EventArgs.Empty);
		}
		#endregion // public virtual new void Clear()

		#region public virtual new void Insert(int index, T item)
		/// <summary>
		/// Overloads <see cref="List{T}.Insert"/>.
		/// </summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public virtual new void Insert(int index, T item)
		{
			lock (this.syncRoot)
			{
				base.Insert(index, item);
			}
			this.OnItemAdded(new ListItemEventArgs(index));
		}
		#endregion // public virtual new void Insert(int index, T item)

		#region public virtual new void InsertRange(int index, IEnumerable<T> collection)
		/// <summary>
		/// Overloads <see cref="List{T}.InsertRange"/>.
		/// </summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public virtual new void InsertRange(int index, IEnumerable<T> collection)
		{
			int count;
			lock (this.syncRoot)
			{
				base.InsertRange(index, collection);
				count = base.Count - index;
			}
			this.OnRangeAdded(new ListRangeEventArgs(index, count));
		}
		#endregion // public virtual new void InsertRange(int index, IEnumerable<T> collection)

		#region public virtual new bool Remove(T item)
		/// <summary>
		/// Overloads <see cref="List{T}.Remove"/>.
		/// </summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public virtual new bool Remove(T item)
		{
			bool result;
			
			lock (this.syncRoot)
			{
				result = base.Remove(item);
			}

			// raise the event only if the removal was successful
			if (result)
			{
				this.OnItemRemoved(EventArgs.Empty);
			}

			return result;
		}
		#endregion // public virtual new bool Remove(T item)

		#region public virtual new int RemoveAll(Predicate<T> match)
		/// <summary>
		/// Overloads <see cref="List{T}.RemoveAll"/>.
		/// </summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public virtual new int RemoveAll(Predicate<T> match)
		{
			int count;
			
			lock (this.syncRoot)
			{
				count = base.RemoveAll(match);
			}

			// raise the event only if the removal was successful
			if (count > 0)
			{
				this.OnRangeRemoved(EventArgs.Empty);
			}

			return count;
		}
		#endregion // public virtual new int RemoveAll(Predicate<T> match)

		#region public virtual new void RemoveAt(int index)
		/// <summary>
		/// Overloads <see cref="List{T}.RemoveAt"/>.
		/// </summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public virtual new void RemoveAt(int index)
		{
			lock (this.syncRoot)
			{
				base.RemoveAt(index);
			}
			this.OnItemRemoved(EventArgs.Empty);
		}
		#endregion // public virtual new void RemoveAt(int index)

		#region public virtual new void RemoveRange(int index, int count)
		/// <summary>
		/// Overloads <see cref="List{T}.RemoveRange"/>.
		/// </summary>
		/// <remarks>This operation is thread-safe.</remarks>
		public virtual new void RemoveRange(int index, int count)
		{
			int listCountOld, listCountNew;
			lock (this.syncRoot)
			{
				listCountOld = base.Count;
				base.RemoveRange(index, count);
				listCountNew = base.Count;
			}

			// raise the event only if the removal was successful
			if (listCountOld != listCountNew)
			{
				this.OnRangeRemoved(EventArgs.Empty);
			}
		}
		#endregion // public virtual new void RemoveRange(int index, int count)

		#region public virtual void RemoveRange(List<T> collection)
		/// <summary>
		/// Removes the specified list of entries from the collection.
		/// </summary>
		/// <param name="collection">Collection to be removed from the list.</param>
		/// <remarks>
		/// This operation employs <see cref="Remove"/> method for removing 
		/// each individual item which is thread-safe. However overall operation isn't atomic,
		/// and hence does not guarantee thread-safety.
		/// </remarks>
		public virtual void RemoveRange(List<T> collection)
		{
			for (int i = 0; i < collection.Count; i++)
			{
				this.Remove(collection[i]);
			}
		}
		#endregion // public void RemoveRange(TimeSheetEntryCollection entries)


		#region public void SuppressEvents()
		/// <summary>
		/// Stops raising events until <see cref="ResumeEvents"/> is called.
		/// </summary>
		public void SuppressEvents()
		{
			this.suppressEvents = true;
		}
		#endregion // public void SuppressEvents()

		#region public void ResumeEvents()
		/// <summary>
		/// Resumes raising events after <see cref="SuppressEvents"/> call.
		/// </summary>
		public void ResumeEvents()
		{
			this.suppressEvents = false;
		}
		#endregion // public void ResumeEvents()


		#region protected virtual void OnCleared(EventArgs e)
		/// <summary>
		/// Raises <see cref="CollectionModified"/> and <see cref="Cleared"/> events.
		/// </summary>
		/// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
		protected virtual void OnCleared(EventArgs e)
		{
			if (this.suppressEvents)
				return;

			if (Cleared != null)
			{
				Cleared(this, e);
			}

			this.OnCollectionModified(new ListModificationEventArgs(ListModification.Cleared, -1, -1));
		}
		#endregion // protected virtual void OnCleared(EventArgs e)

		#region protected virtual void OnCollectionModified(ListModificationEventArgs e)
		/// <summary>
		/// Raises <see cref="CollectionModified"/> events.
		/// </summary>
		/// <param name="e">An <see cref="ListModificationEventArgs"/> that contains the event data.</param>
		protected virtual void OnCollectionModified(ListModificationEventArgs e)
		{
			if (this.suppressEvents)
				return;

			if (CollectionModified != null)
			{
				CollectionModified(this, e);
			}
		}
		#endregion // protected virtual void OnCollectionModified(EventArgs e)

		#region protected virtual void OnItemAdded(ListItemEventArgs e)
		/// <summary>
		/// Raises <see cref="CollectionModified"/> and <see cref="ItemAdded"/> events.
		/// </summary>
		/// <param name="e">An <see cref="ListItemEventArgs"/> that contains the event data.</param>
		protected virtual void OnItemAdded(ListItemEventArgs e)
		{
			if (this.suppressEvents)
				return;

			if (ItemAdded != null)
			{
				ItemAdded(this, e);
			}

			this.OnCollectionModified(new ListModificationEventArgs(ListModification.ItemAdded, e.ItemIndex, 1));
		}
		#endregion // protected virtual void OnItemAdded(ListItemEventArgs e)

		#region protected virtual void OnItemModified(ListItemEventArgs e)
		/// <summary>
		/// Raises <see cref="CollectionModified"/> and <see cref="ItemModified"/> events.
		/// </summary>
		/// <param name="e">An <see cref="ListItemEventArgs"/> that contains the event data.</param>
		protected virtual void OnItemModified(ListItemEventArgs e)
		{
			if (this.suppressEvents)
				return;

			if (ItemModified != null)
			{
				ItemModified(this, e);
			}

			this.OnCollectionModified(new ListModificationEventArgs(ListModification.ItemModified, e.ItemIndex, 1));
		}
		#endregion // protected virtual void OnItemModified(ListItemEventArgs e)

		#region protected virtual void OnItemRemoved(EventArgs e)
		/// <summary>
		/// Raises <see cref="CollectionModified"/> and <see cref="ItemRemoved"/> events.
		/// </summary>
		/// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
		protected virtual void OnItemRemoved(EventArgs e)
		{
			if (this.suppressEvents)
				return;

			if (ItemRemoved != null)
			{
				ItemRemoved(this, e);
			}

			this.OnCollectionModified(new ListModificationEventArgs(ListModification.ItemRemoved, -1, 1));
		}
		#endregion // protected virtual void OnItemRemoved(ListItemEventArgs e)

		#region protected virtual void OnRangeAdded(ListRangeEventArgs e)
		/// <summary>
		/// Raises <see cref="CollectionModified"/> and <see cref="RangeAdded"/> events.
		/// </summary>
		/// <param name="e">An <see cref="ListRangeEventArgs"/> that contains the event data.</param>
		protected virtual void OnRangeAdded(ListRangeEventArgs e)
		{
			if (this.suppressEvents)
				return;

			if (RangeAdded != null)
			{
				RangeAdded(this, e);
			}

			this.OnCollectionModified(new ListModificationEventArgs(ListModification.RangeAdded, e.StartIndex, e.Count));
		}
		#endregion // protected virtual void OnRangeAdded(ListRangeEventArgs e)

		#region protected virtual void OnRangeRemoved(EventArgs e)
		/// <summary>
		/// Raises <see cref="CollectionModified"/> and <see cref="RangeRemoved"/> events.
		/// </summary>
		/// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
		protected virtual void OnRangeRemoved(EventArgs e)
		{
			if (this.suppressEvents)
				return;

			if (RangeRemoved != null)
			{
				RangeRemoved(this, e);
			}

			this.OnCollectionModified(new ListModificationEventArgs(ListModification.RangeRemoved, -1, -1));
		}
		#endregion // protected virtual void OnItemRemoved(ListItemEventArgs e)


		// need to implement explicit IList.Add() to accomodate CollectionEditor control,
		// and ensure that we get events wired to when new objects are added.
		int System.Collections.IList.Add(object value)
		{
			if (value is T)
			{
				this.Add((T)value);
				return this.Count - 1;
			}
			return -1;
		}

		
	}


}
