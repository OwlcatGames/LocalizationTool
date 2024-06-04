using common4gp;
using LocalizationTracker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Kingmaker.Localization.Shared
{
    public class Locale : IComparable<Locale>,
                          IEquatable<Locale>
    {
        public static readonly Locale Empty = new(""); // Technical, not an actual locale
        public static readonly Locale enGB = new("enGB");
        public static readonly Locale ruRU = new("ruRU");
        public static readonly Locale deDE = new("deDE");
        public static readonly Locale frFR = new("frFR");
        public static readonly Locale zhCN = new("zhCN");
        public static readonly Locale esES = new("esES");
        public static readonly Locale Sound = new("Sound");
        public static readonly Locale TranslationSource = new("TranslationSource"); // Technical, not an actual locale

        public static readonly Locale[] DefaultValues =
        {
            enGB,
            ruRU,
            deDE,
            frFR,
            zhCN,
            esES,
            Sound
        };

        public static Locale[] Values
        {
            get
            {
                return s_Values ??= CollectLocales();
            }
        }

        private static Locale[] CollectLocales()
        {
            if (AppConfig.Instance == null)
            {
                return DefaultValues.Prepend(Empty).ToArray();
            }
            else
            {
                HashSet<Locale> locales = new HashSet<Locale>();

                if (AppConfig.Instance.AddDefaultLocales)
                {
                    foreach (Locale locale in DefaultValues)
                    {
                        if (locale == Locale.Sound)
                        {
                            foreach (Locale custom in AppConfig.Instance.Locales)
                                locales.Add(custom);
                        }

                        locales.Add(locale);
                    }
                }

                foreach (Locale custom in AppConfig.Instance.Locales)
                {
                    locales.Add(new Locale(custom));
                }

                locales.Remove(Empty);
                return locales.ToArray();
            }
        }
        //private static Locale[] CollectLocales() => AppConfig.Instance == null
        //    ? DefaultValues.Prepend(Empty).ToArray()
        //    : new[] { Empty }
        //            .Concat(AppConfig.Instance.AddDefaultLocales ? DefaultValues : Enumerable.Empty<Locale>())
        //            .Concat(AppConfig.Instance.Locales.Select(l => new Locale(l)))
        //            .Where(w => w != Empty)
        //            .ToArray();

        public static Locale[] ValuesForTraitsFilter => s_Values.Append(Locale.Empty).ToArray();

        public static readonly Locale[] SourceLocaleValues = Values.Append(TranslationSource).ToArray();

        // used as "russian" and "english" defaults on diff projects
        public static Locale DefaultFrom => Values.FirstOrDefault(l => l.Code.StartsWith("ru"));
        public static Locale DefaultTo => Values.FirstOrDefault(l => l.Code.StartsWith("en"));

        private static Locale[]? s_Values;

        public readonly string Code;

        public Locale(string code)
        {
            Code = code;
        }

        public override string ToString() => Code;

        public override int GetHashCode() => Code.GetHashCode();

        public static implicit operator Locale(string localeCode) => new Locale(localeCode);

        public static implicit operator string(Locale locale) => locale.Code;

        public static implicit operator Locale(long localeCode) => DefaultValues[localeCode];

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is null)
            {
                return false;
            }

            return Equals(obj as Locale);
        }

        public int CompareTo(Locale? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            return string.Compare(Code, other.Code, StringComparison.Ordinal);
        }

        public bool Equals(Locale? other)
        {
            return other?.Code == Code;
        }

        public static bool operator ==(Locale? left, Locale? right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Locale? left, Locale? right)
        {
            return !(left == right);
        }

        public static bool operator <(Locale? left, Locale? right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Locale? left, Locale? right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Locale? left, Locale? right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Locale? left, Locale? right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }
    }
}