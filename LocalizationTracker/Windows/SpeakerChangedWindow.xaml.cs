using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2013.Excel;
using LocalizationTracker.Components;
using LocalizationTracker.Data;
using LocalizationTracker.Logic;
using LocalizationTracker.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LocalizationTracker.Windows
{
    public partial class SpeakerChangedWindow : Window
    {
        public bool IsClosed { get; set; }
        public List<string> TraitEntries { get; set; } = new();

        public event EventHandler WindowClosed;

        ObservableCollection<SpeakersChangedVM> speakersChanges = new();

        public SpeakerChangedWindow(StringEntry[] allStrings)
        {
            InitializeComponent();

            OneTraitSelector.SelectionChanged -= ComboBox_SelectionChanged;
            LangSelector.SelectionChanged -= ComboBox_LangSelectionChanged;

            InitializeAsync(allStrings);

            OneTraitSelector.SelectionChanged += ComboBox_SelectionChanged;
            LangSelector.SelectionChanged += ComboBox_LangSelectionChanged;
        }

        private async Task InitializeAsync(StringEntry[] allStrings)
        {
            //repoType = await ChooseRepo();
            var fileName = "traits-string.txt";

            if (File.Exists(fileName))
            {
                using (var sr = new StreamReader(fileName))
                {
                    while (!sr.EndOfStream)
                    {
                        var t = await sr.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(t))
                            TraitEntries.Add(t);
                    }
                }
            }

            OneTraitSelector.ItemsSource = TraitEntries;

            int index = TraitEntries.IndexOf("Translated");
            if (index != -1)
            {
                OneTraitSelector.SelectedIndex = index;
            }
            else
            {
                OneTraitSelector.SelectedIndex = 0;
            }

            var langs = Locale.Values.Select(v => v.ToString()).ToList();
            LangSelector.ItemsSource = langs;
            LangSelector.SelectedIndex = 0;

            await FindChangedSpeakers(allStrings);
        }

        void ShowImportedStringsButton(object sender, RoutedEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (speakersChanges.Count() != 0)
            {
                foreach (var entry in speakersChanges)
                {
                    stringBuilder.AppendLine(entry.Path);
                }
            }

            StringsFilter.Filter.NameMultiline = stringBuilder.ToString();

            StringEntry.TargetLocale = new Locale(LangSelector.Items[LangSelector.SelectedIndex] as string);

            StringsFilter.Filter.ForceUpdateFilter();
        }

        private async Task FindChangedSpeakers(StringEntry[] allStrings)
        {
            CheckingLabel.Visibility = Visibility.Visible;

            speakersChanges.Clear();
            LogGrid.ItemsSource = null;

            var updateFolderPath = CheckConfigFile();
            var config = JsonSerializer.Deserialize<LastUpdate>(File.ReadAllText(updateFolderPath));

            var startTrait = config.Trait;
            var locale = LangSelector.Items[LangSelector.SelectedIndex] as string;

            var localeTraits = allStrings.Where(w => w.Data.GetLocale(locale) != null &&
                                w.Data.GetLocale(locale).HasTrait(startTrait)).ToArray();

            var tasks = localeTraits
                .Select(async line => await CheckSpeakerUpdate(line, startTrait, locale));

            await Task.WhenAll(tasks);

            if (speakersChanges.Count() == 0)
            {
                MessageBox.Show("Checking speakers completed. Changes not found", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LogGrid.ItemsSource = speakersChanges;
            CheckingLabel.Visibility = Visibility.Hidden;
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            IsClosed = true;
            WindowClosed?.Invoke(this, EventArgs.Empty);
        }

        private string CheckConfigFile()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string updateFolderPath = System.IO.Path.Combine(appDataPath, "LocalizationTracker");

            if (!Directory.Exists(updateFolderPath))
            {
                Directory.CreateDirectory(updateFolderPath);
            }

            string updateFilePath = System.IO.Path.Combine(updateFolderPath, $"LastUpdate.json");

            if (!File.Exists(updateFilePath))
            {
                var firstObj = new LastUpdate()
                {
                    Trait = "Translated"
                };

                var firstObjSer = JsonSerializer.Serialize(firstObj);

                File.WriteAllText(updateFilePath, firstObjSer);
            }

            return updateFilePath;

        }
        private async Task CheckSpeakerUpdate(StringEntry stringEntry, string startTrait, Locale locale)
        {
            var findLocale = stringEntry.Data.GetLocale(locale).Traits.Where(w => w.Trait == startTrait).OrderByDescending(w => w.Trait).First();

            if (stringEntry.Speaker != findLocale.Speaker && stringEntry.Data.SpeakerGender == findLocale.SpeakerGender)
            {

                speakersChanges.Add(new SpeakersChangedVM
                {
                    Path = stringEntry.PathRelativeToStringsFolder,
                    Key = stringEntry.Key,
                    OldSpeaker = findLocale.Speaker,
                    ActualSpeaker = stringEntry.Speaker,
                    Status = "Speaker was changed"
                });
            }
            else if (stringEntry.Data.SpeakerGender != findLocale.SpeakerGender)
            {
                speakersChanges.Add(new SpeakersChangedVM
                {
                    Path = stringEntry.PathRelativeToStringsFolder,
                    Key = stringEntry.Key,
                    OldSpeaker = findLocale.Speaker,
                    ActualSpeaker = stringEntry.Speaker,
                    Status = "Speaker gender was changed"
                });
            }
        }

        void ComboBox_LangSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var lang = LangSelector.SelectedItem as string;
            if (!string.IsNullOrEmpty(lang))
            {
                FindChangedSpeakers(StringManager.AllStrings);
            }
        }

        void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var trait = OneTraitSelector.Items[OneTraitSelector.SelectedIndex] as string;
            if (!string.IsNullOrEmpty(trait))
                ChangeTrait(trait);
        }

        void ChangeTrait(string trait)
        {
            speakersChanges.Clear();

            var newTrait = new LastUpdate()
            {
                Trait = trait
            };

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string updateFilePath = System.IO.Path.Combine(appDataPath, "LocalizationTracker", "LastUpdate.json");

            var newTraitSer = JsonSerializer.Serialize(newTrait);
            File.WriteAllText(updateFilePath, newTraitSer);

            FindChangedSpeakers(StringManager.AllStrings);
        }

    }
}




