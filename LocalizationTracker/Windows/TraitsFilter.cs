using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;
using JetBrains.Annotations;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Data;

namespace LocalizationTracker.Windows
{
	public class TraitsFilter : DependencyObject
	{
		private Locale m_Locale = Locale.Empty;

		private bool m_Not;

		[JsonIgnore]
		public Locale Locale
		{
			get => m_Locale;
			set
			{
				if (value==Locale.Empty ^ m_Locale == Locale.Empty)
					Traits.Clear();

				m_Locale = value;
				LocaleTraitsSelection = m_Locale != Locale.Empty;
				//Updated?.Invoke();
			}
		}

		public bool Not
		{
			get { return m_Not; }
			set
			{
				m_Not = value; 
				//Updated?.Invoke();
			}
		}

		[NotNull]
		public ObservableCollection<string> Traits { get; } = new ObservableCollection<string>();

		public bool LocaleTraitsSelection
		{
			get => (bool)GetValue(LocaleTraitsSelectionProperty);
			set => SetValue(LocaleTraitsSelectionProperty, value);
		}

		public static readonly DependencyProperty LocaleTraitsSelectionProperty =
			DependencyProperty.Register("LocaleTraitsSelection", typeof(bool), typeof(TraitsFilter));

		public event FilterUpdateHandler? Updated;

		public TraitsFilter()
		{
			//Traits.CollectionChanged += (sender, args) => { Updated?.Invoke(); };
		}

		public bool CheckString(StringEntry se)
		{
			return StringFits(se) ^ Not;
		}

		private bool StringFits(StringEntry se)
		{
			if (m_Locale == Locale.Empty)
				return Traits.All(t => se.Data.HasStringTrait(t));

			var localeData = se.Data.GetLocale(m_Locale);
			if (localeData == null || string.IsNullOrEmpty(localeData.Text))
				return false;

			return Traits.All(t => localeData.HasTrait(t));
		}

		public class Json // need a wrapper because otherwise json tries to serialize DependencyObject fields. 
		{
			[JsonInclude]
			[JsonPropertyName("locale")]
			public Locale Locale;

			[JsonInclude]
			[JsonPropertyName("not")]
			public bool Not;

			[JsonInclude]
			[JsonPropertyName("traits")]
			public List<string> Traits;

			public Json()
			{
			}

			public Json(TraitsFilter f)
			{
				Locale = f.m_Locale;
				Not = f.m_Not;
				Traits = f.Traits.ToList();
			}
			
		}

		public TraitsFilter(Json json)
		{
			m_Locale = json.Locale;
			
			LocaleTraitsSelection = m_Locale != Locale.Empty;
			m_Not = json.Not;
			Traits.Clear();
			foreach (string trait in json.Traits)
			{
				Traits.Add(trait);
			}
		}

	}
}