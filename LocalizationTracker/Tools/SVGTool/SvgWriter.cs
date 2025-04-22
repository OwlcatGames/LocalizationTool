using LocalizationTracker.OpenOffice;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Xml;
using static LocalizationTracker.Tools.SVGTool.GenerateSVG;

namespace LocalizationTracker.Tools.SVGTool
{
    public class SvgWriter
    {
        private const float Scale = 1.5f;
        private const int YShift = 30;
        private const string Namespace = "http://www.w3.org/2000/svg";
        public HashSet<NodeProperties> drownNodes = new();

        [NotNull]
        private readonly XmlDocument m_XML;

        [NotNull]
        private readonly XmlElement m_Root;

        private float m_Height = 800;
        private float m_Width = 1000;

        private readonly CultureInfo m_OldCulture;
        private readonly string m_NewLine;
        private readonly string m_IncorrectNewLine;

        public SvgWriter()
        {
            m_OldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            m_NewLine = TextWriter.Null.NewLine;
            m_XML = new XmlDocument();
            m_Root = m_XML.CreateElement("svg", Namespace);
            m_XML.AppendChild(m_Root);
        }

        public void DrawNode(NodeProperties node, float offsetX, float offsetY, Locale locale)
        {
            drownNodes.Add(node);
            var color = GetWindowColor(node.Node.Kind);

            var rectPosition = new PointF(offsetX, offsetY);

            var rect = new RectangleF(rectPosition.X, rectPosition.Y, NodeWidth, node.StrHeight);

            m_Height = Math.Max(rect.Y + node.OccupatedHeight, m_Height);
            m_Width = Math.Max(rect.X + NodeWidth, m_Width);

            //draw nodes
            var nameRect = rect;
            nameRect.Height = YShift;
            rect.Height -= YShift;
            rect.Y += YShift;

            string id = node.Node.Name;
            string href = "";

            DrawText(nameRect, color, id, 4, id, href);

            if (node.Node.Kind == "check")
            {
                var typeRect = rect;
                typeRect.Height = YShift;
                rect.Height -= YShift;
                rect.Y += YShift;
                DrawText(typeRect, color, node.Node.Type, 4);
            }

            if (node.Str != null && !string.IsNullOrEmpty(node.Str.Speaker))
            {
                var speakerRect = rect;
                speakerRect.Height = YShift;
                rect.Height -= YShift;
                rect.Y += YShift;
                DrawText(speakerRect, color, $"speaker: {node.Str.Speaker}", 4);
            }

            if (node.Str != null && !string.IsNullOrEmpty(node.Str.Data.GetText(locale)))
            {
                var textRect = rect;
                textRect.Height = node.StrHeight;
                rect.Height -= node.StrHeight;
                rect.Y += node.StrHeight;
                DrawText(textRect, color, node.Str.Data.GetText(locale), 5);

            }

            if (!string.IsNullOrEmpty(node.Comment))
            {
                var commentRect = rect;
                commentRect.Height = node.CommentHeight;
                rect.Y += node.CommentHeight;
                DrawText(commentRect, color, node.Comment, 4);
            }

            if (node.Str != null && !string.IsNullOrEmpty(node.Str.Data.GetText(locale)) && node.Node.Shared == true)
            {
                var pathRect = rect;
                pathRect.Height = node.PathHeight;
                rect.Height -= node.PathHeight;
                rect.Y += node.PathHeight;

                DrawText(pathRect, color, "File: ", 4, "", "", false, true);
                pathRect.Y += YShift;
                DrawText(pathRect, color, node.Str.PathRelativeToStringsFolder, 4);

                var idsRect = rect;
                idsRect.Height = node.DialogsIdsLineHeight;
                rect.Height -= node.DialogsIdsLineHeight;
                rect.Y += node.DialogsIdsLineHeight;

                DrawText(idsRect, color, "Shared to: ", 4, "", "", false, true);
                idsRect.Y += YShift;
                DrawText(idsRect, color, node.DialogsIds, 4);
            }

            var childNodes = GetReferencedNodes(node);
            var nodePos = nodePosition.FirstOrDefault(w => w.Key.Node.Id == node.Node.Id);


            foreach (var child in childNodes)
                if (drownNodes.Contains(child.Key))
                    DrawConnection(nodePos.Key, nodePos.Value, child.Key, child.Value, "lightgray");
                else
                    DrawConnection(nodePos.Key, nodePos.Value, child.Key, child.Value);


        }

        private List<KeyValuePair<NodeProperties, Position>> GetReferencedNodes(NodeProperties node)
        {
            var childNodes = new List<KeyValuePair<NodeProperties, Position>>();

            foreach (var childId in node.Node.Children)
            {
                var childEntry = nodePosition.FirstOrDefault(w => w.Key.Node.Id == childId);

                if (!childEntry.Equals(default(KeyValuePair<NodeProperties, Position>)))
                {
                    childNodes.Add(childEntry);
                }
            }

            return childNodes;
        }

