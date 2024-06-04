using System;
using System.Text.Json.Serialization;
using Kingmaker.Localization.Shared;

namespace LocalizationTracker.Data.Unreal;

public class UnrealTraitData:ITraitData
{
    [JsonInclude]
    [JsonPropertyName("trait")]
    public readonly string Trait = "";

    [JsonInclude]
    [JsonPropertyName("trait_date")]
    public long m_ModificationDateInTicks;

    // locale text at the time trait was added
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("locale_text")]
    public string LocaleText = "";

    [JsonConstructor]
    public UnrealTraitData(string trait)
    {
        Trait = trait;
    }

    string ITraitData.Trait => Trait;

    [JsonIgnore]
    public DateTimeOffset ModificationDate
    {
        get => m_ModificationDateInTicks > 0
            ? DateTime.FromFileTimeUtc(m_ModificationDateInTicks - UnrealStringData.FileTimeOffset)
            : DateTimeOffset.MinValue;
        set => m_ModificationDateInTicks = value.UtcDateTime.ToFileTimeUtc() + UnrealStringData.FileTimeOffset;
    }

    string ITraitData.LocaleText
    {
        get => LocaleText;
        set => LocaleText=value;
    }
}