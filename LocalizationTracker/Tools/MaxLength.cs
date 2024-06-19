using DiffMatchPatch;
using JetBrains.Annotations;
using LocalizationTracker.Components;
using LocalizationTracker.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LocalizationTracker.Tools
{
    class MaxLength
    {
        public static InlinesWrapper MakeInlines(string text, int symbolsCount)
        {
            var result = new List<InlineTemplate>();

            //Color? filterColor = selectedColor ?? Brushes.LightBlue.Color;

            if (text.Length > symbolsCount)
            {
                var startString = text[..symbolsCount];
                var sub = text[symbolsCount..];

                result.Add(new InlineTemplate(startString));
                result.Add(MakeInline(sub));
            }
            else
            {
                result.Add(new InlineTemplate(text));
            }

            return new InlinesWrapper(result.ToArray());
        }

        private static InlineTemplate MakeInline(string text)
            => new(text)
            {
                Background = Brushes.Yellow.Color,
            };


    }
}
