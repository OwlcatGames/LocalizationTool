using System;

namespace Kingmaker.Localization.Shared;

public interface ITraitData
{
    string Trait { get; }

    DateTimeOffset ModificationDate { get; set; }

    // locale text at the time trait was added
    string LocaleText { get; set; }
}