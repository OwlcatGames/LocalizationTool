﻿using Kingmaker.Localization.Shared;
using LocalizationTracker.Components;
using LocalizationTracker.Data;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LocalizationTracker.Windows
{
    public partial class ExportDialog
    {
        public Locale Source { get; set; } = StringEntry.SourceLocale;
        public Locale Target { get; set; } = StringEntry.TargetLocale;

        public ExportTarget ExportTarget { get; set; } = ExportTarget.LocalizationToExcel;
        public TagRemovalPolicy RemoveTags { get; set; } = TagRemovalPolicy.RetainAll;        

        public bool ExtraContext { get; set; }

        public bool UseFolderHierarchy { get; set; }

        public bool IncludeComment { get; set; } = true;

        public bool SeparateFiles { get; set; } = false;

        public Locale[] SourceValues => Locale.SourceLocaleValues;
        public Locale[] TargetValues => Locale.Values;

        public ExportDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ExportParams ExportParams
            => new(ExportTarget, Source, TargetSelector.SelectedTargets.ToList(), RemoveTags, TraitsSelector.SelectedTraits.ToArray(), ExtraContext, UseFolderHierarchy, IncludeComment, SeparateFiles);

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

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ExportTarget == ExportTarget.LocalizationToExcel)
            {
                MainGrid.RowDefinitions.ElementAt(9).Height = GridLength.Auto;

            }
            else
            {
                MainGrid.RowDefinitions.ElementAt(9).Height = new GridLength(0);
            }
        }

    }

    public record struct ExportParams(ExportTarget ExportTarget, Locale Source, List<Locale> Target, TagRemovalPolicy TagRemovalPolicy, string[] Traits, bool ExtraContext, bool UseFolderHierarchy, bool IncludeComment, bool SeparateFiles);
}