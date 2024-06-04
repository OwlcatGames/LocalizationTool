using Cathei.BakingSheet;

namespace LocalizationTracker.Tools.GlossaryTools;

public class GlossarySheetContainer : SheetContainerBase
{
    public GlossarySheetContainer(Microsoft.Extensions.Logging.ILogger logger) : base(logger) { }
    public GlossarySheet Main { get; set; }
}