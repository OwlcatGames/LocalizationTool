using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;
using LocalizationTracker.Windows;

namespace LocalizationTracker.Components
{
    public partial class TraitsSelector : UserControl
    {
        public ObservableCollection<TraitSelectorEntry> TraitEntries { get; } = new ObservableCollection<TraitSelectorEntry>();


        public ObservableCollection<string> TraitsSource
        {
            get { return (ObservableCollection<string>)GetValue(TraitsSourceProperty); }
            set { SetValue(TraitsSourceProperty, value); }
        }

        public static readonly DependencyProperty TraitsSourceProperty =
            DependencyProperty.Register("TraitsSource", typeof(ObservableCollection<string>), typeof(TraitsSelector), new PropertyMetadata(TraitsSourcePropertyChanged));

        public bool IsLocale
        {
            get { return (bool)GetValue(IsLocaleProperty); }
            set { SetValue(IsLocaleProperty, value); }
        }

        public static readonly DependencyProperty IsLocaleProperty =
            DependencyProperty.Register("IsLocale", typeof(bool), typeof(TraitsSelector), new PropertyMetadata(true, LocalePropertyChanged));

        public string SelectedTraitsText
        {
            get { return (string)GetValue(SelectedTraitsTextProperty); }
            set { SetValue(SelectedTraitsTextProperty, value); }
        }

        public static readonly DependencyProperty SelectedTraitsTextProperty =
            DependencyProperty.Register(nameof(SelectedTraitsText), typeof(string), typeof(TraitsSelector));

        public TraitsSelector()
        {
            InitializeComponent();
            CheckedListBox.ItemsSource = TraitEntries;
            UpdateTraitsList();
        }

        private void ButtonShowPopup_Click(object sender, RoutedEventArgs e)
        {
            PopupTraits.IsOpen = !PopupTraits.IsOpen;
            UpdateTraitsList();
        }

        private static void LocalePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var selector = sender as TraitsSelector;
            selector?.UpdateTraitsList();
        }

        private static void TraitsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var selector = sender as TraitsSelector;
            selector?.UpdateTraitsList();
        }

        private void UpdateTraitsList()
        {
            TraitEntries.Clear();
            var enumValues = IsLocale
                ? Enum.GetNames(typeof(LocaleTrait))
                : Enum.GetNames(typeof(StringTrait));
            foreach (var trait in enumValues)
            {
                TraitEntries.Add(new TraitSelectorEntry(trait, false));
            }

            var fileName = IsLocale
                ? "traits-locale.txt"
                : "traits-string.txt";

            if (File.Exists(fileName))
            {
                using (var sr = new StreamReader(fileName))
                {
                    while (!sr.EndOfStream)
                    {
                        var t = sr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(t))
                            TraitEntries.Add(new TraitSelectorEntry(t, false));
                    }
                }
            }

            CheckedListBox.SelectedItem = TraitEntries.FirstOrDefault();

            if (TraitsSource != null)
            {
                foreach (var e in TraitEntries)
                {
                    e.Selected = TraitsSource.Contains(e.Trait);
                }
                ElementSelected(null, null);
            }

        }

        public IEnumerable<string> SelectedTraits =>
            TraitEntries
                .Where(t => t.Selected)
                .Select(t => t.Trait);

        private void ElementSelected(object sender, RoutedEventArgs e)
        {
            SelectedTraitsText = string.Join(", ", SelectedTraits);

            if (TraitsSource != null)
            {
                TraitsSource.Clear();
                foreach (var t in SelectedTraits)
                {
                    TraitsSource.Add(t);
                }
            }

            ButtonText.Text = $"{SelectedTraitsText}";

        }

        private void ListBoxItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                item.IsSelected = !item.IsSelected;
            }
        }

    }
}
