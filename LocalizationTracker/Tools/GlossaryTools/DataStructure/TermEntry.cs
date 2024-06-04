namespace LocalizationTracker.Tools.GlossaryTools;

/// <summary>
/// Term entry represents piece of text in LocaleEntry of StringEntry
/// </summary>
public class TermEntry
{
    public string TermId;
    public string StringKey;
    public string Comment;
    public int StartIndex;
    public int EndIndex;
    public bool CaseError;
    public int Length => EndIndex - StartIndex + 1;
}