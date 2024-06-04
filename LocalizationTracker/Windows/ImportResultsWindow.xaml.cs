using LocalizationTracker.Data;
using LocalizationTracker.Logic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace LocalizationTracker.Windows
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ImportResultsWindow : Window
    {
        #region Init

        public ImportResultsWindow(ImportRequestResult result)
        {
            InitializeComponent();
            _result = result;

            if (_result != null && _result.ImportResults.Count > 0)
            {
                InitDropdown();
                InitDataGrid();
                InitButtons();
            }
        }

        void InitDropdown()
        {
            var langs = _result.ImportResults.Keys.ToArray();
            LangGroupSelector.ItemsSource = langs;
            LangGroupSelector.SelectedIndex = 0;
        }

        void InitDataGrid()
        {
            LogGrid.AutoGenerateColumns = false;
            var firstGroup = _result.ImportResults.FirstOrDefault();
            ChangeLangGroup(firstGroup.Key);
        }

        void InitButtons()
        {
            SaveButton.Click += SaveButton_Click;
            if (_result.ImportResults.Count > 1)
            {
                SaveAllButton.IsEnabled = true;
                SaveAllButton.Click += SaveAllButton_Click;
            }
            else
            {
                SaveAllButton.IsEnabled = false;
            }
        }


        #endregion Init
        #region XamlMethods

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();

            SaveAllButton.Click -= SaveAllButton_Click;
            SaveButton.Click -= SaveButton_Click;
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var lang = LangGroupSelector.Items[LangGroupSelector.SelectedIndex] as string;
            if (!string.IsNullOrEmpty(lang))
                ChangeLangGroup(lang);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var lang = LangGroupSelector.Items[LangGroupSelector.SelectedIndex] as string;
            if (_result.ImportResults.TryGetValue(lang, out var result))
                LocalizationDiffExporter.SaveResultAsFile(result);
        }

        private void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            var results = _result.ImportResults.Values.ToList();
            LocalizationDiffExporter.SaveResultsAsFile(results);
        }

        #endregion XamlMethods
        #region ImportResultsWindow

        ImportRequestResult _result;

        void ChangeLangGroup(string langGroup)
        {
            if (_result.ImportResults.TryGetValue(langGroup, out var result))
            {
                LogGrid.ItemsSource = result.ImportEntries;
            }
        }

        #endregion ImportResultsWindow
    }
}
