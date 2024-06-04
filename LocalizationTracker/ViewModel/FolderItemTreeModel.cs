using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace LocalizationTracker.ViewModel
{
    /// <summary>
    /// Sample base class for tree items view models. All specialised tree item view model classes
    /// should inherit from this class.
    /// </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
	public class FolderItemTreeModel : INotifyPropertyChanged
	{
		#region Data

		private readonly ObservableCollection<FolderItemTreeModel> children = new ObservableCollection<FolderItemTreeModel>();

		private bool isExpanded;
		private bool isSelected;
		private bool isEditable;
		private bool isEditing;
		private bool isEnabled = true;

		#endregion Data

		#region Constructor

		public FolderItemTreeModel(FolderItemTreeModel? parent, string folder, bool isRootFolder)
		{
			this.Parent = parent;
			this.Folder = folder;
			this.IsRootFolderItem = isRootFolder;
		}

		#endregion Constructor

		#region Public properties

		/// <summary>
		/// Returns the logical child items of this object.
		/// </summary>
		public ObservableCollection<FolderItemTreeModel> Children => children;

		/// <summary>
		/// Gets/sets whether the TreeViewItem 
		/// associated with this object is expanded.
		/// </summary>
		public bool IsExpanded
		{
			get { return isExpanded; }
			set
			{
				if (value != isExpanded)
				{
					isExpanded = value;
					OnPropertyChanged("IsExpanded");

					// Expand all the way up to the root.
					if (isExpanded && Parent != null)
                        Parent.IsExpanded = true;

					if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
					{
						foreach (var child in Children)
						{
							child.IsExpanded = value;
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets/sets whether the TreeViewItem 
		/// associated with this object is selected.
		/// </summary>
		public bool IsSelected
		{
			get { return isSelected; }
			set
			{
				if (value != isSelected)
				{
					isSelected = value;
					OnPropertyChanged("IsSelected");
				}
			}
		}

		public bool IsEditable
		{
			get { return isEditable; }
			set
			{
				if (value != isEditable)
				{
					isEditable = value;
					OnPropertyChanged("IsEditable");
				}
			}
		}

		public bool IsEditing
		{
			get { return isEditing; }
			set
			{
				if (value != isEditing)
				{
					isEditing = value;
					OnPropertyChanged("IsEditing");
				}
			}
		}

		public bool IsEnabled
		{
			get { return isEnabled; }
			set
			{
				if (value != isEnabled)
				{
					isEnabled = value;
					OnPropertyChanged("IsEnabled");
				}
			}
		}

		public bool IsVisible => stringCount != 0;

		public readonly string Folder;

		public readonly FolderItemTreeModel? Parent;

		public override string ToString() => $"[Node {DisplayName}]";

		public readonly bool IsRootFolderItem;
		#endregion Public properties

		#region ViewModelBase

		/// <summary>
		/// Returns the user-friendly name of this object.
		/// Child classes can set this property to a new value,
		/// or override it to determine the value on-demand.
		/// </summary>
		public virtual string DisplayName
		{
			get => ShowCounts
				? $"{Name} ({StringCount} strings, {WordCount} words)"
				: Name;
		}

		public virtual string Name
		{
			get => IsRootFolderItem 
				? "..." 
				: Path.GetFileName(Folder.TrimEnd('/'));
		}

		private bool showCounts = true;
		public bool ShowCounts
		{
			get => showCounts;
			set
			{
				if (showCounts != value)
				{
					showCounts = value;
					OnPropertyChanged(nameof(DisplayName));
				}
			}
		}

		private int stringCount;
		public int StringCount
		{
			get => stringCount; 
			set
			{
				if (stringCount != value)
				{
					stringCount = value;
                    OnPropertyChanged(nameof(IsVisible));
                    if (ShowCounts)
						OnPropertyChanged(nameof(DisplayName));
                }
            }
		}

		private int wordCount;
		public int WordCount
		{
			get => wordCount; 
			set
			{
				if (wordCount != value)
				{
					wordCount = value;
                    if (ShowCounts)
                        OnPropertyChanged(nameof(DisplayName));
                }
            }
		}

		public int Depth => Parent?.Depth + 1 ?? 0;

		#endregion ViewModelBase

		#region INotifyPropertyChanged members

		/// <summary>
		/// Raised when a property on this object has a new value.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Raises this object's PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">The property that has a new value.</param>
		protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion INotifyPropertyChanged members
	}
}
