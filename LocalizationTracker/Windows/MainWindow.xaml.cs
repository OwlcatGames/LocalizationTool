using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using JetBrains.Annotations;
using LocalizationTracker.Data;
using LocalizationTracker.Properties;
using LocalizationTracker.Tools;
using LocalizationTracker.Utility;
using LocalizationTracker.ViewModel;
using wpf4gp;
using Button = System.Windows.Controls.Button;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Control = System.Windows.Controls.Control;
using Key = System.Windows.Input.Key;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using LocalizationTracker.Logic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.Reflection;
using System.Text.Json;
using DeepL;
using DeepL.Model;
using System.Windows.Documents;
using LocalizationTracker.Components;
using LocalizationTracker.Tools.GlossaryTools;
using Brushes = System.Windows.Media.Brushes;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using LocalizationTracker.Tools.SVGTool;
using StringsCollector.Data.Unreal;
using StringsCollector.Data.Unity;
using System.Windows.Media;

namespace LocalizationTracker.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        RepoConnection repoConnection = new RepoConnection();
        public TaskCompletionSource<bool> scanCompletionSource = new TaskCompletionSource<bool>();


        public double FontValue = 13;
        public double FontSizeValue
        {
            get => FontValue;
            set
            {
                if (FontValue != value)
                {
                    FontValue = value;
                    OnPropertyChanged(nameof(FontSizeValue));
                    SaveFontSize();
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        public bool IsDefault { get; set; }

        [NotNull]
        public StringsFilter Filter => StringsFilter.Filter;

        [NotNull]
        public readonly DispatcherTimer FilterTimer = new() { Interval = TimeSpan.FromSeconds(2) };

        [NotNull]
        public ObservableRangeCollection<StringEntry> StringsSource { get; } = new(new List<StringEntry>());

        public ObservableCollection<FolderItemTreeModel> FoldersSource { get; } = new();
        public System.Windows.Media.Color? SelectedColor { get; set; }

        public ImportResultsWindow? ImportResults;

        public ExportResultsWindow? ExportResults;

        public StringDetailsWindow? StringDetails;

        public bool IsCopyDataButtonIsVisible()
            => AppConfig.Instance.EnableCopyData;

        private static HashSet<string> ExpandedFolders = new HashSet<string>();

        private FilterMode m_LastFilterMode;

        private DeepL.Translator m_Translator;

        private MultilineSearch m_MultilineSearchWindow;
        private SpeakerChangedWindow m_SpeakerChangedWindow;

        public Locale SourceLocale
        {
            get => StringEntry.SourceLocale;
            set
            {
                StringEntry.SourceLocale = value;
                NotifyPropertyChanged(nameof(SourceLocale));
                UpdateFilter(true);
            }
        }

        public Locale[] SourceLocaleValues => Locale.SourceLocaleValues;

        public Locale TargetLocale
        {
            get => StringEntry.TargetLocale;
            set
            {
                StringEntry.TargetLocale = value;
                NotifyPropertyChanged(nameof(TargetLocale));
                UpdateFilter(true);
            }
        }

        public Locale[] TargetLocaleValues => Locale.Values;

        public string[] SourceTraitValues => LocaleTraitExtensions.Values.Select(v => v.ToString()).ToArray();


        private bool m_WordCueCount = true;

        public bool WordCueCount
        {
            get => m_WordCueCount;
            set
            {
                m_WordCueCount = value;
                var sortedPaths = StringsFilter.FilteredStrings;
                if (value)
                    UpdateCounts(sortedPaths, SourceLocale);
                else
                    UpdateStringsCount(sortedPaths, null, 0, sortedPaths.Length, FoldersSource);
            }
        }

        public MainWindow()
        {
            try
            {
                File.Delete("error.log");
            }
            catch
            {
                // ignored
            }

            FindVersionInfo();

            InitializeComponent();
            Show();

            this.DataContext = this;

            string iconPath = AppConfig.Instance.IconPath;
            if (File.Exists(iconPath))
            {
                BitmapImage iconImage = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute));
                this.Icon = iconImage;
            }

            try
            {
                m_LastFilterMode = Filter.Mode;
                FilterTimer.Tick += (sender, args) =>
                                    {
                                        FilterTimer.Stop();
                                        UpdateFilter(Filter.Mode == FilterMode.Updated_Trait);
                                        UpdateFilter(StringsFilter.Filter.Text != null || StringsFilter.Filter.Text == "");
                                    };

                Filter.Updated +=
                    () =>
                    {
                        FilterTimer.Stop();
                        FilterTimer.Start();
                    };

                Filter.ModeUpdated +=
                    () =>
                    {
                        FilterTimer.Stop();
                        UpdateFilterMode();
                        var updateInlines = Filter.Mode != m_LastFilterMode;

                        UpdateFilter(updateInlines);
                        m_LastFilterMode = Filter.Mode;
                    };

                Filter.HideTagsUpdated +=
                    () =>
                    {
                        FilterTimer.Stop();
                        UpdateFilter(true);
                        FilterTimer.Start();
                    };
                Glossary.Instance.GlossaryUpdatedEvent +=
                    () =>
                  {
                      FilterTimer.Stop();
                      UpdateFilter(true);
                      FilterTimer.Start();
                  };
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox("Startup error.", "Unexpected error during startup");
            }

            LoadExpandedFolders();
            LoadFilterColor();
            LoadFontSize();

            DataContext = this;

            if (AppConfig.Instance.DeepLAvailable)
            {
                m_Translator = new Translator(AppConfig.Instance.DeepL.APIKey);
            }
            StringManager.InitializeArchive(AppConfig.Instance.Engine);

            Strings.AutoGenerateColumns = false;
            Folders.SelectionChanged += (_, _) => UpdateStringsView();

            Action onPropertyChanged = () =>
                                       {
                                           OnPropertyChanged(nameof(SourceLocale));
                                           OnPropertyChanged(nameof(TargetLocale));
                                       };

            StringEntry.SourceLocaleChanged += onPropertyChanged;
            StringEntry.TargetLocaleChanged += onPropertyChanged;

            UpdateFilterMode();
            Rescan();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void FindVersionInfo()
        {
            var pathToVersion = Path.Combine(
                Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
                "version.txt");
            var localVersion = File.Exists(pathToVersion)
                ? File.ReadAllText(pathToVersion).TrimEnd('\r', '\n', ' ')
                : "[no local version]";
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString();
            var pathToNetwork = @"\\samba.owlcat.local\Owlcat\infrastructure\LocalizationTracker\version.txt";
            var hasRemote = File.Exists(pathToNetwork);
            var remoteVersion = hasRemote ? File.ReadAllText(pathToNetwork).TrimEnd('\r', '\n', ' ') : "";
            var project = AppConfig.Instance.Project;

            Title = hasRemote
                ? assemblyVersion == remoteVersion
                    ? $"{project} Localization Tool  Version {assemblyVersion} (up to date)"
                    : $"{project} Localization Tool  Version {assemblyVersion} (OBSOLETE, latest is {remoteVersion})"
                : $"Localization Tool Version {assemblyVersion}, network drive not available";
        }

        private static void LoadExpandedFolders()
        {
            if (!string.IsNullOrWhiteSpace(Settings.Default.Expanded_Folders))
            {
                var stringArray = JsonSerializer.Deserialize<string[]>(Settings.Default.Expanded_Folders);
                ExpandedFolders = stringArray.ToHashSet();
            }
        }

        private static void SaveExpandedFolders()
        {
            var stringArray = ExpandedFolders.ToArray();
            var str = JsonSerializer.Serialize(stringArray);
            Settings.Default.Expanded_Folders = str;
        }

        private void SaveFilterColor()
        {
            var filterColor = StringsFilter.Filter.SelectedColor;
            var savedColor = JsonSerializer.Serialize(filterColor);
            Settings.Default.Selected_Color = savedColor;

        }
        private void LoadFilterColor()
        {
            if (!string.IsNullOrWhiteSpace(Settings.Default.Selected_Color))
            {
                var savedColor = JsonSerializer.Deserialize<System.Windows.Media.Color>(Settings.Default.Selected_Color);
                StringsFilter.Filter.SelectedColor = savedColor;
            }
        }

        private void SaveFontSize()
        {
            var fontSize = FontValue;
            var savedfontSize = JsonSerializer.Serialize(fontSize);
            Settings.Default.Selected_FontSize = savedfontSize;

        }
        private void LoadFontSize()
        {
            if (!string.IsNullOrWhiteSpace(Settings.Default.Selected_FontSize))
            {
                var savedFontSize = JsonSerializer.Deserialize<double>(Settings.Default.Selected_FontSize);
                FontValue = savedFontSize;
            }
        }

        public IEnumerable<Control> GridContextMenu
        {
            get
            {
                var item = new MenuItem { Header = "Rescan Strings" };
                item.Click += RescanStrings;
                item.FontSize = FontSizeValue;

                yield return item;

                yield return new Separator();

                item = new MenuItem { Header = "Export..." };
                item.Click += ExportSelected;
                item.IsEnabled = Strings.SelectedItems.Count > 0;
                item.FontSize = FontSizeValue;
                yield return item;

                item = new MenuItem { Header = "Import..." };
                item.Click += Import;
                item.FontSize = FontSizeValue;
                yield return item;

                yield return new Separator();

                if (StringEntry.SourceLocale != Locale.TranslationSource && AppConfig.Instance.DeepLAvailable)
                {
                    item = new MenuItem { Header = "DeepL translate" };
                    item.Click += DeepLTranslate;
                    item.FontSize = FontSizeValue;
                    yield return item;

                    item = new MenuItem { Header = "DeepL translate comments" };
                    item.Click += DeepLTranslateComments;
                    item.FontSize = FontSizeValue;
                    yield return item;


                    yield return new Separator();
                }

                item = new MenuItem { Header = "String Details" };
                item.Click += ShowStringDetails;
                item.FontSize = FontSizeValue;
                yield return item;

                if (ImportResults != null)
                {
                    item = new MenuItem { Header = "Last Import Results" };
                    item.Click += ShowImportResults;
                    item.FontSize = FontSizeValue;
                    yield return item;
                }

                if (ExportResults != null)
                {
                    item = new MenuItem { Header = "Last Export Results" };
                    item.Click += ShowExportResults;
                    item.FontSize = FontSizeValue;
                    yield return item;
                }

                item = new MenuItem { Header = "Open file directory" };
                item.Click += OpenFileDirectories;
                item.FontSize = FontSizeValue;
                yield return item;

                item = new MenuItem { Header = "Open file" };
                item.Click += OpenFileFromDirectory;
                item.FontSize = FontSizeValue;
                yield return item;

                yield return new Separator();
                var selectedItem = ((StringEntry)Strings.SelectedItem);
                if (StringEntry.SourceLocale == Locale.TranslationSource)
                {
                    item = new MenuItem { Header = "Update Translation Source" };
                    item.Click += UpdateTranslationSourceSelected;
                    item.IsEnabled = Strings.SelectedItems.Count > 0;
                    item.FontSize = FontSizeValue;
                    yield return item;

                    item = new MenuItem { Header = "Apply Diffs" };
                    item.FontSize = FontSizeValue;
                    if (selectedItem != null && selectedItem.SourceLocaleEntry.TryGetInlinesCollection(InlineCollectionType.DiffSource, out var inlinesWrapper))
                    {
                        int pointer = 0;
                        for (int i = 0; i < inlinesWrapper.InlineTemplates.Length; i++)
                        {
                            var diffInline = inlinesWrapper.InlineTemplates[i];
                            if (diffInline.InlineType == InlineType.DiffDelete)
                            {
                                var diffItem = new MenuItem { Header = "DEL: " + diffInline.Text };
                                item.Items.Add(diffItem);
                                int currentPointer = pointer;
                                diffItem.Click += (object sender, RoutedEventArgs e) => ApplyInlineRemove(selectedItem, currentPointer, diffInline);
                                pointer += diffInline.Text.Length;
                            }
                            else if (diffInline.InlineType == InlineType.DiffInsert)
                            {
                                var diffItem = new MenuItem { Header = "ADD: " + diffInline.Text };
                                int currentPointer = pointer;
                                diffItem.Click += (object sender, RoutedEventArgs e) => ApplyInlineInsert(selectedItem, currentPointer, diffInline);
                                item.Items.Add(diffItem);
                            }
                            else
                            {
                                pointer += diffInline.Text.Length;
                            }
                        }
                    }

                    item.IsEnabled = Strings.SelectedItems.Count > 0;
                    if (item.Items.Count > 0)
                        yield return item;
                }
                else
                {
                    item = new MenuItem { Header = "Force Translation Source" };
                    item.Click += ForceTranslationSourceSelected;
                    item.IsEnabled = Strings.SelectedItems.Count > 0;
                    item.FontSize = FontSizeValue;
                    yield return item;
                }

                item = new MenuItem { Header = "Change Traits..." };
                item.Click += ChangeTraitsSelected;
                item.IsEnabled = Strings.SelectedItems.Count > 0;
                item.FontSize = FontSizeValue;
                yield return item;

                yield return new Separator();
                item = new MenuItem { Header = "Terms Glossary" };
                item.FontSize = FontSizeValue;
                if (selectedItem != null &&
                    Glossary.Instance.TryGetTermsInStringEntry(selectedItem, out var termEntries))
                {
                    foreach (var term in Glossary.Instance.FilterDuplicates(termEntries))
                    {
                        TextBlock text = new TextBlock();
                        string termLocale = Glossary.Instance.GetTermLocale(term.TermId, selectedItem);
                        text.Inlines.AddRange(
                            new Inline[]
                            {
                                new Run(termLocale) { FontWeight = FontWeights.Bold},
                                new Run("\n" + term.Comment)
                            });
                        item.Items.Add(new MenuItem { Header = text });
                    }

                    yield return item;
                    yield return new Separator();
                }

                yield return new Separator();

                item = new MenuItem { Header = "Delete" };
                item.Click += DeleteStringsSelected;
                item.IsEnabled = Strings.SelectedItems.Count > 0;
                item.FontSize = FontSizeValue;
                yield return item;
            }

        }

        private void ApplyInlineRemove(StringEntry item, int index, InlineTemplate diffInline)
        {
            if (StringEntry.SourceLocale != Locale.TranslationSource)
                return;

            item.Reload();
            var localeData = item.Data.GetLocale(StringEntry.TargetLocale);
            if (localeData?.TranslatedFrom == null)
                return;

            localeData.OriginalText = localeData.OriginalText.Remove(index, diffInline.Text.Length);
            item.Save();

            item.UpdateInlines();

            if (Filter.Mode == FilterMode.Updated_Source)
                UpdateFilter();
            else
                UpdateStringsView();
        }
        private void ApplyInlineInsert(StringEntry item, int index, InlineTemplate diffInline)
        {
            if (StringEntry.SourceLocale != Locale.TranslationSource)
                return;

            item.Reload();
            var localeData = item.Data.GetLocale(StringEntry.TargetLocale);
            if (localeData?.TranslatedFrom == null)
                return;

            localeData.OriginalText = localeData.OriginalText.Insert(index, diffInline.Text);
            item.Save();

            item.UpdateInlines();

            if (Filter.Mode == FilterMode.Updated_Source)
                UpdateFilter();
            else
                UpdateStringsView();
        }

        public IEnumerable<Control> FoldersContextMenu
        {
            get
            {
                var item = new MenuItem { Header = "Rescan Strings" };
                item.Click += RescanStrings;
                item.FontSize = FontSizeValue;
                yield return item;

                yield return new Separator();

                item = new MenuItem { Header = "Export..." };
                item.Click += ExportAll;
                item.IsEnabled = Strings.Items.Count > 0;
                item.FontSize = FontSizeValue;
                yield return item;

                item = new MenuItem { Header = "Import..." };
                item.Click += Import;
                item.FontSize = FontSizeValue;
                yield return item;

                item = new MenuItem { Header = "Export wordcount" };
                item.Click += ExportWordcount;
                item.IsEnabled = Strings.Items.Count > 0;
                item.FontSize = FontSizeValue;
                yield return item;

                item = new MenuItem { Header = "Generate SVG" };
                item.Click += SaveAllDialogsIntoSvg;
                item.IsEnabled = Strings.Items.Count > 0;
                item.FontSize = FontSizeValue;
                yield return item;

                yield return new Separator();

                if (ImportResults != null)
                {
                    item = new MenuItem { Header = "Last Import Results" };
                    item.Click += ShowImportResults;
                    item.FontSize = FontSizeValue;
                    yield return item;
                }

                if (ExportResults != null)
                {
                    item = new MenuItem { Header = "Last Export Results" };
                    item.Click += ShowExportResults;
                    item.FontSize = FontSizeValue;
                    yield return item;
                }

                yield return new Separator();

                if (AppConfig.Instance.Engine == StringManager.EngineType.Unreal)
                {
                    item = new MenuItem { Header = "Remove unused" };
                    item.FontSize = FontSizeValue;
                    item.Click += RemoveUnusedStringsFolder;
                    yield return item;
                }

                if (StringEntry.SourceLocale != Locale.TranslationSource && AppConfig.Instance.DeepLAvailable)
                {
                    item = new MenuItem { Header = "DeepL translate" };
                    item.FontSize = FontSizeValue;
                    item.Click += DeepLTranslateFolder;
                    yield return item;

                    item = new MenuItem { Header = "DeepL translate comments" };
                    item.FontSize = FontSizeValue;
                    item.Click += DeepLTranslateCommentsFolder;
                    yield return item;

                    yield return new Separator();
                }

                if (StringEntry.SourceLocale == Locale.TranslationSource)
                {
                    item = new MenuItem { Header = "Update Translation Source" };
                    item.FontSize = FontSizeValue;
                    item.Click += UpdateTranslationSourceAll;
                    item.IsEnabled = Strings.Items.Count > 0;
                    yield return item;
                }

                item = new MenuItem { Header = "Change Traits..." };
                item.FontSize = FontSizeValue;
                item.Click += ChangeTraitsAll;
                item.IsEnabled = Strings.Items.Count > 0;
                yield return item;

                yield return new Separator();

            }
        }

        private void RemoveUnusedStringsFolder(object sender, RoutedEventArgs e)
        {
            var strings = Strings.Items.OfType<StringEntry>().Where(se => ((UnrealStringData)se.Data).Unused).ToArray();
            StringManager.DeleteStrings(strings);
            UpdateFilter();

        }

        private void DeepLTranslate(object sender, RoutedEventArgs e)
        {
            DeepLTranslateStrings(
                Strings.SelectedItems.OfType<StringEntry>().ToList());
        }
        private void DeepLTranslateFolder(object sender, RoutedEventArgs e)
        {
            DeepLTranslateStrings(
                Strings.Items.OfType<StringEntry>().ToList());
        }
        private void DeepLTranslateComments(object sender, RoutedEventArgs e)
        {
            DeepLTranslateComments(
                Strings.SelectedItems.OfType<StringEntry>().ToList());
        }
        private void DeepLTranslateCommentsFolder(object sender, RoutedEventArgs e)
        {
            DeepLTranslateComments(
                Strings.Items.OfType<StringEntry>().ToList());
        }

        private async void DeepLTranslateStrings(List<StringEntry> strings)
        {
            // for testing
            // for (int ii = 0; ii < strings.Count; ii++)
            // {
            //     strings[ii].TargetLocaleEntry.Text = TranslateUtility.PrepareTagsForTranslation(strings[ii].SourceLocaleEntry.Text);
            // }
            // UpdateFilter();
            // return;

            bool StringHasTranslation(StringEntry s) => !string.IsNullOrEmpty(s.TargetLocaleEntry.Text) && !(s.Data.GetLocale(s.TargetLocaleEntry.Locale)?.HasTrait("AIGenerated") ?? true);

            if (strings.Any(StringHasTranslation))
            {
                var result = MessageBox.Show(this, "Some strings already have non-AI translation. Translate anyway?", "Warning", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Cancel: return;
                    case MessageBoxResult.No:
                        strings.RemoveAll(StringHasTranslation);
                        break;
                    case MessageBoxResult.Yes:
                        break;
                }
            }


            ScanningLabel.Content = $"Translating {strings.Count}/{strings.Count}";
            ScanningLabel.Visibility = Visibility.Visible;

            try
            {
                var progress = new Progress<int>(count =>
                {
                    ScanningLabel.Content = $"Translating {count}/{strings.Count}";
                    UpdateFilter();
                });
                await TranslateUtility.Translate(strings, m_Translator, AppConfig.Instance.AddForRetranslationTag, AppConfig.Instance.Engine, progress);

                ScanningLabel.Content = "Scanning...";
                ScanningLabel.Visibility = Visibility.Hidden;
                UpdateFilter();
            }
            catch (Exception x)
            {
                x.ShowDetails();
            }
        }
        private async void DeepLTranslateComments(List<StringEntry> strings)
        {
            ScanningLabel.Content = $"Translating {strings.Count}/{strings.Count}";
            ScanningLabel.Visibility = Visibility.Visible;

            try
            {
                var progress = new Progress<int>(count =>
                {
                    ScanningLabel.Content = $"Translating {count}/{strings.Count}";
                    UpdateFilter();
                });
                await TranslateUtility.TranslateComment(strings, m_Translator, progress, Filter.Mode);

                ScanningLabel.Content = "Scanning...";
                ScanningLabel.Visibility = Visibility.Hidden;
                UpdateFilter();
            }
            catch (Exception x)
            {
                x.ShowDetails();
            }
        }

        private void OpenColorSelector_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                StringsFilter.Filter.SelectedColor = System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                SaveFilterColor();
                Settings.Default.Save();
            }
        }
        private void SetDefaultColor_Click(object sender, RoutedEventArgs e)
        {
            StringsFilter.Filter.SelectedColor = Brushes.LightBlue.Color;
            SaveFilterColor();
            Settings.Default.Save();
        }

        public Visibility ShowOnNonModdersOnly
            => AppConfig.Instance.ModdersVersion ? Visibility.Collapsed : Visibility.Visible;

        public Visibility GlossaryIsEnabled =>
            AppConfig.Instance.Glossary.GlossaryIsEnabled ? ShowOnNonModdersOnly : Visibility.Hidden;

        public Visibility ShowOnNonModdersOnUnityOnly
            => AppConfig.Instance.ModdersVersion ? Visibility.Collapsed : ShowOnUnityOnly;

        public Visibility ShowOnNonModdersOnUnrealOnly
            => AppConfig.Instance.ModdersVersion ? Visibility.Collapsed : ShowOnUnrealOnly;

        public Visibility ShowOnUnityOnly
            => AppConfig.Instance.Engine == StringManager.EngineType.Unity ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowOnUnrealOnly
            => AppConfig.Instance.Engine == StringManager.EngineType.Unreal ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowOnPathfinderOnly
            => AppConfig.Instance.Project == "Pathfinder: WotR" ? Visibility.Visible : Visibility.Collapsed;


        void ExportWordcount(object sender, RoutedEventArgs e)
        {
            int maxDepth = 0;
            var models = new List<FolderItemTreeModel>();

            void AddToListRecursive(FolderItemTreeModel model)
            {
                models.Add(model);
                foreach (var child in model.Children)
                {
                    AddToListRecursive(child);
                }

                if (maxDepth < model.Depth)
                    maxDepth = model.Depth;
            }

            foreach (FolderItemTreeModel model in Folders.SelectedItems)
            {
                AddToListRecursive(model);
            }

            using (var sw = new StreamWriter("structure.csv"))
            {
                foreach (var model in models)
                {
                    int modelDepth = model.Depth;
                    for (int ii = 0; ii < modelDepth; ii++)
                    {
                        sw.Write(" ,");
                    }
                    sw.Write(model.Name);
                    sw.Write(",");
                    for (int ii = modelDepth + 1; ii < maxDepth; ii++)
                    {
                        sw.Write(" ,");
                    }
                    sw.Write(model.StringCount);
                    sw.Write(",");
                    sw.Write(model.WordCount);
                    sw.WriteLine();
                }
            }

        }

        private void DublicatesClick(object sender, RoutedEventArgs e)
        {
            var dublicates = StringManager.Duplicates;
            if (dublicates.Count > 0)
            {
                new DublicateResolverWindow(this, dublicates).ShowDialog();
                Rescan();
            }
            else
            {
                MessageBox.Show(this, "Not have dublicates");
            }
        }

        private void OnSmartCommitClick(object sender, RoutedEventArgs e)
        {
            var fileName = "LocalizationTracker\\SmartLocCommit.exe";
            var fullPath = Path.Combine(Environment.CurrentDirectory, fileName);

            if (File.Exists(fullPath))
            {
                ProcessStartInfo pi = new ProcessStartInfo();
                pi.WorkingDirectory = "../Localization";
                pi.FileName = fileName;
                Process.Start(pi);
            }
            else
            {
                MessageBox.Show($"{fileName} not found in the current directory.");
            }
        }

        private CancellationTokenSource m_RescanCts = new();
        private Task m_RescanTask = Task.CompletedTask;

        private void Rescan()
        {
            m_RescanCts.Cancel();
            m_RescanCts = new CancellationTokenSource();
            var ct = m_RescanCts.Token;
            m_RescanTask = Scan(m_RescanTask, ct);
        }


        private async Task Scan(Task prevTask, CancellationToken ct)
        {
            try
            {
                await prevTask;
            }
            catch (Exception)
            {
            }

            ScanningLabel.Content = "Scanning...";
            ScanningLabel.Visibility = Visibility.Visible;

            var rootDir = new DirectoryInfo(AppConfig.Instance.StringsFolder);
            DirectoryInfo? dialogsDir = null;
            if (!string.IsNullOrEmpty(AppConfig.Instance.DialogsFolder))
            {
                dialogsDir = new DirectoryInfo(AppConfig.Instance.DialogsFolder);
            }
            try
            {
                await Task.Run(() => StringManager.Scan(rootDir, ct, dialogsDir), ct);

                ct.ThrowIfCancellationRequested();

                var selectedFolders = Folders.SelectedItems
                    .Cast<FolderItemTreeModel>()
                    .Select(v => (v.Folder, v.IsRootFolderItem))
                    .ToImmutableHashSet();

                UpdateFolderSource(rootDir, selectedFolders);

                UpdateFilter();

                scanCompletionSource.SetResult(true);

            }
            catch (OperationCanceledException)
            {
            }
            catch (AggregateException ex)
            {
                var messages = string.Join("\n", ex.InnerExceptions.Select(e => e.Message));
                ex.ShowMessageBox(messages, "Failed to scan strings");
                scanCompletionSource.SetResult(false);
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox(ex.Message, "Failed to scan strings");
                scanCompletionSource.SetException(ex);
            }

            ScanningLabel.Visibility = Visibility.Hidden;
            scanCompletionSource = new TaskCompletionSource<bool>();
        }


        public async void SaveAllDialogsIntoSvg(object sender, RoutedEventArgs e)
        {
            var selectedStrings = Strings.Items.OfType<StringEntry>().ToArray();

            var selectedDialog = selectedStrings.Where(w => w.DialogsDataList != null && w.DialogsDataList.Any()).Select(s => s.DialogsDataList.First()).Distinct();

            foreach (var dialog in selectedDialog)
            {
                await Task.Run(() => GenerateSVG.FindAllDialogues(dialog.Name));
            }

            MessageBox.Show("Generation complete!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void UpdateFilterMode()
        {
            TraitSelection.Visibility =
                Filter.Mode == FilterMode.Updated_Trait
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            var voiceCommentSource = Strings.Columns[8];
            var voiceCommentTarget = Strings.Columns[9];
            var commentSource = Strings.Columns[6];
            var commentTarget = Strings.Columns[7];
            var sourceTextVoiceColumn = Strings.Columns[10];
            var targetTextVoiceColumn = Strings.Columns[11];
            var sourceText = Strings.Columns[4];
            var targetText = Strings.Columns[5];

            commentSource.Visibility = Visibility.Visible;
            commentTarget.Visibility = Visibility.Visible;
            voiceCommentSource.Visibility = Visibility.Collapsed;
            voiceCommentTarget.Visibility = Visibility.Collapsed;
            sourceTextVoiceColumn.Visibility = Visibility.Collapsed;
            targetTextVoiceColumn.Visibility = Visibility.Collapsed;
            sourceText.Visibility = Visibility.Visible;
            targetText.Visibility = Visibility.Visible;

            var targetColumn = Strings.Columns[5];
            targetColumn.Visibility =
                Filter.Mode == FilterMode.Updated_Trait
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            var keyColumn = Strings.Columns[0];
            if (Filter.Mode == FilterMode.Key_Duplicates)
            {
                Strings.Items.SortDescriptions.Clear();
                keyColumn.Visibility = Visibility.Visible;
                keyColumn.SortDirection = ListSortDirection.Ascending;
            }
            else if (Filter.Mode == FilterMode.Voice_Comments)
            {
                keyColumn.Visibility = Visibility.Visible;
                voiceCommentSource.Visibility = Visibility.Visible;
                voiceCommentTarget.Visibility = Visibility.Visible;
                commentSource.Visibility = Visibility.Collapsed;
                commentTarget.Visibility = Visibility.Collapsed;
                sourceTextVoiceColumn.Visibility = Visibility.Visible;
                targetTextVoiceColumn.Visibility = Visibility.Visible;
                sourceText.Visibility = Visibility.Collapsed;
                targetText.Visibility = Visibility.Collapsed;
            }
            else
            {
                keyColumn.Visibility = Visibility.Collapsed;
                keyColumn.SortDirection = null;
            }
        }

        private Task s_LastUpdateTask = Task.CompletedTask;
        private CancellationTokenSource s_cts = new CancellationTokenSource();

        private async Task UpdateFilter(bool updateInlines = false)
        {
            StringEntry[] filtered;
            var locale = SourceLocale;

            Filter.Update();

            if (updateInlines)
            {
                Parallel.ForEach(
                    StringsFilter.FilteredStrings,
                    entry =>
                    {
                        entry.UpdateInlines();
                    });
            }

            if (!string.IsNullOrEmpty(Filter.NameMultiline))
                Filter.CheckMultilineSearch(StringsFilter.FilteredStrings);
            else
                Filter.NotFound = "";

            UpdateStringsCount(StringsFilter.FilteredStrings, null, 0, StringsFilter.FilteredStrings.Length, FoldersSource);
            UpdateStringsView();

            if (WordCueCount)
                UpdateCounts(StringsFilter.FilteredStrings, locale);
        }

        private void UpdateCounts(StringEntry[] sortedPaths, Locale locale)
        {
            s_cts.Cancel();
            s_cts = new CancellationTokenSource();

            s_LastUpdateTask = UpdateFilterImpl(s_LastUpdateTask, sortedPaths, locale, s_cts.Token);
        }


        private async Task UpdateFilterImpl(
            Task prevTask, StringEntry[] sortedPaths, Locale locale, CancellationToken ct)
        {
            try
            {
                await prevTask;
            }
            catch (Exception)
            {
            }

            await CalcLengthsArray(sortedPaths, locale, ct);
        }

        private async Task CalcLengthsArray(StringEntry[] sortedPaths, Locale locale, CancellationToken ct)
        {
            int GetWordCount(StringEntry path)
            {
                var lsd = path.Data;

                var lang = lsd.GetLocale(locale);
                if (lang == null)
                    return 0;
                var text = lang.Text;
                var words = StringUtils.CountTotalWords(text);
                return words;
            }

            var lengths = await Task.Run(
                () => sortedPaths.AsParallel().Select(GetWordCount).ToArray(),
                ct);
            UpdateStringsCount(sortedPaths, lengths, 0, sortedPaths.Length, FoldersSource);
        }

        private void UpdateStringsCount(
            StringEntry[] sortedStringPaths, int[]? wordCount, int _start, int _count, IEnumerable items)
        {


            int lastIndex = _start;
            foreach (var item in items.Cast<FolderItemTreeModel>())
            {
                var dir = item.Folder;

                Comparison<StringEntry, string> comparison =
                    item.IsRootFolderItem ? StringEntryComparison.Exact : StringEntryComparison.LimitLen;

                int firstIndex = sortedStringPaths.LowerBound(
                    lastIndex,
                    _start + _count - lastIndex,
                    dir,
                    comparison);
                lastIndex = sortedStringPaths.UpperBound(
                    firstIndex,
                    _start + _count - firstIndex,
                    dir,
                    comparison);

                var count = wordCount == null ? lastIndex - firstIndex : 0;
                var wordsCount = 0;
                if (wordCount != null)
                {
                    for (int i = firstIndex; i < lastIndex; i++)
                    {
                        if (sortedStringPaths[i].Data.ShouldCount)
                        {
                            wordsCount += wordCount[i];
                            count++;
                        }
                    }
                }

                item.StringCount = count;
                item.ShowCounts = WordCueCount;
                item.WordCount = wordsCount;
                UpdateStringsCount(sortedStringPaths, wordCount, firstIndex, lastIndex - firstIndex, item.Children);
            }
        }

        private void UpdateFolderSource(
            DirectoryInfo rootDir,
            IReadOnlySet<(string FullName, bool IsRootFolderItem)> selectedFolders)
        {
            FoldersSource.Clear();
            var stack = new Stack<(FolderItemTreeModel, int)>();
            int current = 0;

            // we want to recursively add all strings: adding Strings/Foo/Bar to Strings/Foo to Strings
            // AND add a special item to all folders where there are strings inside (i.e. not all strings are accounted for by subfolders)
            while (current < StringManager.AllFolders.Count)
            {
                var cf = StringManager.AllFolders[current];
                if (stack.Count == 0)
                {
                    // we're adding a new root folder                     
                    var root = CreateTreeItem(null, cf.Item1, selectedFolders);
                    stack.Push((root, cf.Item2));
                    FoldersSource.Add(root);
                    current++;
                }
                else
                {
                    var prev = stack.Pop();
                    if (cf.Item1.StartsWith(prev.Item1.Folder))
                    {
                        // we're adding a subfolder
                        var next = CreateTreeItem(prev.Item1, cf.Item1, selectedFolders);
                        prev.Item1.Children.Add(next);

                        // push the root back on the stack, but reduce the count - we've handled this many strings in the subfolder
                        stack.Push((prev.Item1, prev.Item2 - cf.Item2));
                        // push the added folder
                        stack.Push((next, cf.Item2));
                        current++;
                    }
                    else
                    {
                        // we're moving up the stack. If there are any strings unhandled by sub-folders, add an item in the root for strings
                        if (prev.Item2 > 0 && prev.Item1.Children.Count > 0)
                        {
                            var dirRootItem = new FolderItemTreeModel(prev.Item1, prev.Item1.Folder, true)
                            {
                                IsSelected = selectedFolders.Contains((prev.Item1.Folder, true))
                            };
                            prev.Item1.Children.Insert(0, dirRootItem);
                        }
                        // do not push this item back on stack: we're done with it
                        // do not increment current: we haven't yet added it to the collection
                    }
                }
            }

            // if stack is not empty, add items for "unhandled" strings for all folders in it
            while (stack.Count > 0)
            {
                var folder = stack.Pop();
                if (folder.Item2 > 0 && folder.Item1.Children.Count > 0)
                {
                    var dirRootItem = new FolderItemTreeModel(folder.Item1, folder.Item1.Folder, true)
                    {
                        IsSelected = selectedFolders.Contains((folder.Item1.Folder, true))
                    };
                    folder.Item1.Children.Insert(0, dirRootItem);
                }
            }
        }

        private static FolderItemTreeModel CreateTreeItem(
            FolderItemTreeModel? parent, string dir, IReadOnlySet<(string Folder, bool IsRootFolderItem)> selected)
        {
            var item = new FolderItemTreeModel(parent, dir, false)
            {
                IsExpanded = ExpandedFolders.Contains(dir),
                IsSelected = selected.Contains((dir, false))
            };

            item.PropertyChanged +=
                (sender, args) =>
                {
                    if (args.PropertyName == "IsExpanded")
                    {
                        if (item.IsExpanded)
                        {
                            ExpandedFolders.Add(item.Folder);
                            SaveExpandedFolders();
                            Settings.Default.Save();
                        }
                        else
                        {
                            ExpandedFolders.Remove(item.Folder);
                            SaveExpandedFolders();
                            Settings.Default.Save();
                        }
                    }
                };

            return item;
        }

        private static int FindNextCommonSeparator(int startIdx, string[] strings)
        {
            int minLen = strings.Select(v => v.Length).Min();
            if (startIdx > minLen)
                return -1;
            for (int pos = startIdx; ; ++pos)
            {
                if (strings.All(str => pos == str.Length || str[pos] == '/'))
                    return pos;

                if (strings.Any(str => pos == str.Length))
                    return -1;

                if (strings.Select(v => v[pos]).Distinct().Count() > 1)
                    return -1;
            }
        }

        public static string GetLongestCommonPathPrefix(string[] strings)
        {
            if (strings.Length <= 0)
            {
                return "";
            }

            if (strings.Length == 1)
            {
                return strings[0];
            }

            int lastSlashPos = FindNextCommonSeparator(0, strings);
            if (lastSlashPos == -1)
            {
                return "";
            }

            while (true)
            {
                int nextSlashPos = FindNextCommonSeparator(lastSlashPos + 1, strings);
                if (nextSlashPos == -1)
                    return strings[0][0..lastSlashPos];
                lastSlashPos = nextSlashPos;
            }
        }

        [CanBeNull]
        private string GetBaseSelectedDir()
        {
            var dirPaths = Folders.SelectedItems.OfType<FolderItemTreeModel>()
                .Select(m => m.Folder)
                .ToArray();

            if (dirPaths.Length <= 0)
            {
                return null;
            }

            var basePath = GetLongestCommonPathPrefix(dirPaths);
            if (basePath.EndsWith("/"))
            {
                basePath = basePath.Substring(0, basePath.Length - 1);
            }

            return basePath;
        }

        private void UpdateStringsView()
        {
            // save settings regulary
            Settings.Default.Save();

            Strings.CommitEdit();
            StringsSource.Clear();

            var dirs = Folders.SelectedItems.Cast<FolderItemTreeModel>()
                .OrderBy(p => p.Folder, StringComparer.InvariantCultureIgnoreCase)
                .ToArray();

            if (dirs.Length <= 0)
            {
                return;
            }

            var baseDir = GetBaseSelectedDir();
            StringEntry.SelectedDirPrefix = baseDir;

            int lastIndex = 0;
            foreach (var item in dirs)
            {
                Comparison<StringEntry, string> comparison =
                    item.IsRootFolderItem ? StringEntryComparison.Exact : StringEntryComparison.LimitLen;
                int firstIndex = StringsFilter.FilteredStrings.LowerBound(
                    lastIndex,
                    StringsFilter.FilteredStrings.Length - lastIndex,
                    item.Folder,
                    comparison);
                lastIndex = StringsFilter.FilteredStrings.UpperBound(
                    firstIndex,
                    StringsFilter.FilteredStrings.Length - firstIndex,
                    item.Folder,
                    comparison);

                var newEntries = StringsFilter.FilteredStrings.AsMemory()[firstIndex..lastIndex];
                foreach (var se in newEntries.Span)
                    se.ClearRelativePath();

                StringsSource.AddRange(newEntries.Span);
            }
        }

        private Memory<StringEntry> ClearTags(Memory<StringEntry> newEntries, string[] tagsEncyclopedia)
        {
            foreach (var item in newEntries.Span)
            {
                item.SourceLocaleEntry.Text = StringUtils.RemoveTagsExcept(item.SourceLocaleEntry.Text, Array.Empty<string>());
                item.TargetLocaleEntry.Text = StringUtils.RemoveTagsExcept(item.TargetLocaleEntry.Text, Array.Empty<string>());
            }

            return newEntries;
        }

        private void RescanStrings(object sender, RoutedEventArgs e)
        {
            Rescan();
        }

        private void ChangeTraitsAll(object sender, EventArgs e)
        {
            ChangeTraits(StringsSource);
        }

        private void ChangeTraitsSelected(object sender, EventArgs e)
        {
            ChangeTraits(Strings.SelectedItems.OfType<StringEntry>());
        }

        private void UpdateTranslationSourceAll(object sender, EventArgs e)
        {
            UpdateTranslationSource(StringsSource);
        }

        private void UpdateTranslationSourceSelected(object sender, EventArgs e)
        {
            UpdateTranslationSource(Strings.SelectedItems.OfType<StringEntry>());
        }

        private void ForceTranslationSourceSelected(object sender, EventArgs e)
        {
            ForceTranslationSource(Strings.SelectedItems.OfType<StringEntry>());
        }

        private void ExportSelected(object sender, EventArgs e)
        {
            var selectedStrings = Strings.SelectedItems.OfType<StringEntry>().ToArray();
            Export(selectedStrings);
        }

        private void ExportAll(object sender, EventArgs e)
        {
            var selectedStrings = Strings.Items.OfType<StringEntry>().ToArray();
            Export(selectedStrings);
        }

        private void ChangeTraits(IEnumerable<StringEntry> items)
        {
            try
            {
                var dialog = new ChangeTraitsDialog { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    foreach (var item in items)
                    {
                        item.Reload();
                        foreach (var t in dialog.Traits)
                        {
                            if (dialog.Locale != null)
                            {
                                if (dialog.Remove)
                                {
                                    item.Data.RemoveTrait(dialog.Locale, t);
                                }
                                else
                                {
                                    item.Data.AddTrait(dialog.Locale, t);
                                }

                            }
                            else
                            {
                                if (dialog.Remove)
                                {
                                    item.Data.RemoveStringTrait(t);
                                }
                                else
                                {
                                    item.Data.AddStringTrait(t);
                                }
                            }
                        }


                        var locales = item.Data.Languages.SelectMany(w => w.Traits);
                        var stringTraits = item.Data.StringTraits;

                        //if (!string.IsNullOrEmpty(item.Speaker))
                        //{

                        //    foreach (var i in stringTraits)                //оставлю, если надо будет все трейты заполнять снова
                        //    {
                        //        if (string.IsNullOrEmpty(i.Speaker))
                        //            i.Speaker = item.Speaker;

                        //        if (string.IsNullOrEmpty(i.SpeakerGender))
                        //            i.SpeakerGender = item.Data.SpeakerGender;
                        //    }

                        //    foreach (var i in locales)
                        //    {
                        //        if (string.IsNullOrEmpty(i.Speaker))
                        //            i.Speaker = item.Speaker;

                        //        if (string.IsNullOrEmpty(i.SpeakerGender))
                        //            i.SpeakerGender = item.Data.SpeakerGender;
                        //    }

                        //}

                        item.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox("Could not change status.", "Unexpected error");
            }
        }

        private void UpdateTranslationSource(IEnumerable<StringEntry> items)
        {
            if (StringEntry.SourceLocale != Locale.TranslationSource)
                return;

            foreach (var item in items)
            {
                item.Reload();
                var localeData = item.Data.GetLocale(StringEntry.TargetLocale);
                if (localeData?.TranslatedFrom == null)
                    continue;

                var currentSource = item.Data.GetText(localeData.TranslatedFrom);
                localeData.OriginalText = currentSource;
                item.Save();

                item.UpdateInlines();
            }

            if (Filter.Mode == FilterMode.Updated_Source)
                UpdateFilter();
            else
                UpdateStringsView();
        }

        private void ForceTranslationSource(IEnumerable<StringEntry> items)
        {
            if (StringEntry.SourceLocale == Locale.TranslationSource)
                return;

            foreach (var item in items)
            {
                item.Reload();
                var localeData = item.Data.GetLocale(StringEntry.TargetLocale);
                if (localeData == null || string.IsNullOrEmpty(localeData.Text))
                    continue;

                item.Data.UpdateTranslation(
                    StringEntry.TargetLocale,
                    localeData.Text,
                    StringEntry.SourceLocale,
                    item.Data.GetText(StringEntry.SourceLocale));

                item.Save();
            }

            if (Filter.Mode == FilterMode.Updated_Source)
                UpdateFilter();
            else
                UpdateStringsView();
        }

        private void DeleteStringsSelected(object sender, EventArgs e)
        {
            var selected = Strings.SelectedItems.Cast<StringEntry>().ToArray();
            if (selected.Length == 0)
                return;

            int selectedIndex = Strings.SelectedIndex;

            StringManager.DeleteStrings(selected);

            UpdateFilter();
            Strings.Focus();
            Strings.SelectedIndex = selectedIndex;
            Strings.CurrentCell = Strings.SelectedCells.FirstOrDefault();
        }

        private void Import(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = LocalizationImporter.Import(this);

                if (result.ImportResults.Count != 0)
                {
                    ImportResults = new ImportResultsWindow(result);
                    ImportResults.Show();
                    UpdateFilter(true);
                }
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox("Could not import.", "Unexpected error");
            }
        }

        private void Export(StringEntry[] items)
        {
            try
            {
                var selectedDir = GetBaseSelectedDir();
                if (selectedDir == null) return;

                var traitSelector = this.FindName("UpdatedTraitsSelector") as TraitsSelector;

                var test = UpdatedTraitsSelector.SelectedTraits;

                if (traitSelector != null)
                {
                    var selectedTraits = traitSelector.SelectedTraits.ToList();
                    var selectedTraitsText = string.Join(", ", selectedTraits);

                    LocalizationExporter.Export(Path.GetFileName(selectedDir), items, this, selectedTraitsText);
                }
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox("Could not export.", "Unexpected error");
            }
        }

        private void ShowImportResults(object sender, RoutedEventArgs e)
        {
            ImportResults?.Show();
        }

        private void ShowExportResults(object sender, RoutedEventArgs e)
        {
            ExportResults?.Show();
        }

        [GeneratedRegex(
            @"m_JsonPath""?: ""?(?<Path>[^""\n]+)""?",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture)]
        private static partial Regex GrabJsonPaths();

        private static string[] ReadJsonPaths(string assetFilePath)
        {
            try
            {
                var assetText = File.ReadAllText(assetFilePath);
                if (!assetText.Contains("m_JsonPath"))
                    return Array.Empty<string>();

                var matches = GrabJsonPaths().Matches(assetText);
                return matches.Select(match => match.Groups["Path"].Value.Replace("\\\\", "/")).ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private void AddTraitsFilter(object sender, RoutedEventArgs e)
        {
            Filter.AddTraitsFilter();
        }

        private void RemoveTraitsFilter(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            Filter.RemoveTraitFilter(btn?.Tag as TraitsFilter);
        }

        private void ClearTraitFilters(object sender, RoutedEventArgs e)
        {
            Filter.ClearTraitFilters();
        }

        private void SaveTraitFilters(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "filter";
            dialog.DefaultExt = ".json";
            dialog.Filter = "Json files (.json)|*.json";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                // Save document
                Filter.SaveTraitFilters(dialog.FileName);
            }
        }

        private void LoadTraitFilters(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "filter";
            dialog.DefaultExt = ".json";
            dialog.Filter = "Json files (.json)|*.json";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                // Save document
                Filter.LoadTraitFilters(dialog.FileName);
            }
        }

        private void Folders_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement? fe = e.Source as FrameworkElement;
            if (fe == null)
                return;

            var menu = new ContextMenu { ItemsSource = FoldersContextMenu };
            fe.ContextMenu = menu;
        }

        private void Strings_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement? fe = e.Source as FrameworkElement;
            if (fe == null)
                return;

            var menu = new ContextMenu { ItemsSource = GridContextMenu };
            fe.ContextMenu = menu;
        }

        private void ShowStringDetails(object sender, RoutedEventArgs e)
        {
            if (StringDetails == null)
            {
                StringDetails = new StringDetailsWindow();
                StringDetails.Closed += (s, a) => StringDetails = null;
            }

            StringDetails?.Show();
            StringDetails?.Focus();
            OnStringsSelectionChanged();
        }

        private void Strings_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
            OnStringsSelectionChanged();

        private void OnStringsSelectionChanged()
        {
            if (StringDetails == null)
            {
                return;
            }

            var selected = Strings.SelectedItems.OfType<StringEntry>().ToList();
            if (selected.Count == 1)
            {
                StringDetails.DataContext = new StringDetailsVM(selected[0]);
            }
            else
            {
                StringDetails.DataContext = StringDetailsVM.Empty;
            }
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void Strings_OnKeyDown(object sender, KeyEventArgs e)
        {
            bool control = (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0;
            if (control && e.Key == Key.Delete && Filter.Mode == FilterMode.Key_Duplicates)
            {
                DeleteStringsSelected(sender, e);
            }

            // move content to duplicate string and delete
            if (control && e.Key == Key.M && Filter.Mode == FilterMode.Key_Duplicates)
            {
                var selected = Strings.SelectedItems.OfType<StringEntry>().ToList();
                if (selected.Count == 1)
                {
                    var targets = StringsFilter.FilteredStrings
                        .Where(se => se.Key == selected[0].Key)
                        .Where(se => se != selected[0])
                        .ToList();

                    if (targets.Count == 1)
                    {
                        selected[0].Reload();
                        targets[0].Data = selected[0].Data;
                        targets[0].Save();
                        targets[0].UpdateLocaleEntries();
                        DeleteStringsSelected(sender, e);
                    }
                }
            }
        }

        private void MultilineSearchText_Click(object sender, RoutedEventArgs e)
        {
            if (m_MultilineSearchWindow == null || m_MultilineSearchWindow.IsClosed)
            {
                m_MultilineSearchWindow = new MultilineSearch(this);
                m_MultilineSearchWindow.Show();
            }
            else
            {
                m_MultilineSearchWindow.Show();
                m_MultilineSearchWindow.Focus();
            }

            m_MultilineSearchWindow.WindowClosed += MultilineSearchWindowClosed;
        }

        private void UpdatedSpeakersWindow_Click(object sender, RoutedEventArgs e)
        {
            if (m_SpeakerChangedWindow == null || m_SpeakerChangedWindow.IsClosed)
            {
                m_SpeakerChangedWindow = new SpeakerChangedWindow(StringManager.AllStrings);
                m_SpeakerChangedWindow.Width = 745;
                m_SpeakerChangedWindow.Height = 600;
                m_SpeakerChangedWindow.Show();
            }
            else
            {
                m_SpeakerChangedWindow.Show();
                m_SpeakerChangedWindow.Focus();
            }

        }

        private void MultilineSearchWindowClosed(object sender, EventArgs e)
        {
            UpdateFilter(true);
        }

        private void OnUpdateGlossaryClick(object sender, RoutedEventArgs e)
        {
            Glossary.Instance.UpdateGlossary();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        
        {
            UpdateFilter(true);
        }

        private void OpenFileFromDirectory(object sender, RoutedEventArgs e)
        {
            var selectedStrings = Strings.SelectedItems.OfType<StringEntry>().ToArray();
            List<string> paths = selectedStrings.Select(s => s.AbsolutePath).ToList();

            foreach (var path in paths)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        MessageBox.Show($"Файл не найден: {path}");
                        continue;
                    }

                    var processInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    };

                    Process.Start(processInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия файла: {ex.Message}");
                }
            }
        }
        private void OpenFileDirectories(object sender, RoutedEventArgs e)
        {
            var selectedStrings = Strings.SelectedItems.OfType<StringEntry>().ToArray();
            List<string> paths = selectedStrings.Select(s => s.AbsolutePath).ToList();

            try
            {
                var uniqueDirectories = paths
                    .Where(File.Exists)
                    .Select(Path.GetDirectoryName)
                    .Where(dir => dir != null && Directory.Exists(dir))
                    .Distinct()
                    .ToList();

                foreach (var directory in uniqueDirectories)
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true
                    };

                    Process.Start(processInfo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия папок: {ex.Message}");
            }
        }



    }
}