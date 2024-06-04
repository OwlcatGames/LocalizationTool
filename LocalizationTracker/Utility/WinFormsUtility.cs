using LocalizationTracker.Windows;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace LocalizationTracker.Utility
{
    class WinFormsUtility
    {
        public static ExportParams? TryGetExportParams(Window owner)
        {
            var dialog = new ExportDialog { Owner = owner };
            if (dialog.ShowDialog() == true)
                return dialog.ExportParams;
            return default;
        }


        public static bool TryGetOpenFilePath(string rootPath, string filter, out string[] selectedFiles)
        {
            var fileDialog = new OpenFileDialog()
            {
                InitialDirectory = rootPath,
                Filter = filter,
                Multiselect = true
            };

            selectedFiles = fileDialog.ShowDialog() == true ? fileDialog.FileNames : new string[0];
            return selectedFiles.Length > 0;
        }

        public static void OpenFolderAndSelectFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            // combine the arguments together
            // it doesn't matter if there is a space after ','
            string argument = "/select, \"" + filePath + "\"";

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }
    }
}
