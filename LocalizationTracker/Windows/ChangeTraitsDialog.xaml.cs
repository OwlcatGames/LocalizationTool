using System.Linq;
using System.Windows;
using System.Windows.Input;
using Kingmaker.Localization.Shared;

namespace LocalizationTracker.Windows
{
	public partial class ChangeTraitsDialog
	{
		public bool Remove { get; set; }

		public Locale? Locale;

		public string[] Traits;

		public ChangeTraitsDialog()
		{
			InitializeComponent();
			ComboLocale.Items.Add("");
			foreach (var locale in Locale.Values)
			{
				ComboLocale.Items.Add(locale);
			}
			TraitsSelector.IsLocale = false;
		}

		private void ButtonCancel_Click(object sender, RoutedEventArgs e)
		{
			Cancel();
		}

		private void ButtonOk_Click(object sender, RoutedEventArgs e)
		{
			if (!TraitsSelector.SelectedTraits.Any())
			{
				Cancel();
				return;
			}

			DialogResult = true;
			Locale = !string.IsNullOrEmpty(ComboLocale.SelectedItem?.ToString())
				? ComboLocale.SelectedItem as Locale
				: null;
			Traits = TraitsSelector.SelectedTraits.ToArray();
			Close();
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			Cancel();
		}

		private void Cancel()
		{
			DialogResult = false;
			Close();
		}

		private void ComboLocale_OnSelected(object sender, RoutedEventArgs e)
		{
			TraitsSelector.IsLocale = !string.IsNullOrEmpty(ComboLocale.SelectedItem?.ToString());
		}
	}
}