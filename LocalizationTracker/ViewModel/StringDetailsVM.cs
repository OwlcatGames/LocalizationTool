using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Data;
using LocalizationTracker.Data.Unreal;

namespace LocalizationTracker.ViewModel
{
	public class StringDetailsVM
	{
		public static readonly StringDetailsVM Empty 
			= new StringDetailsVM {CommonVisibility = Visibility.Collapsed};

		public Visibility CommonVisibility { get; private set; }

		public Locale Source { get; }

		[NotNull]
		public string Path { get; set; }

		[NotNull]
		public string Key { get; }

		[NotNull]
		public string Comment { get; }

		[NotNull]
		public string Speaker { get; }

		[NotNull]
		public LocaleDetailsVM[] Locales { get; }

		[NotNull]
		public TraitDetailsVM[] StringTraits { get; }

		public Visibility CommentVisibility 
			=> string.IsNullOrEmpty(Comment)
			? Visibility.Collapsed
			: Visibility.Visible;

		public Visibility SpeakerVisibility 
			=> string.IsNullOrEmpty(Speaker)
				? Visibility.Collapsed
				: Visibility.Visible;

		public Visibility AttachmentVisibility 
			=> string.IsNullOrEmpty(AttachedImagePath)
				? Visibility.Collapsed
				: Visibility.Visible;

		public Visibility TraitsVisibility
			=> StringTraits.Length <= 0
				? Visibility.Collapsed
				: Visibility.Visible;
		
		public string AttachedImagePath { get; }

		public StringDetailsVM()
		{
			CommonVisibility = Visibility.Visible;
			Path = "path/to/text.json";
			Key = "aaaaa-bbbbb-cccc";
			Comment = "Comment";
			Speaker = "Speaker";

			var ruLocale = new LocaleDetailsVM();
			var enLocale = new LocaleDetailsVM();

			ruLocale.Locale = enLocale.TranslatedFrom;
			ruLocale.Text = enLocale.OriginalText;
			ruLocale.TranslationVisibility = Visibility.Collapsed;

			Locales = new[] {ruLocale, enLocale};

			var importantTrait = new TraitDetailsVM("Important");
			StringTraits = new [] {importantTrait};
		}

		public StringDetailsVM(StringEntry se)
		{
			CommonVisibility = Visibility.Visible;
			var stringData = se.Data;
			Path = se.PathRelativeToStringsFolder;
			Source = stringData.Source;
			Key = stringData.Key;
			Comment = stringData.Comment;
			Speaker = stringData.Speaker;
			AttachedImagePath = string.IsNullOrEmpty(stringData.AttachmentPath)
				? ""
				: System.IO.Path.GetFullPath(
					System.IO.Path.Combine(AppConfig.Instance.AttachmentsPath, stringData.AttachmentPath));

			Locales = stringData.Languages
				.Select(ld => new LocaleDetailsVM(ld))
				.ToArray();

			StringTraits = stringData.StringTraits?
				.Select(td => new TraitDetailsVM(td))
				.ToArray() ?? new TraitDetailsVM[0];
		}
	}
}