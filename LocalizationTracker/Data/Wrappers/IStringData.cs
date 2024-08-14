using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kingmaker.Localization.Shared;
using LocalizationTracker.Data.Unreal;
using static LocalizationTracker.Data.Unreal.UnrealStringData;

namespace LocalizationTracker.Data.Wrappers;

public interface IStringData
{
    public Locale Source { get; }

    public string Key { get; }

    public string Comment { get; set; }

    public StringKind Kind { get; }

    public string Speaker { get; }

    public string SpeakerGender { get; }

    public ParentId ParentId { get; }

    public string OwnerLink { get; }
    public string StringPath { get; }
    public DateTimeOffset ModificationDate { get; }

    public string AttachmentPath { get; }


    public IEnumerable<ILocaleData> Languages { get; }

    public IEnumerable<ITraitData> StringTraits { get; }
    bool ShouldCount { get; }

    bool UpdateText(Locale locale, string text, bool updateDate = true);
    void UpdateTranslation(Locale locale, string text, Locale translatedFrom, string originalText);

    void AddTraitInternal(ITraitData trait);
    void RemoveTraitInternal(string trait);

    public void AddTrait(Locale locale, string trait)
    {
        var localeData = GetLocale(locale);
        if (localeData == null)
            return;

        var traitData = localeData.Traits.FirstOrDefault(t => t.Trait == trait);
        if (traitData == null)
        {
            traitData = CreateTraitData(trait);
            localeData.AddTraitInternal(traitData);
        }

        traitData.LocaleText = localeData.Text;
        traitData.ModificationDate = DateTimeOffset.UtcNow;
    }

    ITraitData CreateTraitData(string trait, bool isVirtual=false);

    public void RemoveTrait(Locale locale, string trait)
    {
        var localeData = GetLocale(locale);
        localeData?.RemoveTraitInternal(trait);
    }

    public void AddStringTrait(string trait, bool isVirtual = false)
    {
        var traitData = StringTraits.FirstOrDefault(t => t.Trait == trait);
        if (traitData == null)
        {
            traitData = CreateTraitData(trait, isVirtual);
            AddTraitInternal(traitData);
        }

        traitData.ModificationDate = DateTimeOffset.UtcNow;
    }

    public void RemoveStringTrait(string trait)
    {
        RemoveTraitInternal(trait);
    }

    public bool HasStringTrait(string trait)
    {
        return StringTraits != null && StringTraits.Any(t => t.Trait == trait);
    }
    public string[] GetStringTraits()
    {
        if (StringTraits == null)
            return new string[] { };

        return StringTraits
            .Select(t => t.Trait)
            .ToArray();
    }

    public ILocaleData? GetLocale(Locale locale) => Languages.FirstOrDefault(s => s.Locale == locale);

    public ILocaleData EnsureLocale(Locale locale);

    public ITraitData? GetTraitData(Locale locale, string trait)
    {
        var localeData = GetLocale(locale);
        return localeData?.Traits?.FirstOrDefault(td => td.Trait == trait);
    }

    public ITraitData? GetStringTraitData(string trait)
    {
        return StringTraits?.FirstOrDefault(td => td.Trait == trait);
    }

    public string GetText(Locale locale)
    {
        var localeData = GetLocale(locale);
        return localeData?.Text ?? "";
    }

    public string[] GetTraits(Locale locale)
    {
        var localeData = GetLocale(locale);
        if (localeData == null)
            return new string[] { };

        if (localeData.Traits == null)
            return new string[] { };

        return localeData.Traits
            .Select(t => t.Trait)
            .ToArray();
    }

}