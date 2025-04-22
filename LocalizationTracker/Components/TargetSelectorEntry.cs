using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using StringsCollector.Data;

namespace LocalizationTracker.Components
{
	public class TargetSelectorEntry
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

		public Locale Target { get; }

		public TargetSelectorEntry(Locale target, bool selected)
		{
			Target = target;
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