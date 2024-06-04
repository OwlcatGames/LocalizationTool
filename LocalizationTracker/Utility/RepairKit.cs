using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace LocalizationTracker.Utility
{
    class RepairKit
    {
        public static void RemovePrefixs(string path)
        {
            using (ZipArchive zFile = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                foreach (var zEntry in zFile.Entries)
                {
                    string text;
                    using var stream = zEntry.Open();
                    using var sr = new StreamReader(stream, encoding: Encoding.UTF8, leaveOpen: true);
                    text = sr.ReadToEnd();

                    text = text.Replace("<x:", "<");
                    text = text.Replace("</x:", "</");
                    text = text.Replace("<x:ext", "<ext");
                    text = text.Replace("</x:ext>", "</ext>");
                    text = text.Replace("xmlns:x", "xmlns");

                    stream.Seek(0, SeekOrigin.Begin);
                    using StreamWriter sw = new(stream, encoding: Encoding.UTF8, leaveOpen: true);
                    sw.Write(text);
                    stream.SetLength(stream.Position);
                }
            }
        }

        public static void RepairPathInExportXLSX(string path)
        {
            using (ZipArchive file = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var brokenNames = file.Entries.Where(v => !v.FullName.Replace("\\", "/").Equals(v.FullName, StringComparison.InvariantCultureIgnoreCase)).Select(v => v.FullName).ToArray();
                foreach (var brokenName in brokenNames)
                {
                    var entry = file.GetEntry(brokenName);
                    var newName = brokenName.Replace("\\", "/");
                    var newEntry = file.CreateEntry(newName);
                    using (var newStream = newEntry.Open())
                    using (var oldStream = entry.Open())
                    {
                        oldStream.CopyTo(newStream);
                    }
                    entry.Delete();
                }
            }
        }
    }
}
