using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Utility;

namespace LocalizationTracker.Data.Wrappers;

partial class StringsArchiveUnity : StringsArchive
{
    public override List<IStringData> LoadAll(DirectoryInfo rootDir, StringEntry[]? skipUnmodified, CancellationToken ct)
    {
        HashSet<string> files = rootDir.EnumerateFiles("*.json", SearchOption.AllDirectories)
            .AsParallel()
            .WithCancellation(ct)
            .Select(f => f.FullName)
            .ToHashSet();
        
        if (skipUnmodified!=null)
        {
            foreach (var se in skipUnmodified)
                files.Remove(se.AbsolutePath);
        }

        var newLoadedEntries = files
            .AsParallel()
            .WithCancellation(ct)
            .Select(LoadData);

        return newLoadedEntries.ToList();
    }

    private IStringData LoadData(string absolutePath)
    {
        if (absolutePath.Length >= 260)
        {
            throw new IOException($"The path is too long: {absolutePath}");
        }

        LocalizedStringData? Data = null;
        
        var modificationTime = File.GetLastWriteTime(absolutePath);
            
        var rootUri = new Uri(Path.GetFullPath(AppConfig.Instance.StringsFolder), UriKind.Absolute);
        var fileUri = new Uri(absolutePath, UriKind.Absolute);
            
        try
        {
            using (var sr = new StreamReader(absolutePath))
            {
                var text = sr.ReadToEnd();
                Data = JsonSerializer.Deserialize<LocalizedStringData>(text, JsonSerializerHelpers.JsonSerializerOptions);
            }
        }
        catch (Exception e)
        {
            throw new IOException($"Failed to parse json file: {absolutePath}", e);
        }

        if (Data == null)
            throw new IOException($"Failed to parse json file (data is null): {absolutePath}");
        
        Data.AbsolutePath = absolutePath;
        Data.StringPath = rootUri.MakeRelativeUri(fileUri).ToString();
        Data.ModificationDate = modificationTime;

        return Data;
    }

    public override Task SaveAll()
    {
        throw new System.NotImplementedException();
    }

    public override void Save(IStringData str)
    {
        var absolutePath = ((LocalizedStringData)str).AbsolutePath;
        if (!File.Exists(absolutePath))
        {
            throw new Exception("Строка больше не существует по старому пути. Нажмите Rescan или перезапустите локтулзу прежде, чем продолжить");
        }

        using (var sw = new StreamWriter(absolutePath))
        {
            var json = JsonSerializer.Serialize((LocalizedStringData)str, JsonSerializerHelpers.JsonSerializerOptions);
            var unescaped = JsonSerializerHelpers.UnescapeUnicodeSymbols(json);
            sw.Write(unescaped);
        }
    }

    public override IStringData Reload(IStringData str)
    {
        var absolutePath = ((LocalizedStringData)str).AbsolutePath;
        if (!File.Exists(absolutePath))
        {
            throw new Exception("Строка больше не существует по старому пути. Нажмите Rescan или перезапустите локтулзу прежде, чем продолжить");
        }

        return LoadData(absolutePath);
    }
    
    public override void Delete(IStringData str)
    {
        File.Delete(((LocalizedStringData)str).AbsolutePath);
    }

    public override bool IsFileModified(IStringData str)
    {
        var absolutePath = ((LocalizedStringData)str).AbsolutePath;
        return !File.Exists(absolutePath) || File.GetLastWriteTime(absolutePath) > str.ModificationDate;
    }
}