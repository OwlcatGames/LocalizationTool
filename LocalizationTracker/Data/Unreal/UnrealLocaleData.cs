using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Data.Wrappers;
using LocalizationTracker.Utility;

namespace LocalizationTracker.Data.Unreal;

public class UnrealLocaleData : ILocaleData
{
    [JsonInclude]
    [JsonPropertyName("locale")]
    public Locale m_Locale;

    [JsonInclude]
    [JsonPropertyName("sourceLocale")]
    public Locale? m_TranslatedFrom;

    [JsonInclude]
    [JsonPropertyName("text")]
    public string m_Text;

    [JsonInclude]
    [JsonPropertyName("modification_date")]
    public long m_ModificationDateInTicks;

    [JsonInclude]
    [JsonPropertyName("translation_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonDefaultValue(0)]
    public long m_TranslationDateInTicks;

    [JsonInclude]
    [JsonPropertyName("original_text")]
    public string m_OriginalText;
    
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonDefaultValue("")]
    [JsonPropertyName("translation_comment")]
    public string m_Comment = "";

    [JsonInclude]
    [JsonPropertyName("traits")]
    public List<UnrealTraitData> m_Traits = new();

    [JsonIgnore]
    public Locale Locale => m_Locale;

    [JsonIgnore]
    public string Text
    {
        get => m_Text;
        set => m_Text = value;
    }

    [JsonIgnore]
    public DateTimeOffset ModificationDate
    {
        get => m_ModificationDateInTicks > 0
            ? DateTime.FromFileTimeUtc(m_ModificationDateInTicks - UnrealStringData.FileTimeOffset)
            : DateTimeOffset.MinValue;
        set => m_ModificationDateInTicks = value.UtcDateTime.ToFileTimeUtc() + UnrealStringData.FileTimeOffset;
    }

    [JsonIgnore]
    public Locale? TranslatedFrom
    {
        get => m_TranslatedFrom;
        set => m_TranslatedFrom = value;
    }

    [JsonIgnore]
    public DateTimeOffset? TranslationDate
    {
        get => m_TranslationDateInTicks > 0
            ? DateTime.FromFileTimeUtc(m_TranslationDateInTicks - UnrealStringData.FileTimeOffset)
            : null;
        set => m_TranslationDateInTicks = value?.UtcDateTime.ToFileTimeUtc() + UnrealStringData.FileTimeOffset ?? 0;
    }

    [JsonIgnore]
    public string OriginalText
    {
        get => m_OriginalText;
        set => m_OriginalText = value;
    }

    [JsonIgnore]
    public string TranslatedComment
    {
        get => m_Comment;
        set => m_Comment = value;
    }

    [JsonIgnore]
    public IEnumerable<ITraitData> Traits => m_Traits ??= new List<UnrealTraitData>();

    public UnrealLocaleData(Locale locale)
    {
        m_Locale = locale;
        m_ModificationDateInTicks = DateTime.UtcNow.ToFileTimeUtc();
    }
    public void AddTraitInternal(ITraitData trait)
    {
        m_Traits ??= new List<UnrealTraitData>();
        m_Traits.Add((UnrealTraitData)trait);
    }

    public void RemoveTraitInternal(string trait)
    {
        m_Traits?.RemoveAll(t => t.Trait == trait);
    }
    [JsonConstructor]
    public UnrealLocaleData()
    {
    }
}