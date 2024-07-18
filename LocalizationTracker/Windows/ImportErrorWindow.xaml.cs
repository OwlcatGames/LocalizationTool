using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace LocalizationTracker.Windows
{
    public partial class ImportErrorWindow : Window
    {
        public ImportErrorWindow(Dictionary<string, Exception> fails)
        {
            InitializeComponent();
            DisplayErrors(fails);
        }

        private void DisplayErrors(Dictionary<string, Exception> fails)
        {
            StringBuilder stringBuilder = new StringBuilder();

            ErrorTextBlock.Inlines.Add($"Failed to import {fails.Count} files:\r\n");
            foreach (var file in fails)
            {
                var name = System.IO.Path.GetFileName(file.Key);
                var message = file.Value.Message;

                Run fileNameRun = new Run(name)
                {
                    FontWeight = FontWeights.Bold
                };
                Run messageRun = new Run($" : {message}\r\n");

                ErrorTextBlock.Inlines.Add(fileNameRun);
                ErrorTextBlock.Inlines.Add(messageRun);
            }
        }
    }
}
