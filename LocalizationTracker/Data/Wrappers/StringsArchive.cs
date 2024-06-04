using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LocalizationTracker.Logic;

namespace LocalizationTracker.Data.Wrappers;

/// <summary>
/// Wraps the whole set of strings on disk. For Unity this is a folder with string files, for Unreal this is one giant json
/// </summary>
public abstract class StringsArchive
{
    public abstract List<IStringData> LoadAll(
        DirectoryInfo rootDir, StringEntry[]? skipUnmodified, CancellationToken ct);
    public abstract Task SaveAll();
    public abstract void Save(IStringData str);
    public abstract IStringData Reload(IStringData str);
    public abstract void Delete(IStringData str);
    public virtual bool HasUnsavedChanges => false; // Unity can save any string immediately, but Unreal has to track changes and save the whole list
    
    public virtual bool IsFileModified(IStringData str) => false;

    public event Action StringChanged= () => { };

    protected void RaiseStringChanged()
    {
        StringChanged();
    }
}