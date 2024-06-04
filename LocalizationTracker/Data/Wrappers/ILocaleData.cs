using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Localization.Shared;

namespace LocalizationTracker.Data.Wrappers;

public interface ILocaleData
{
    public Locale Locale { get; }

    public string Text  { get; }

    public DateTimeOffset ModificationDate { get; }

    public Locale? TranslatedFrom { get; }

    public DateTimeOffset? TranslationDate { get; }

    public string OriginalText { get; set; }

    public string TranslatedComment { get; set; }

    public IEnumerable<ITraitData> Traits { get; }

    public bool HasTrait(string trait)
    {
        return Traits != null && Traits.Any(t => t.Trait == trait);
    }
    
    void AddTraitInternal(ITraitData trait);
    void RemoveTraitInternal(string trait);
}