using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace LocalizationTracker.Components
{
	public class TraitSelectorEntry
	{
		private bool m_Selected;

		public bool Selected
		{
			get { return m_Selected; }
			set
			{
				{
					m_Selected = value;
					OnPropertyChanged(nameof(Selected));
				}
			}
		}

		public string Trait { get; }

		public TraitSelectorEntry(string trait, bool selected)
		{
			Trait = trait;
			Selected = selected;
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}