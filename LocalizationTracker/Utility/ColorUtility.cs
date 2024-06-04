using System.Windows.Media;

namespace LocalizationTracker
{
    public static class ColorUtility
    {
        public static Color Red => new Color()
        {
            R = 255,
            G = 153,
            B = 153,
            A = 255
        };

        public static Color Green => new Color
        {
            R = 215,
            G = 227,
            B = 188,
            A = 255
        };

        public static Color ExportDiffGreen => new Color
        {
            R = 0,
            G = 128,
            B = 0,
            A = 255
        };

        public static Color ExportDiffRed => new Color()
        {
            R = 255,
            G = 0,
            B = 0,
            A = 255
        };

        public static Color ExportDiffYellow => new Color
        {
            R = 255,
            G = 255,
            B = 0,
            A = 255
        };

        public static Color ContextYellow => new Color
        {
            R = 255,
            G = 255,
            B = 204,
            A = 255
        };

        public static DocumentFormat.OpenXml.Spreadsheet.Color MediaColorToOXMLColor(Color color)
            => new DocumentFormat.OpenXml.Spreadsheet.Color() { Rgb = MediaColorToHEX(color) };

        public static string MediaColorToHEX(Color color)
            => string.Format("{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
    }
}