        private void DrawText(
            RectangleF rect, System.Windows.Media.Color color, string content, int fontSize, string id = "", string href = "",
            bool keepLines = false, bool bold = false)
        {
            var box = m_XML.CreateElement("rect", Namespace);

            m_Root.AppendChild(box);
            box.SetAttribute("x", rect.X + "");
            box.SetAttribute("y", rect.Y + "");
            box.SetAttribute("width", rect.Width + "");
            box.SetAttribute("height", rect.Height + "");

            int r = (int)(color.R);
            int g = (int)(color.G);
            int b = (int)(color.B);
            box.SetAttribute("fill", "none");
            box.SetAttribute("stroke", string.Format("rgb({0}, {1}, {2})", r, g, b));
            box.SetAttribute("stroke-width", "5");

            var textRect = rect;
            textRect.X += 5;
            textRect.Y += 5;
            textRect.Height -= 10;
            textRect.Width -= 10;


            var text = m_XML.CreateElement("foreignObject", Namespace);
            m_Root.AppendChild(text);
            text.SetAttribute("x", textRect.X + "");
            text.SetAttribute("y", textRect.Y + "");
            text.SetAttribute("width", textRect.Width + "");
            text.SetAttribute("height", textRect.Height + "");
            text.SetAttribute("requiredFeatures", "http://www.w3.org/TR/SVG11/feature#Extensibility");

            if (bold)
            {
                textRect.X += 5;
                textRect.Y += 5;
                text.SetAttribute("font-weight", "bold");
            }

            XmlElement el;
            if (href == "")
            {
                el = m_XML.CreateElement("font", "http://www.w3.org/1999/xhtml");
                el.SetAttribute("size", fontSize.ToString());
                el.SetAttribute("style", $"width: {textRect.Width}px; word-wrap: break-word;");
            }
            else
            {
                el = m_XML.CreateElement("a", "http://www.w3.org/1999/xhtml");
                el.SetAttribute("href", href);
            }

            if (id != "")
            {
                el.SetAttribute("id", id);
            }

            text.AppendChild(el);


            if (!keepLines)
            {
                string fixedContent = content
                    .Replace("\n", m_NewLine);

                el.InnerText = $"{m_NewLine}{fixedContent}{m_NewLine}";

            }
            else
            {
                string fixedContent = content
                    .Replace("\n", m_NewLine)
                    .Replace(m_NewLine, $"<br xmlns=\"http://www.w3.org/1999/xhtml\"/>{m_NewLine}")
                    .Replace("    ", "&#160;&#160;&#160;&#160;  ");

                el.InnerXml = $"{m_NewLine}{fixedContent}{m_NewLine}";
            }

        }

        private void DrawConnection(NodeProperties fromNode, Position fromPosition, NodeProperties toNode, Position toPosition, string color = "black")
        {
            //// Проверяем, находится ли конечная позиция справа от начальной
            //if (toPosition.X < fromPosition.X + NodeWidth)
            //    return; // Если нет, ничего не рисуем

            Vector2 s = new Vector2(fromPosition.X + NodeWidth, fromPosition.Y + fromNode.OccupatedHeight / 2);
            Vector2 e = new Vector2(toPosition.X, toPosition.Y + toNode.OccupatedHeight / 2);

            Vector2 tg1 = s + 0.5f * new Vector2(Math.Abs(e.X - s.X), 0);
            Vector2 tg2 = e - 0.5f * new Vector2(Math.Abs(e.X - s.X), 0);

            var path = m_XML.CreateElement("path", Namespace);
            m_Root.AppendChild(path);

            path.SetAttribute("d",
                string.Format("M {0} {1} C {2} {3} {4} {5} {6} {7}", s.X, s.Y, tg1.X, tg1.Y, tg2.X, tg2.Y, e.X, e.Y));
            path.SetAttribute("fill", "none");
            path.SetAttribute("stroke", color);
            path.SetAttribute("stroke-width", "2");
        }

        public void Save(string fileName, string fileSource)
        {
            try
            {
                string rootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Svg");
                string relativePath = Path.GetDirectoryName(fileSource);

                if (relativePath == null)
                {
                    Console.WriteLine("Ошибка: не удалось получить путь к исходному файлу.");
                    return;
                }

                string subDirectoryPath = Path.GetRelativePath(AppConfig.Instance.DialogsFolder, relativePath);

                string directoryPath = Path.Combine(rootDirectory, subDirectoryPath);
                string fullPath = Path.Combine(directoryPath, fileName);

                Directory.CreateDirectory(directoryPath);

                m_Root.SetAttribute("width", m_Width.ToString());
                m_Root.SetAttribute("height", m_Height.ToString());

                using (var writer = new XmlTextWriter(fullPath, Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    m_XML.WriteTo(writer);
                    writer.Flush();
                }

                Console.WriteLine($"Файл успешно сохранен: {fullPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении файла: {ex.Message}");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = m_OldCulture;
            }
        }



        private System.Windows.Media.Color GetWindowColor(string kind)
        {
            switch (kind)
            {
                case "root":
                    return Colors.LightGray;
                case "answerlist":
                    return Colors.GreenYellow;
                case "answer":
                    return Colors.Yellow;
                case "cue":
                    return Colors.LightGreen;
                case "check":
                    return Colors.Red;
                default:
                    return Colors.LightGray;
            }
        }
    }
}
