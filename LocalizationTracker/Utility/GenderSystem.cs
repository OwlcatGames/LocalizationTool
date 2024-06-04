using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace LocalizationTracker.Utility
{
    static class GenderSystem
    {
        static Dictionary<string, Gender> _characterGenders = new Dictionary<string, Gender>();

        static HashSet<string> _excludedName = new HashSet<string>() { "Names" };

        static bool _wasInited = false;

        public static string GetNameWithGender(string name)
        {
            if (!_characterGenders.TryGetValue(name, out var gender))
                gender = Gender.Unknown;
            return $"{name}:{gender}";
        }

        public static void Init()
        {
            if (_wasInited)
                return;

            _wasInited = true;
            Rescan();
        }

        public static void Rescan()
        {
            _characterGenders = new Dictionary<string, Gender>();
            var rootPath = AppConfig.Instance.UnitsFolder;
            if (!string.IsNullOrEmpty(rootPath))
            {
                ScanFolder(rootPath);
            }
            else MessageBox.Show("В настроечном файле не указан путь до папки с ассетами юнитов. \nОпределить гендер говорящего будет невозможно");
        }

        static void ScanFolder(string folderPath)
        {
            if (!CheckFolderIsIncluded(folderPath))
                return;

            foreach (var file in Directory.EnumerateFiles(folderPath, "*.asset"))
            {
                var fileText = File.ReadAllText(file);
                Regex regex = new Regex($@"Gender: ([0-9])");
                var match = regex.Match(fileText);
                if (match != null)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var gender = GetValueFromString(match.Value);
                    if (!_characterGenders.ContainsKey(name))
                        _characterGenders.Add(name, (Gender)gender);
                }
            }

            var childs = Directory.GetDirectories(folderPath);
            foreach (var child in childs)
                ScanFolder(child);
        }

        static bool CheckFolderIsIncluded(string folderPath)
        {
            foreach (var name in _excludedName)
            {
                if (folderPath.Contains(name))
                    return false;
            }

            return true;
        }

        static int GetValueFromString(string input)
        {
            var builder = new StringBuilder();
            int val = 0;

            for (int i = 0; i < input.Length; i++)
            {
                var chr = input[i];
                if (char.IsDigit(chr))
                    builder.Append(chr);
            }

            if (builder.Length > 0)
            {
                var value = builder.ToString();
                int.TryParse(value, out val);
            }

            return val;
        }

        enum Gender
        {
            Male = 0,
            Female = 1,
            Unknown = 255
        }
    }
}
