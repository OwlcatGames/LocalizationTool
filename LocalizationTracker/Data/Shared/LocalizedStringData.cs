using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using LocalizationTracker.Data.Wrappers;
using LocalizationTracker.Utility;
using static LocalizationTracker.Data.Unreal.UnrealStringData;

namespace Kingmaker.Localization.Shared
{
	public class LocalizedStringData: IStringData
	{
		[JsonPropertyName("source")]
		[JsonInclude]
		public Locale Source;

		[NotNull]
        [JsonInclude]
        [JsonPropertyName("key")]
		public string Key = "";

		[NotNull]
        [JsonInclude]
        [JsonPropertyName("comment")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		[JsonDefaultValue("")]
		public string Comment = "";

		[NotNull]
        [JsonInclude]
        [JsonPropertyName("speaker")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonDefaultValue("")]
        public string Speaker = "";

		[NotNull]
        [JsonInclude]
        [JsonPropertyName("speakerGender")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonDefaultValue("")]
        public string SpeakerGender = "";

        [NotNull]
        [JsonInclude]
        [JsonPropertyName("ownerGuid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonDefaultValue("")]
        public string OwnerGuid = "";

		[NotNull]
        [JsonInclude]
        [JsonPropertyName("languages")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<LocaleData> Languages = new ();

		[JsonInclude]
		[JsonPropertyName("string_traits")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public List<TraitData>? StringTraits;

		[JsonIgnore]
		public StringKind Kind {  get; set; }

		public LocalizedStringData(Locale source, string key)
		{
			Source = source;
			Key = key;
		}

		[JsonConstructor]
		public LocalizedStringData()
		{
		}

		[JsonIgnore]
		public bool ShouldCount => true;

		public bool UpdateText(Locale locale, string text, bool updateDate = true)
		{
			text = TextFixupHelper.ApplyFixups(locale, text);

			bool modified = false;
			var localeData = GetLocale(locale);
			if (localeData == null)
			{
				if (string.IsNullOrEmpty(text))
					return false;

				localeData = new LocaleData(locale);
				if (Languages.Count == 0)
					Source = locale;

				Languages.Add(localeData);
				modified = true;
			}

			if (localeData.Text != text)
			{
				localeData.Text = text;
				if (updateDate)
					localeData.ModificationDate = DateTimeOffset.UtcNow;

				modified = true;
			}

			return modified;
		}

		public bool ReapplyFixups()
		{
			bool updated = false;
			foreach (var l in Languages)
			{
				var s = TextFixupHelper.ApplyFixups(l.Locale, l.Text);
				if (s != l.Text)
				{
					l.Text = s;
					updated = true;
				}

				var originalLocale = l.TranslatedFrom ?? l.Locale;
				s = TextFixupHelper.ApplyFixups(originalLocale, l.OriginalText);
				if (s != l.OriginalText)
				{
					l.OriginalText = s;
					updated = true;
				}

				if (l.Traits != null)
				{
					foreach (var t in l.Traits)
					{
						s = TextFixupHelper.ApplyFixups(l.Locale, t.LocaleText);
						if (s != t.LocaleText)
						{
							t.LocaleText = s;
							updated = true;
						}
					}
				}
			}

			return updated;
		}

		public void UpdateTranslation(Locale locale, string text, Locale translatedFrom, string originalText)
		{
			UpdateText(locale, text);

			var localeData = GetLocale(locale);
			if (localeData == null)
				return;

			if (locale != translatedFrom)
			{
				localeData.TranslatedFrom = translatedFrom;
				localeData.OriginalText = originalText;
				localeData.TranslationDate = DateTimeOffset.UtcNow;
			}
		}

		public void AddTraitInternal(ITraitData trait)
		{
			StringTraits ??= new List<TraitData>();
			StringTraits.Add((TraitData)trait);
		}

		public void RemoveTraitInternal(string trait)
		{
			StringTraits?.RemoveAll(t => t.Trait == trait);
		}

		public ITraitData CreateTraitData(string trait) => new TraitData(trait);

		private LocaleData? GetLocale(Locale locale)
		{
			return (LocaleData?)((IStringData)this).GetLocale(locale);
		}

		public ILocaleData EnsureLocale(Locale locale)
		{
			var localeData = GetLocale(locale);
			if (localeData != null)
				return localeData;
			
			localeData = new LocaleData()
			{
				Locale = locale, ModificationDate = DateTimeOffset.UtcNow
			};
			Languages.Add(localeData);
			return localeData;
		}


		public bool UpdateComment(string comment)
		{
			if (Comment != comment)
			{
				Comment = comment;
				return true;
			}

			return false;
		}

		[JsonIgnore]
		public string StringPath { get; set; }
		[JsonIgnore]
		public DateTimeOffset ModificationDate { get; set; }

		[JsonIgnore]
		public string AttachmentPath => "";

		[JsonIgnore]
		public string AbsolutePath { get; set; }

		
		Locale IStringData.Source => Source;

		string IStringData.Key => Key;

		string IStringData.Comment
		{
			get => Comment;
			set => Comment = value;
		}

		string IStringData.Speaker => Speaker;

		string IStringData.SpeakerGender => SpeakerGender;

		string IStringData.OwnerLink => OwnerGuid;

		IEnumerable<ILocaleData> IStringData.Languages => Languages;

		IEnumerable<ITraitData> IStringData.StringTraits => StringTraits ??= new();
	}

	public class LocaleData: ILocaleData
	{
        [JsonInclude]
        [JsonPropertyName("locale")]
		public Locale Locale;

		[NotNull]
		[JsonInclude]
		[JsonPropertyName("text")]
		public string Text = "";

        [JsonInclude]
		[JsonPropertyName("modification_date")]
		public DateTimeOffset ModificationDate;


        [CanBeNull]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("translation_comment")]
        public string m_Comment;


        [CanBeNull]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		[JsonPropertyName("translated_from")]
		public Locale? TranslatedFrom;

		[CanBeNull]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("translation_date")]
		public DateTimeOffset? TranslationDate;

		[NotNull]
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("original_text")]
        [JsonDefaultValue("")]
        public string OriginalText = "";

		[JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("traits")]
		public List<TraitData>? Traits;

		public LocaleData(Locale locale)
		{
			Locale = locale;
            ModificationDate = DateTimeOffset.Now;
		}

		[JsonConstructor]
		public LocaleData()
		{
		}

        Locale ILocaleData.Locale => Locale;

		string ILocaleData.Text => Text;

		DateTimeOffset ILocaleData.ModificationDate => ModificationDate;

		Locale? ILocaleData.TranslatedFrom => TranslatedFrom;

		DateTimeOffset? ILocaleData.TranslationDate => TranslationDate;

		string ILocaleData.OriginalText
		{
			get => OriginalText;
			set => OriginalText = value;
		}

        [JsonIgnore]
        public string TranslatedComment
		{
			get => m_Comment;
			set => m_Comment = value;
		}

		IEnumerable<ITraitData> ILocaleData.Traits => Traits ??= new List<TraitData>();
		public void AddTraitInternal(ITraitData trait)
		{
			Traits ??= new List<TraitData>();
			Traits.Add((TraitData)trait);
		}

		public void RemoveTraitInternal(string trait)
		{
			Traits?.RemoveAll(t => t.Trait == trait);
		}
	}
}