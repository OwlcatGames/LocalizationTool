using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;

namespace LocalizationTracker.Components
{
    public class InlinesWrapper
    {
        public IEnumerable<Inline> Inlines => InlineTemplates.Select(v => v.MakeInline());

        public readonly InlineTemplate[] InlineTemplates = Array.Empty<InlineTemplate>();

        public bool HasAny => InlineTemplates.Length > 0;

        public InlinesWrapper()
        {
        }

        public InlinesWrapper(string text)
        {
            if (!string.IsNullOrEmpty(text))
                InlineTemplates = new[] { new InlineTemplate(text) };
        }

        public InlinesWrapper(InlineTemplate[] inlines)
        {
            InlineTemplates = inlines;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var inline in InlineTemplates)
            {
                sb.Append(inline.Text);
            }

            return sb.ToString();
        }
    }
}