using Kingmaker.Localization.Shared;
using LocalizationTracker.Data;
using LocalizationTracker.Logic;
using LocalizationTracker.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LocalizationTracker.Windows
{
	/// <summary>
	/// Логика взаимодействия для DublicateResolverWindow.xaml
	/// </summary>
	public partial class DublicateResolverWindow : Window
	{
		public DublicateResolverWindow(Window owner, Dictionary<string, List<StringEntry>> dublicates)
		{
			InitializeComponent();
			Owner = owner;

			Variants.SelectionMode = SelectionMode.Single;
			_dublicates = dublicates;
			SubscribeButton();
			Turn();
		}

		Dictionary<string, List<StringEntry>> _dublicates;

		StringBuilder _builder = new StringBuilder();

		ResultInfo _result;

		List<DublicateInfo> _dublicateInfos = new List<DublicateInfo>();

		List<SelectionPoint> _selectionPoints = new List<SelectionPoint>();

		int _pointIndex = 0;

		string _key;

		void SubscribeButton()
		{
			NextButton.Click += OnNextLoc;
			PreviewButton.Click += OnPreviewLoc;
			SelectButton.Click += OnSelect;
			ResolveButton.Click += OnResolve;
		}

		private void OnNextLoc(object sender, RoutedEventArgs e)
		{
			UpdatePointValue();
			_pointIndex++;
			SetPoint();
		}

		private void OnPreviewLoc(object sender, RoutedEventArgs e)
		{
			UpdatePointValue();
			_pointIndex--;
			SetPoint();
		}

		private void OnSelect(object sender, RoutedEventArgs e)
		{
			if (Variants.SelectedIndex != -1)
			{
				var selected = GetSelected();
				var origin = _dublicateInfos.Find(x => x.Entry.AbsolutePath.Replace(AppConfig.Instance.AbsStringsFolder, string.Empty) == selected);
				SingleUsings(origin);
			}
		}

		private void OnResolve(object sender, RoutedEventArgs e)
		{
			DublicatesResolver.Resolve(_result, _dublicateInfos);
			_result = default;
			_dublicateInfos = default;

			_dublicates.Remove(_key);
			if (_dublicates.Count > 0)
			{
				Turn();
			}
			else
			{
				Close();
			}
		}

		void SetPoint()
		{
			var point = _selectionPoints[_pointIndex];
			SetVariantContent(point.Strings);
			Variants.SelectedIndex = point.SelectedIndex;
			Instuction.Text = $"Выберете локализацию для языка: {point.Locale}";

			NextButton.IsEnabled = _selectionPoints.Count > _pointIndex + 1;
			PreviewButton.IsEnabled = _pointIndex - 1 >= 0;
		}

		void UpdatePointValue()
		{
			if (Variants.SelectedIndex != -1)
			{
				var point = _selectionPoints[_pointIndex];
				var selected = point.Strings[Variants.SelectedIndex];
				point.SelectedIndex = Variants.SelectedIndex;
				_result.SelectedLocales[point.Locale] = selected;
			}
		}

		void Turn()
		{
			CommonInfoField.Text = $"Осталось конфликтов: {_dublicates.Count}";

			var item = _dublicates.First();
			_key = item.Key;
			_builder.AppendLine($"Дубликаты ключа: {item.Key}");
			_dublicateInfos = DublicatesResolver.GetCurrentUsing(item.Value);

			var usingCount = _dublicateInfos.Count(x => x.IsUsing);
			switch (usingCount)
			{
				case 0:
					ZeroUsings();
					break;

				case 1:
					var originIndex = _dublicateInfos.IndexAt(x => x.IsUsing);
					var origin = _dublicateInfos[originIndex];
					SingleUsings(origin);
					break;

				default:
					_builder.AppendLine($"(Sic!) {usingCount} StringEntry используется в ассетах проекта. Нонсенс. Обратитесь к программистам");
					break;
			}

			CurrentCaseField.Text = _builder.ToString();
			_builder.Clear();
		}

		void ZeroUsings()
		{
			_builder.AppendLine("Ни одна из строк с данным ключом сейчас не используется. Выберете основную или удалите все");
			_builder.AppendLine($"После нажатия Select Origin - произойдёт переключение в режим разрешения конфликта");
			_builder.AppendLine($"После нажатия Resolve - дубликаты будут удалены");
			Instuction.Text = "Выберете файл, который нужно оставить, а затем нажмите Select, либо нажмите Resolve чтобы удалить все строки.";

			var list = new List<string>(_dublicateInfos.Count);
			foreach (var info in _dublicateInfos)
			{
				var path = info.Entry.AbsolutePath.Replace(AppConfig.Instance.AbsStringsFolder, string.Empty);
				list.Add(path);
			}

			Variants.ItemsSource = list;
			SwitchZeroButtons();
		}

		void SingleUsings(DublicateInfo origin)
		{
			PrepearePoints(origin);
			if (_selectionPoints.Count > 0)
			{
				_pointIndex = 0;
				SetPoint();

				_builder.AppendLine($"Путь до используемой строки: {origin.Entry.AbsolutePath.Replace(AppConfig.Instance.AbsStringsFolder, string.Empty)}");
				_builder.AppendLine($"С помощью кнопок Preview и Next вы можете переключать локализации, чтобы выбрать тот перевод, что останется в используемой строке");
				_builder.AppendLine($"После нажатия Resolve - дубликаты будут удалены, а используемая строка обновлена");
				SwitchSingleButtons();
			}
			else
			{
				OnResolve(null, null);
			}
		}

		void PrepearePoints(DublicateInfo origin)
		{
			_result = new ResultInfo() { Entry = origin.Entry };
			foreach (var locale in Locale.DefaultValues)
			{
				var point = new SelectionPoint(locale);
				foreach (var info in _dublicateInfos)
				{
					var text = new LocaleEntry(info.Entry, locale).Text;
					if (!string.IsNullOrEmpty(text))
					{
						point.Strings.Add(text);
						if (info == origin)
						{
							point.SelectedIndex = point.Strings.Count - 1;
						}
					}
				}

				if (point.Strings.Count > 0)
				{
					if (point.Strings.Count == 1)
					{
						if (point.SelectedIndex != -1)
						{
							_result.SelectedLocales.Add(locale, point.Strings[0]);
						}
						else
						{
							point.Strings.Insert(0, string.Empty);
							point.SelectedIndex = 0;
							_selectionPoints.Add(point);
						}
					}
					else
					{
						if (point.SelectedIndex == -1)
						{
							point.Strings.Insert(0, string.Empty);
							point.SelectedIndex = 0;
						}

						_selectionPoints.Add(point);
					}
				}
			}
		}

		void SetVariantContent(List<string> strings)
		{
			Variants.Items.Clear();
			for (int i = 0; i < strings.Count; i++)
			{
				var border = new Border() { BorderBrush = Brushes.Black, };
				var item = new TextBlock() { Text = strings[i], TextWrapping = TextWrapping.Wrap };

				border.Child = item;
				Variants.Items.Add(border);
			}
		}

		string GetSelected()
		{
			var selected = Variants.SelectedItem;
			if (selected != null)
			{
				var item = selected as Border;
				var text = item.Child as TextBox;
				return text.Text;
			}

			return string.Empty;
		}

		void SwitchZeroButtons()
		{
			NextButton.Visibility = Visibility.Collapsed;
			PreviewButton.Visibility = Visibility.Collapsed;
			SelectButton.Visibility = Visibility.Visible;
			ResolveButton.Visibility = Visibility.Visible;
		}

		void SwitchSingleButtons()
		{
			NextButton.Visibility = Visibility.Visible;
			PreviewButton.Visibility = Visibility.Visible;
			SelectButton.Visibility = Visibility.Collapsed;
			ResolveButton.Visibility = Visibility.Visible;
		}

		class SelectionPoint
		{
			public SelectionPoint(Locale locale)
				=> Locale = locale;

			public Locale Locale;
			public List<string> Strings = new List<string>();
			public int SelectedIndex = -1;
		}
	}
}
