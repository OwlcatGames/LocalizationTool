using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Data.Wrappers;
using LocalizationTracker.Logic;
using LocalizationTracker.Utility;

namespace LocalizationTracker.Data.Unreal;

class StringList
{
    [JsonInclude]
    [JsonPropertyName("strings")]
    public List<UnrealStringData> Strings;
}

class StringsArchiveUnreal : StringsArchive
{
    private bool m_HasUnsavedChanges;

    public override List<IStringData> LoadAll(
        DirectoryInfo rootDir, StringEntry[]? skipUnmodified, CancellationToken ct)
    {
        HashSet<string> files = rootDir.EnumerateFiles("*.json", SearchOption.AllDirectories)
            .AsParallel()
            .WithCancellation(ct)
            .Select(f => f.FullName)
            .ToHashSet();

        //if (skipUnmodified!=null) 
        //{
        //    foreach (var se in skipUnmodified)
        //        files.Remove(se.Key);
        //}

        var newLoadedEntries = files
            .AsParallel()
            .WithCancellation(ct)
            .Select(LoadData)
            .ToList();
; // todo: save the list, noting which strings go to which file?

        return newLoadedEntries;
    }

    private IStringData LoadData(string absolutePath)
    {
        UnrealStringData? usd;

        try
        {
            using (var sr = new StreamReader(absolutePath))
            {
                var text = sr.ReadToEnd();
                usd = JsonSerializer.Deserialize<UnrealStringData>(text, JsonSerializerHelpers.JsonSerializerOptions);
            }
        }
        catch (Exception e)
        {
            throw new IOException($"Failed to parse json file: {absolutePath}", e);
        }

        if (usd == null)
        {
            throw new IOException($"Failed to parse json file: {absolutePath}. Wrong object type.");
        }

        {
            usd.SourceFile = absolutePath;
        }

        return usd;
    }

    public override Task SaveAll()
    {
        return Task.Run(
            () =>
            {
                var group = StringManager.AllStrings
                    .AsParallel()
                    .Select(s => s.Data)
                    .Cast<UnrealStringData>()
                    //.GroupBy(d => d.SourceFile)
                    .Where(d => d.HasUnsavedChanges)
                    .ToList();

             //  foreach (var file in group)
                {
                    // var list = new StringList() { Strings = file.OrderBy(s=>s.InternalOrder).ToList() };

                    foreach (var data in group)
                    {
                        data.HasUnsavedChanges = false;

                        try
                        {
                            using (var sw = new StreamWriter(data.SourceFile))
                            {
                                var text = JsonSerializer.Serialize(data, JsonSerializerHelpers.JsonSerializerOptions);
                                text = JsonSerializerHelpers.UnescapeUnicodeSymbols(text);
                                sw.WriteLine(text);
                            }
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine(x.ToString());
                        }

                    }

                }

                m_HasUnsavedChanges = false;
            });
    }

    public override void Save(IStringData str)
    {
        var absolutePath = ((UnrealStringData)str).SourceFile;
        if (!File.Exists(absolutePath))
        {
            throw new Exception("Строка больше не существует по старому пути. Нажмите Rescan или перезапустите локтулзу прежде, чем продолжить");
        }

        using (var sw = new StreamWriter(absolutePath))
        {
            var json = JsonSerializer.Serialize((UnrealStringData)str, JsonSerializerHelpers.JsonSerializerOptions);
            var unescaped = JsonSerializerHelpers.UnescapeUnicodeSymbols(json);
            sw.Write(unescaped);
        }
        
        RaiseStringChanged();
    }

    public override IStringData Reload(IStringData str)
    {
        var absolutePath = ((UnrealStringData)str).SourceFile;
        if (!File.Exists(absolutePath))
        {
            throw new Exception("Строка больше не существует по старому пути. Нажмите Rescan или перезапустите локтулзу прежде, чем продолжить");
        }

        return LoadData(absolutePath);
    }

    public override void Delete(IStringData str)
    {
        File.Delete(((UnrealStringData)str).SourceFile);
        RaiseStringChanged();
    }

    public override bool IsFileModified(IStringData str)
    {
        return true;
        //var absolutePath = ((UnrealStringData)str).SourceFile;
        //return !File.Exists(absolutePath) || File.GetLastWriteTime(absolutePath) > str.ModificationDate;
    }

    public override bool HasUnsavedChanges => m_HasUnsavedChanges;
}