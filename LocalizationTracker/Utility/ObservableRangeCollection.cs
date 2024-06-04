using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace LocalizationTracker.Utility
{
	public class ObservableRangeCollection<T> : ObservableCollection<T>
	{
		private const string CountString = "Count";
		private const string IndexerName = "Item[]";

		protected enum ProcessRangeAction
		{
			Add,
			Replace,
			Remove
		};

		public ObservableRangeCollection() : base()
		{
		}

		public ObservableRangeCollection(IEnumerable<T> collection) : base(collection)
		{
		}

		public ObservableRangeCollection(List<T> list) : base(list)
		{
		}

		protected virtual void ProcessRange(Span<T> collection, ProcessRangeAction action)
		{
			if (collection == null) 
				throw new ArgumentNullException(nameof(collection));

            if (collection.Length == 0)
				return;

			CheckReentrancy();

			if (action == ProcessRangeAction.Replace) Items.Clear();
			foreach (var item in collection)
			{
				if (action == ProcessRangeAction.Remove) 
					Items.Remove(item);
				else 
					Items.Add(item);
			}

			OnPropertyChanged(new PropertyChangedEventArgs(CountString));
			OnPropertyChanged(new PropertyChangedEventArgs(IndexerName));
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public void AddRange(Span<T> collection)
		{
			ProcessRange(collection, ProcessRangeAction.Add);
		}

		public void ReplaceRange(Span<T> collection)
		{
			ProcessRange(collection, ProcessRangeAction.Replace);
		}

		public void RemoveRange(Span<T> collection)
		{
			ProcessRange(collection, ProcessRangeAction.Remove);
		}
	}
}