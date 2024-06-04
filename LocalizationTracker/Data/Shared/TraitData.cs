using System;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Kingmaker.Localization.Shared;

public class TraitData:ITraitData
{
    [NotNull]
    [JsonInclude]
    [JsonPropertyName("trait")]
    public readonly string Trait = "";

    [JsonInclude]
    [JsonPropertyName("trait_date")]
    public DateTimeOffset ModificationDate;

    // locale text at the time trait was added
    [NotNull]
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("locale_text")]
    public string LocaleText = "";

    [JsonConstructor]
    public TraitData([NotNull] string trait)
    {
        Trait = trait;
    }

    string ITraitData.Trait => Trait;

    DateTimeOffset ITraitData.ModificationDate
    {
        get => ModificationDate;
        set => ModificationDate=value;
    }

    string ITraitData.LocaleText
    {
        get => LocaleText;
        set => LocaleText=value;
    }
}