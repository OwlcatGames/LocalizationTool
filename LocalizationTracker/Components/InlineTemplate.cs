using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using static LocalizationTracker.Components.InlineTemplate;

namespace LocalizationTracker.Components;

public record struct InlineTemplate(
    string Text, 
    Color? Foreground = null, 
    Color? Background = null, 
    bool StrikeThrough = false, 
    bool Underline = false,
    FontWeight? FontWeight = null,
    InlineType InlineType = InlineType.Default)
{
    public InlineTemplate(string text, Color? foreground, Color? background, bool strikeThrough, FontWeight? fontWeight)
        :this(text, foreground, background, strikeThrough, false, fontWeight)
    {
    }

    public Inline MakeInline()
    {
        var run = new Run(Text);
        if (Foreground != null)
            run.Foreground = new SolidColorBrush(Foreground.Value);
        if (Background != null)
            run.Background = new SolidColorBrush(Background.Value);
        if (StrikeThrough)
            run.TextDecorations.Add(TextDecorations.Strikethrough);
        if (FontWeight != null)
            run.FontWeight = FontWeight.Value;
        if (Underline)
            run.TextDecorations.Add(GetUnderlineDecoration());
        return run;
    }
    
    private TextDecoration GetUnderlineDecoration()
    {
        TextDecoration myUnderline = new();
        Pen myPen = new();
        myPen.Brush = new SolidColorBrush(Colors.Blue);
        myPen.Brush.Opacity = 1;
        myPen.Thickness = 2;
        myUnderline.Pen = myPen;
        myUnderline.PenThicknessUnit = TextDecorationUnit.FontRecommended;
        return myUnderline;
    }
}
public enum InlineType
{
    Default,
    DiffDelete,
    DiffInsert,
    SpellCheckError,
    GlossaryTerm,
    MaxLength
}

public enum InlineCollectionType
{
    Default,
    TagsMismatch,
    DiffTrait,
    DiffSource,
    DiffSourceNoTags,
    SpellCheck,
    Filter,
    Glossary,
    MaxLength
}