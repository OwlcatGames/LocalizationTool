using Kingmaker.Localization.Shared;
using LocalizationTracker.Data;
using LocalizationTracker.Data.Wrappers;
using LocalizationTracker.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LocalizationTracker.Components
{
    public partial class TargetSelector : UserControl
    {
        public ObservableCollection<TargetSelectorEntry> TargetEntries { get; } = new ObservableCollection<TargetSelectorEntry>();


        public ObservableCollection<string> TargetSource
        {
            get { return (ObservableCollection<string>)GetValue(TargetSourceProperty); }
            set { SetValue(TargetSourceProperty, value); }
        }

        public static readonly DependencyProperty TargetSourceProperty =
            DependencyProperty.Register("TargetSource", typeof(ObservableCollection<string>), typeof(TargetSelector), new PropertyMetadata(TargetSourcePropertyChanged));

        public string SelectedTargetText
        {
            get { return (string)GetValue(SelectedTargetTextProperty); }
            set { SetValue(SelectedTargetTextProperty, value); }
        }

        public static readonly DependencyProperty SelectedTargetTextProperty =
            DependencyProperty.Register(nameof(SelectedTargetText), typeof(string), typeof(TargetSelector));

        public TargetSelector()
        {
            InitializeComponent();
            CheckedListBox.ItemsSource = TargetEntries;
            UpdateTargetsList();
        }

        private void ButtonShowPopup_Click(object sender, RoutedEventArgs e)
        {
            PopupTraits.IsOpen = !PopupTraits.IsOpen;
        }

        private static void TargetSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var selector = sender as TargetSelector;
            selector?.UpdateTargetsList();
        }

        private void UpdateTargetsList()
        {
            TargetEntries.Clear();

            foreach (var line in Locale.Values)
            {
                if (line == StringEntry.TargetLocale)
                {
                    TargetEntries.Add(new TargetSelectorEntry(line, true));
                    SelectedTargetText = string.Join(", ", SelectedTargets);
                    ButtonText.Text = $"{SelectedTargetText}";
                }
                else
                {
                    TargetEntries.Add(new TargetSelectorEntry(line, false));
                }
            }

            CheckedListBox.SelectedItem = TargetEntries.FirstOrDefault();

            if (TargetSource != null)
            {
                foreach (var e in TargetEntries)
                {
                    e.Selected = TargetSource.Contains(e.Target);
                }
                ElementSelected(null, null);
            }

        }

        public IEnumerable<Locale> SelectedTargets =>
            TargetEntries
                .Where(t => t.Selected)
                .Select(t => t.Target);

        private void ElementSelected(object sender, RoutedEventArgs e)
        {
            SelectedTargetText = string.Join(", ", SelectedTargets);

            if (TargetSource != null)
            {
                TargetSource.Clear();
                foreach (var t in SelectedTargets)
                {
                    TargetSource.Add(t);
                }
            }

            ButtonText.Text = $"{SelectedTargetText}";
        }
    }

}
