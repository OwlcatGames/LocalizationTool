using System;
using System.Windows;
using LocalizationTracker.Components;
using StringsCollector.Data.Wrappers;

namespace LocalizationTracker.ViewModel
{
	public class TraitDetailsVM
	{
		public string Trait { get; }

		public DateTimeOffset ModificationDate { get; }

		public InlinesWrapper Text { get; }

		public Visibility TextVisibility
			=> Text.HasAny
				? Visibility.Visible
				: Visibility.Collapsed;

		// designer data
		public TraitDetailsVM()
		{
			Trait = "Final";
			ModificationDate = DateTimeOffset.UtcNow - TimeSpan.FromDays(2);
			Text = new InlinesWrapper("Final Text");
		}

		// designer data
		internal TraitDetailsVM(string trait, string text = "")
		{
			Trait = trait;
			ModificationDate = DateTimeOffset.UtcNow - TimeSpan.FromDays(2);
			Text = new InlinesWrapper(text);
		}

		public TraitDetailsVM(ITraitData traitData)
		{
			Trait = traitData.Trait;
			ModificationDate = traitData.ModificationDate.ToLocalTime();
			Text = new InlinesWrapper(traitData.LocaleText);
		}
	}
}