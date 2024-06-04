using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LocalizationTracker.Components;
using LocalizationTracker.Utility;

namespace LocalizationTracker.Data
{
    public class TagsList
    {
        public class Entry
        {
            public int StartIndex;
            public int EndIndex;
            public string Tag;
            public string FullText;
            public bool IsClosing;
            public bool IsUnmatched;
            public bool WrongOpenClose;
        }

        public readonly Entry[] Tags;

        public bool? HasUnmatchedTags;

        private TagsList(Entry[] tags)
        {
            Tags = tags;
        }

        public static TagsList Parse(string str)
        {
            var tags = StringUtils.ExtractTags(str);

            // count opening/closing
            Stack<Entry> stack = new();
            foreach (var tag in tags)
            {
                if (AppConfig.Instance.NeedClosingTags.Contains(tag.Tag))
                {
                    if (!tag.IsClosing)
                    {
                        tag.WrongOpenClose = true;
                        stack.Push(tag);
                    }
                    else
                    {
                        tag.WrongOpenClose = stack.Count == 0 || stack.Peek().Tag != tag.Tag;
                        if (!tag.WrongOpenClose)
                        {
                            stack.Pop().WrongOpenClose = false;
                        }
                    }
                }
                else
                {
                    if (tag.IsClosing)
                        tag.WrongOpenClose = true;
                }
            }
            return new TagsList(tags);
        }

        public InlinesWrapper MakeInlines(string text)
        {
            InlineTemplate GetRunForTag(Entry tag)
            {
                InlineTemplate run = new InlineTemplate(text.Substring(tag.StartIndex, tag.EndIndex - tag.StartIndex));
                run.Foreground = tag.IsUnmatched ? Brushes.Red.Color : Brushes.ForestGreen.Color;
                if (tag.WrongOpenClose)
                {
                    run.Background = Brushes.Yellow.Color;
                }

                if (tag.IsUnmatched)
                {
                    run.FontWeight = FontWeights.Bold;
                }
                return run;
            }

            if (Tags.Length == 0)
            {
                return new InlinesWrapper(text); // no tags at all
            }

            var result = new List<InlineTemplate>();

            int idx = 0;
            // tags list would be sorted by tag index because it was parsed that way
            foreach (var tag in Tags)
            {
                // add plain text before the tag
                if (idx < tag.StartIndex)
                {
                    result.Add(new InlineTemplate(text.Substring(idx, tag.StartIndex-idx)));
                }
                // add tag itself
                result.Add(GetRunForTag(tag));
                idx = tag.EndIndex;
            }
            // add span after last tag
            if (idx < text.Length)
            {
                result.Add(new InlineTemplate(text.Substring(idx)));
            }

            return new InlinesWrapper(result.ToArray());
        }

        public static bool Compare(TagsList l1, TagsList l2)
        {
            var hs1 = new HashSet<TagsList.Entry>(l1.Tags);
            var hs2 = new HashSet<TagsList.Entry>(l2.Tags);

            foreach (var tag in l1.Tags)
            {
                if (AppConfig.Instance.IgnoreMismatchedTags.Contains(tag.Tag))
                {
                    hs1.Remove(tag);
                    continue;
                }

                var match = hs2.FirstOrDefault(t => t.FullText == tag.FullText);
                if (match != null)
                {
                    hs1.Remove(tag);
                    hs2.Remove(match);
                }
            }

            hs2.RemoveWhere(t => AppConfig.Instance.IgnoreMismatchedTags.Contains(t.Tag));

            foreach (var tag in l1.Tags)
            {
                tag.IsUnmatched = hs1.Contains(tag);
            }
            foreach (var tag in l2.Tags)
            {
                tag.IsUnmatched = hs2.Contains(tag);
            }

            l1.HasUnmatchedTags = l2.HasUnmatchedTags = hs1.Count > 0 || hs2.Count > 0;

            return !l1.HasUnmatchedTags.Value;
        }
    }
}