using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using JetBrains.Annotations;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Components;
using LocalizationTracker.Data.Wrappers;

namespace LocalizationTracker.ViewModel
{
	public class LocaleDetailsVM
	{
		public Locale Locale { get; internal set; }

		public DateTimeOffset ModificationDate { get; internal set; }

		[NotNull]
		public InlinesWrapper Text { get; internal set; }

		public Visibility TranslationVisibility { get; internal set; }

		public Locale TranslatedFrom { get; internal set; }

		public DateTimeOffset TranslationDate { get; internal set; }

		[NotNull]
		public InlinesWrapper OriginalText { get; internal set; }

		public String TranslatorComment { get; internal set; }

		[NotNull]
		public List<TraitDetailsVM> Traits { get; internal set; }

		public Visibility TraitsVisibility
			=> Traits.Count <= 0
				? Visibility.Collapsed
				: Visibility.Visible;

		// designer data
		internal LocaleDetailsVM()
		{
			Locale = Locale.DefaultTo;
			ModificationDate = DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
			Text = new InlinesWrapper("Locale text");
			TranslationVisibility = Visibility.Visible;
			TranslatedFrom = Locale.DefaultFrom;
			TranslationDate = DateTimeOffset.UtcNow - TimeSpan.FromDays(5);
			OriginalText = new InlinesWrapper("Текст в русской локали");
			TranslatorComment = "";

			var finalTrait = new TraitDetailsVM("Final", "Final Text");
			var relevatTrait = new TraitDetailsVM("Relevant", "Relevant Text");
			Traits = new List<TraitDetailsVM> {finalTrait, relevatTrait};
		}

		public LocaleDetailsVM(ILocaleData localeData)
		{
			Locale = localeData.Locale;
			ModificationDate = localeData.ModificationDate;
			Text = new InlinesWrapper(localeData.Text);
			TranslatorComment = localeData.TranslatedComment;

			bool translated = localeData.TranslatedFrom != null && localeData.TranslatedFrom != Locale.Empty;
			if (translated)
			{
				TranslationVisibility = Visibility.Visible;
				TranslatedFrom = localeData.TranslatedFrom;
				TranslationDate = localeData.TranslationDate ?? DateTimeOffset.MinValue;
				OriginalText = new InlinesWrapper(localeData.OriginalText);
			}
			else
			{
				TranslationVisibility = Visibility.Collapsed;
				TranslatedFrom = Locale;
				TranslationDate = DateTimeOffset.MinValue;
				OriginalText = new InlinesWrapper();
			}

			Traits = localeData.Traits?
				.OrderBy(or => or.ModificationDate)
				.Select(td => new TraitDetailsVM(td))
				.ToList() ?? new List<TraitDetailsVM>();
		}
	}
}