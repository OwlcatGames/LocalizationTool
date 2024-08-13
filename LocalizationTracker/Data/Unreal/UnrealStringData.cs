using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Data.Wrappers;
using LocalizationTracker.Utility;

namespace LocalizationTracker.Data.Unreal;

public class UnrealStringData : IStringData
{
    public static readonly long
        FileTimeOffset =
            DateTime.Parse("12:00AM January 1, 1601").Ticks; // difference between Unreal time and FileTimeUtc

    [JsonInclude]
    [JsonPropertyName("textId")]
    public StringId m_TextId;

    [JsonInclude]
    [JsonPropertyName("ownerPath")]
    public string m_OwnerPath;

    [JsonInclude]
    [JsonPropertyName("source")]
    public Locale m_Source;

    [JsonInclude]
    [JsonPropertyName("source_text")]
    public string
        m_SourceText; // not used right now, but required so that we do not lose the field (which will make unreal crash)

    [JsonInclude]
    [JsonPropertyName("source_modification_date")]
    public long m_ModificationDateInTicks;

    [JsonInclude]
    [JsonPropertyName("comment")]
    public string m_Comment;

    public enum StringKind
    {
        Default,
        DialogCue,
        DialogAnswer
    };

    [JsonInclude]
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public StringKind m_Kind;

    [JsonInclude]
    [JsonPropertyName("speaker")]
    public string m_Speaker;

    [JsonInclude]
    [JsonPropertyName("speaker_gender")]
    public string m_SpeakerGender;

    [JsonInclude]
    [JsonPropertyName("unused")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Unused;

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonDefaultValue("")]
    [JsonPropertyName("screenshot")]
    public string m_Screenshot = "";

    [JsonInclude]
    [JsonPropertyName("languages")]
    public List<UnrealLocaleData> m_Languages = new();

    [JsonInclude]
    [JsonPropertyName("string_traits")]
    public List<UnrealTraitData> m_StringTraits = new();
    
    private List<UnrealTraitData> m_VirtualStringTraits = new(); // not serialized

    string? m_FolderPath;

    [JsonIgnore]
    public Locale Source => m_Source;

    [JsonIgnore]
    public string Key => m_TextId.ToString();

    [JsonIgnore]
    public string Comment
    {
        get => m_Comment;
        set => m_Comment = value;
    }

    [JsonIgnore]
    public StringKind Kind => m_Kind;


    [JsonIgnore]
    public string Speaker => m_Speaker;

    [JsonIgnore]
    public string SpeakerGender => m_SpeakerGender;

    [JsonIgnore]
    public string OwnerLink => m_OwnerPath;

    [JsonIgnore]
    public string StringPath
    {
        get
        {
            if (m_FolderPath != null)
                return m_FolderPath;

            if (!m_OwnerPath.Contains('/'))
            {
                // path MUST have at least some folder, but Unreal sometimes has strings without path (added without export+sync, or old unused strings) 
                m_FolderPath = m_OwnerPath == ""
                    ? m_TextId.Namespace == ""
                        ? "No path/" + m_TextId.Key
                        : "No path/" + m_TextId.Namespace + "/" + m_TextId.Key
                    : "No path/" + m_OwnerPath;
            }
            else if (m_OwnerPath.StartsWith("/"))
            {
                m_FolderPath = m_OwnerPath.Remove(0, 1); // We don't need leading slash
            }
            else
            {
                m_FolderPath = m_OwnerPath;
            }

            return m_FolderPath;
        }
    }

    [JsonIgnore]
    public DateTimeOffset ModificationDate =>
        m_ModificationDateInTicks - FileTimeOffset > 0L
            ? DateTime.FromFileTimeUtc(m_ModificationDateInTicks - FileTimeOffset)
            : DateTimeOffset.MinValue;


    [JsonIgnore]
    public string AttachmentPath => m_Screenshot;

    [JsonIgnore]
    public IEnumerable<ILocaleData> Languages => m_Languages;

    [JsonIgnore]
    public IEnumerable<ITraitData> StringTraits => m_StringTraits.Concat(m_VirtualStringTraits);

    [JsonIgnore]
    public bool ShouldCount => true;

    [JsonIgnore]
    public string SourceFile { get; set; }

    [JsonIgnore]
    public bool HasUnsavedChanges { get; set; }

    [JsonIgnore]
    public int InternalOrder; // used to ensure string order does not change on save

    private UnrealLocaleData? GetLocale(Locale locale)
    {
        return (UnrealLocaleData?)((IStringData)this).GetLocale(locale);
    }

    public ILocaleData EnsureLocale(Locale locale)
    {
        var localeData = GetLocale(locale);
        if (localeData != null)
            return localeData;

        localeData = new UnrealLocaleData
        {
            m_Locale = locale, ModificationDate = DateTimeOffset.UtcNow
        };
        m_Languages.Add(localeData);
        return localeData;
    }

    public void AddTraitInternal(ITraitData trait)
    {
        if (trait is UnrealTraitData { IsVirtual: true } td)
        {
            m_VirtualStringTraits.Add(td);
        }
        else
        {
            m_StringTraits ??= new List<UnrealTraitData>();
            m_StringTraits.Add((UnrealTraitData)trait);
        }
    }

    public void RemoveTraitInternal(string trait)
    {
        m_StringTraits?.RemoveAll(t => t.Trait == trait);
        m_VirtualStringTraits?.RemoveAll(t => t.Trait == trait);
    }

    public ITraitData CreateTraitData(string trait, bool isVirtual=false) => new UnrealTraitData(trait){IsVirtual=isVirtual};

    public bool UpdateText(Locale locale, string text, bool updateDate = true)
    {
        //text = ApplyFixups(locale, text);

        bool modified = false;
        var localeData = GetLocale(locale);
        if (localeData == null)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            localeData = new UnrealLocaleData(locale);
            if (m_Languages.Count == 0)
                m_Source = locale;

            m_Languages.Add(localeData);
            modified = true;
        }

        if (localeData.Text != text)
        {
            localeData.Text = text;
            if (updateDate)
                localeData.ModificationDate = DateTimeOffset.UtcNow;

            modified = true;
        }

        return modified;
    }


    public void UpdateTranslation(Locale locale, string text, Locale translatedFrom, string originalText)
    {
        UpdateText(locale, text);

        var localeData = GetLocale(locale);
        if (localeData == null)
            return;

        if (locale != translatedFrom)
        {
            localeData.TranslatedFrom = translatedFrom;
            localeData.OriginalText = originalText;
            localeData.TranslationDate = DateTime.UtcNow;
        }
    }

    public void PostLoad(string absolutePath)
    {
        SourceFile = absolutePath;
        // mark with virtual trait if it is conflicted
        var nativeLocale = GetLocale(AppConfig.Instance.UnrealNativeLocale);
        if (nativeLocale != null && nativeLocale.Text != m_SourceText)
        {
            AddTraitInternal(new UnrealTraitData("Source_Mismatch"){LocaleText = m_SourceText, IsVirtual = true});
        }
    }
}

public class StringId
{
    [JsonInclude]
    public string Namespace;

    [JsonInclude]
    public string Key;

    public override string ToString()
    {
        return Namespace + ":" + Key;
    }

    public StringId(string key)
    {
        var split = key.Split(":");
        Namespace = split[0];
        Key = split[1];
    }

    [JsonConstructor]
    public StringId()
    {
    }
}