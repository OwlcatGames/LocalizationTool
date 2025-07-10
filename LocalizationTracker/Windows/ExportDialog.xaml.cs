using LocalizationTracker.Components;
using LocalizationTracker.Data;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LocalizationTracker.Logic;
using StringsCollector.Data.Unity;

namespace LocalizationTracker.Windows
{
    public enum GridRowType
    {
        Format = 0,
        Source = 1,
        Target = 2,
        AddTraits = 3,
        RemoveTags = 4,
        Context = 5,
        Hierarchy = 7,
        Comments = 8,
        ExportSeparateFiles = 9,
        SortAsSvg = 10,
        Buttons = 11
    }


    public partial class ExportDialog
    {
        private static readonly Dictionary<ExportTarget, Dictionary<GridRowType, GridLength>> RowVisibilityConfig =
     new()
     {
         [ExportTarget.LocalizationToExcel] = new Dictionary<GridRowType, GridLength>
         {
             [GridRowType.Format] = GridLength.Auto,
             [GridRowType.Source] = GridLength.Auto,
             [GridRowType.Target] = GridLength.Auto,
             [GridRowType.AddTraits] = GridLength.Auto,
             [GridRowType.RemoveTags] = GridLength.Auto,
             [GridRowType.Context] = GridLength.Auto,
             [GridRowType.Hierarchy] = GridLength.Auto,
             [GridRowType.Comments] = GridLength.Auto,
             [GridRowType.ExportSeparateFiles] = GridLength.Auto,
             [GridRowType.SortAsSvg] = GridLength.Auto,
             [GridRowType.Buttons] = GridLength.Auto
         },
         [ExportTarget.VoiceComments] = new Dictionary<GridRowType, GridLength>
         {
             [GridRowType.Context] = new GridLength(0),
             [GridRowType.Comments] = new GridLength(0),
         },
         [ExportTarget.UpdatedTraitToExcel] = new Dictionary<GridRowType, GridLength>
         {
             [GridRowType.Context] = new GridLength(0),
             [GridRowType.Target] = new GridLength(0),
             [GridRowType.AddTraits] = new GridLength(0),
             [GridRowType.ExportSeparateFiles] = new GridLength(0),

         }
     };

        public Locale Source { get; set; } = StringEntry.SourceLocale;
        public Locale Target { get; set; } = StringEntry.TargetLocale;

        public ExportTarget ExportTarget { get; set; } = ExportTarget.LocalizationToExcel;
        public TagRemovalPolicy RemoveTags { get; set; } = TagRemovalPolicy.RetainAll;

        public bool ExtraContext { get; set; }

        public bool UseFolderHierarchy { get; set; }

        public bool IncludeComment { get; set; } = true;

        public bool SeparateFiles { get; set; } = false;
        public bool SortAsSvg { get; set; } = false;

        public Locale[] SourceValues => Locale.SourceLocaleValues;
        public Locale[] TargetValues => Locale.Values;

        public Visibility ShowOnUnrealOnly
                    => AppConfig.Instance.Engine == StringManager.EngineType.Unreal ? Visibility.Visible : Visibility.Collapsed;

        public ExportDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ExportParams ExportParams
            => new(ExportTarget, Source, TargetSelector.SelectedTargets.ToList(), RemoveTags, TraitsSelector.SelectedTraits.ToArray(), ExtraContext, UseFolderHierarchy, IncludeComment, SeparateFiles, SortAsSvg);

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }


        private void ApplyGridRowConfig(ExportTarget target)
        {
            foreach (var rowDef in MainGrid.RowDefinitions)
                rowDef.Height = GridLength.Auto;

            if (RowVisibilityConfig.TryGetValue(target, out var config))
            {
                foreach (var kvp in config)
                {
                    MainGrid.RowDefinitions[(int)kvp.Key].Height = kvp.Value;
                }
            }
            else
            {
                return;
            }
        }


        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyGridRowConfig(ExportTarget);
        }

    }

    public record struct ExportParams(ExportTarget ExportTarget, Locale Source, List<Locale> Target, TagRemovalPolicy TagRemovalPolicy, string[] Traits, bool ExtraContext, bool UseFolderHierarchy, bool IncludeComment, bool SeparateFiles, bool SortAsSvg);
}