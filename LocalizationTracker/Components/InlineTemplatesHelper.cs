using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalizationTracker.Components;

public static class InlineTemplatesHelper
{
    public static InlinesWrapper MergeWith(this InlinesWrapper first, InlinesWrapper second) => 
        new InlinesWrapper(Merge(first.InlineTemplates, second.InlineTemplates));
    
    public static InlineTemplate[] Merge(InlineTemplate[] left, InlineTemplate[] right)
    {
        var leftLength = left.Sum(it => it.Text.Length);
        var rightLength = right.Sum(it => it.Text.Length);
        if (leftLength != rightLength) return left;

        IList<InlineTemplate> current = new List<InlineTemplate>(); 
            
        var li = 0; // left
        var ri = 0; // right
        var ci = 0; // current

        var lp = 0;
        var rp = 0;
        var cp = 0;

        while (lp < leftLength)
        {
            var le = lp + left[li].Text.Length;
            var re = rp + right[ri].Text.Length;

            InlineTemplate newInline = new InlineTemplate("", 
                right[ri].Foreground ?? left[li].Foreground ,
                right[ri].Background ?? left[li].Background, 
                right[ri].StrikeThrough ? right[ri].StrikeThrough : left[li].StrikeThrough, 
                right[ri].Underline ? right[ri].Underline : left[li].Underline, 
                right[ri].FontWeight ?? left[li].FontWeight,
                right[ri].InlineType != InlineType.Default ? right[ri].InlineType : left[li].InlineType
                );
                
            if (lp < rp) // left start first
            {
                if (le < re)
                {
                    newInline.Text = right[ri].Text.Substring(0, le - rp);
                    lp = le;
                    li++;
                }
                else
                {
                    newInline.Text = right[ri].Text;
                    rp = re;
                    ri++;
                    if (le == re)
                    {
                        lp = le;
                        li++;
                    }
                }
            }
            else
            {
                if (le < re)
                {
                    newInline.Text = left[li].Text;
                    lp = le;
                    li++;
                }
                else
                {
                    newInline.Text = left[li].Text.Substring(0, re - lp);
                    rp = re;
                    ri++;
                    if (le == re)
                    {
                        lp = le;
                        li++;
                    }
                }
            }


            current.Add(newInline);
        }

        return current.ToArray();
    }
}