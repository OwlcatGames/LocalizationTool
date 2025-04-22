using Cathei.BakingSheet;
using static LocalizationTracker.Tools.GlossaryTools.GlossarySheet;

namespace LocalizationTracker.Tools.GlossaryTools;

public class GlossarySheetContainer : SheetContainerBase
{
    public GlossarySheetContainer(Microsoft.Extensions.Logging.ILogger logger) : base(logger) { }
    public GlossarySheet Main { get; set; }

}

public class AmberGlossarySheetContainer : SheetContainerBase
{
    public AmberGlossarySheetContainer(Microsoft.Extensions.Logging.ILogger logger) : base(logger) { }
    public AmberGlossarySheet Main { get; set; }
}