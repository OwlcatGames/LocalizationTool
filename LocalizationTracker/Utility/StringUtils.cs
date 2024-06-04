using LocalizationTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace LocalizationTracker.Utility
{
    public static partial class StringUtils
    {
        private static TagsList.Entry Convert(Match match)
        {
            var isClosing = match.Groups["Closing"].Captures.Any();
            var tag = match.Groups["TagName"].Value;
            return new TagsList.Entry()
            {
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length,
                Tag = tag.ToLowerInvariant(),
                FullText = match.Value,
                IsClosing = isClosing
            };
        }

        public static TagsList.Entry[] ExtractTags(string str)
        {
            var tags = SplitWordsTags().Matches(str).Where(v => v.Groups["Tag"].Captures.Any()).Select(Convert).ToArray();
            return tags;
        }

        [GeneratedRegex(@"
                    (
                        (?<Word>(\w|(?<=\w)['-](?=\w))+)
                        |
                        (?<Nonword>[^\w{}]+)
                        |
                        (?<Tag>{((?<TagName>[^/}|][^|}]*)|(?<Closing>/)(?<TagName>[^|}]*))(\|(?<Subtag>[^|}]*))*?})
                    )
        ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture)]
        public static partial Regex SplitWordsTags();

        public static string RemoveTagsExcept(string str, IEnumerable<string> tagsToRetain)
        {
            var matches = SplitWordsTags().Matches(str);

            var words = matches.Where(v => 
                !v.Groups["Tag"].Captures.Any() || 
                v.Groups["TagName"].Captures.All(tagName => tagsToRetain.Contains(tagName.Value))).Select(v => v.Value);
            return string.Join("", words);
        }
        public static string RemoveTags(string str, IEnumerable<string> tagsToRemove)
        {
            var matches = SplitWordsTags().Matches(str);

            var words = matches.Where(v =>
                !v.Groups["Tag"].Captures.Any() ||
                v.Groups["TagName"].Captures.All(tagName => !tagsToRemove.Contains(tagName.Value))).Select(v => v.Value);
            return string.Join("", words);
        }

        public static int CountTotalWords(string str)
        {
            var matches = SplitWordsTags().Matches(str);

            var words = matches.SelectMany(v => v.Groups["Word"].Captures).Count();
            return words;
        }


        [GeneratedRegex(@"
            {n(?:\|(?<Subtag>[^\|}]+))*?}
                    (?:
                        (?<Word>\w+)
                        |
                        (?<Nonword>[^\w{}]+)
                        |
                        (?<nonntag>
                        {(?<TagName>[^n}]|[^|}]{2,})(\|(?<Subtag>[^|}]*))*?}|
                        {/(?<TagName>[^n}]|[^|}]{2,})(\|(?<Subtag>[^|}]*))*?}|{}
                        )
                    )*?
            ({/n(?:\|(?<Subtag>[^\|}]+))*?}|$|(?={n(?:\|(?<Subtag>[^\|}]+))*?}))
            ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture)]
        public static partial Regex NonVocalWordMatcher();

        public static int CountNonVocalWords(string str)
        {
            var matches = NonVocalWordMatcher().Matches(str);

            var words = matches.SelectMany(v => v.Groups["Word"].Captures).Count();
            return words;
        }

        private static string ToLinePattern(string line)
            => string.Join(".*", line.Split()
                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                    .Select(Regex.Escape));

        public static string ToPattern(string filter)
        {
            var lines = filter.Replace("\\", "/").Split(Environment.NewLine).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
            for (int i = 0; i < lines.Count(); i++)
            {
                int name = lines[i].IndexOf("Strings/");
                if (name >= 0)
                {
                    lines[i] = lines[i].Substring(name + "Strings/".Length);
                }
            }

            if (lines.Skip(1).Any())
                return "(" + string.Join("|", lines.Select(ToLinePattern)) + ")";
            else if (lines.Any())
                return ToLinePattern(lines.First());
            else
                return string.Empty;
        }

        public static bool MatchesFilter(string text, string filter, bool ignoreCase)
            => Regex.Match(text, ToPattern(filter), ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None).Success;

        public static bool MatchesPattern(string text, string pattern, bool ignoreCase)
            => Regex.Match(text, pattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None).Success;
    }
}