﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Xamarin.Forms
{

	internal abstract class ShellElementCollection :
		IList<BaseShellItem>,
		INotifyCollectionChanged
	{
		public event NotifyCollectionChangedEventHandler VisibleItemsChangedInternal;
		readonly List<NotifyCollectionChangedEventArgs> _notifyCollectionChangedEventArgs;
		bool _pauseCollectionChanged;
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		public event NotifyCollectionChangedEventHandler VisibleItemsChanged;
		public int Count => Inner.Count;
		public bool IsReadOnly => Inner.IsReadOnly;
		IList _inner;
		IList _visibleItems;

		protected ShellElementCollection()
		{
			_notifyCollectionChangedEventArgs = new List<NotifyCollectionChangedEventArgs>();
		}

		internal IList Inner
		{
			get => _inner;
			private protected set
			{
				if (_inner != null)
					throw new ArgumentException("Inner can only be set once");

				_inner = value;
				((INotifyCollectionChanged)_inner).CollectionChanged += InnerCollectionChanged;
			}
		}

		protected void OnVisibleItemsChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			if (_pauseCollectionChanged)
			{
				_notifyCollectionChangedEventArgs.Add(args);
				return;
			}

			VisibleItemsChanged?.Invoke(VisibleItemsReadOnly, args);
			VisibleItemsChangedInternal?.Invoke(VisibleItemsReadOnly, args);
		}


		protected IList VisibleItems
		{
			get => _visibleItems;
			private protected set
			{
				_visibleItems = value;
				((INotifyCollectionChanged)_visibleItems).CollectionChanged += OnVisibleItemsChanged;
			}
		}

		public IReadOnlyCollection<BaseShellItem> VisibleItemsReadOnly
		{
			get;
			private protected set;
		}


		void PauseCollectionChanged() => _pauseCollectionChanged = true;


		void ResumeCollectionChanged()
		{
			_pauseCollectionChanged = false;

			var pendingEvents = _notifyCollectionChangedEventArgs.ToList();
			_notifyCollectionChangedEventArgs.Clear();

			foreach (var args in pendingEvents)
				OnVisibleItemsChanged(this, args);
		}

		#region IList

		public BaseShellItem this[int index]
		{
			get => (BaseShellItem)Inner[index];
			set => Inner[index] = value;
		}

		public void Clear()
		{
			try
			{
				PauseCollectionChanged();
				var list = Inner.Cast<BaseShellItem>().ToList();
				RemoveInnerCollection();
				Inner.Clear();
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, list));

			}
			finally
			{
				ResumeCollectionChanged();
			}
		}

		public virtual void Add(BaseShellItem item) => Inner.Add(item);

		public virtual bool Contains(BaseShellItem item) => Inner.Contains(item);

		public virtual void CopyTo(BaseShellItem[] array, int arrayIndex) => Inner.CopyTo(array, arrayIndex);

		public abstract IEnumerator<BaseShellItem> GetEnumerator();

		public virtual int IndexOf(BaseShellItem item) => Inner.IndexOf(item);

		public virtual void Insert(int index, BaseShellItem item) => Inner.Insert(index, item);

		public abstract bool Remove(BaseShellItem item);

		public virtual void RemoveAt(int index) => Inner.RemoveAt(index);

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Inner).GetEnumerator();

		#endregion

		void InnerCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (BaseShellItem element in e.NewItems)
				{
					if (element is IElementController controller)
						OnElementControllerInserting(controller);

					CheckVisibility(element);
				}
			}

			if (e.OldItems != null)
			{
				Removing(e.OldItems);
			}

			CollectionChanged?.Invoke(this, e);
		}

		void Removing(IEnumerable items)
		{
			foreach (BaseShellItem element in items)
			{
				if (VisibleItems.Contains(element))
					VisibleItems.Remove(element);

				if (element is IElementController controller)
					OnElementControllerRemoving(controller);
			}
		}

		protected void RemoveInnerCollection()
		{
			Removing(Inner);
		}

		protected virtual void CheckVisibility(BaseShellItem element)
		{
			if (IsShellElementVisible(element))
			{
				if (VisibleItems.Contains(element))
					return;

				int visibleIndex = 0;
				for (var i = 0; i < Inner.Count; i++)
				{
					var item = Inner[i];

					if (!IsShellElementVisible(element))
						continue;

					if (item == element)
					{
						VisibleItems.Insert(visibleIndex, element);
						break;
					}

					if (VisibleItems.Contains(item))
						visibleIndex++;
				}
			}
			else if (VisibleItems.Contains(element))
			{
				VisibleItems.Remove(element);
			}
		}

		protected virtual bool IsShellElementVisible(BaseShellItem item)
		{
			if (item is ShellGroupItem sgi)
			{
				return (sgi.ShellElementCollection.VisibleItemsReadOnly.Count > 0) ||
					item is IMenuItemController;
			}

			return false;
		}


		protected virtual void OnElementControllerInserting(IElementController controller)
		{
			if (controller is ShellGroupItem sgi)
			{
				sgi.ShellElementCollection.VisibleItemsChanged += OnShellElementControllerItemsCollectionChanged;
			}
		}

		protected virtual void OnElementControllerRemoving(IElementController controller)
		{
			if (controller is ShellGroupItem sgi)
			{
				sgi.ShellElementCollection.VisibleItemsChanged -= OnShellElementControllerItemsCollectionChanged;
			}
		}

		void OnShellElementControllerItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			foreach (BaseShellItem section in (e.NewItems ?? e.OldItems ?? (IList)Inner))
			{
				if (section.Parent == null)
					section.ParentSet += OnParentSet;
				else
					CheckVisibility(section.Parent as BaseShellItem);
			}

			void OnParentSet(object s, System.EventArgs __)
			{
				var shellSection = (BaseShellItem)s;
				shellSection.ParentSet -= OnParentSet;
				CheckVisibility(shellSection.Parent as BaseShellItem);
			}
		}

	}

	internal abstract class ShellElementCollection<TBaseShellItem> :
		ShellElementCollection,
		IList<TBaseShellItem>		
		where TBaseShellItem : BaseShellItem
	{

		public ShellElementCollection()
		{
			var items = new ObservableCollection<TBaseShellItem>();
			VisibleItems = items;
			VisibleItemsReadOnly = new ReadOnlyCollection<TBaseShellItem>(items);
		}

		public new ReadOnlyCollection<TBaseShellItem> VisibleItemsReadOnly
		{
			get => (ReadOnlyCollection<TBaseShellItem>)base.VisibleItemsReadOnly;
			private protected set => base.VisibleItemsReadOnly = value;
		}

		internal new IList<TBaseShellItem> Inner
		{
			get => (IList<TBaseShellItem>)base.Inner;
			set => base.Inner = (IList)value;
		}


		TBaseShellItem IList<TBaseShellItem>.this[int index]
		{
			get => (TBaseShellItem)Inner[index];
			set => Inner[index] = value;
		}

		public virtual void Add(TBaseShellItem item) => Inner.Add(item);

		public virtual bool Contains(TBaseShellItem item) => Inner.Contains(item);

		public virtual void CopyTo(TBaseShellItem[] array, int arrayIndex) => Inner.CopyTo(array, arrayIndex);

		public virtual int IndexOf(TBaseShellItem item) => Inner.IndexOf(item);

		public virtual void Insert(int index, TBaseShellItem item) => Inner.Insert(index, item);

		public virtual bool Remove(TBaseShellItem item) => Inner.Remove(item);

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Inner).GetEnumerator();

		IEnumerator<TBaseShellItem> IEnumerable<TBaseShellItem>.GetEnumerator()
		{
			return Inner.GetEnumerator();
		}

		public override IEnumerator<BaseShellItem> GetEnumerator()
		{
			return Inner.Cast<BaseShellItem>().GetEnumerator();
		}

		public override bool Remove(BaseShellItem item)
		{
			return Remove((TBaseShellItem)item);
		}
	}
}
