using LocalizationTracker.Data;
using System.Collections.Generic;
using LocalizationTracker.Logic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Text;


namespace LocalizationTracker.Tools.SVGTool
{

    public class NodeProperties
    {
        public DialogsData.Node Node { get; set; }
        public StringEntry? Str { get; set; }
        public float StrHeight { get; set; }
        public float CommentHeight { get; set; }
        public float PathHeight { get; set; }
        public string DialogsIds { get; set; }
        public float DialogsIdsLineHeight { get; set; }
        public float OccupatedHeight { get; set; }
        public int CurrentDepth { get; set; }
        public string Comment { get; set; }
    }

    public class Position
    {
        public float X { get; set; }
        public float Y { get; set; }

    }

    public static class GenerateSVG
    {
        private const float Scale = 1.5f;
        public const float NodeWidth = 200 * Scale;
        public const float NodeNameHeight = 30;
        public const float NodeSpeakerHeight = 30;
        public const float SpeakerHeight = 30; 
        public const float HeaderHeight = 30;

        public const int NodeTextMaxLineCount = 25;
        public const float NodeTextLineHeight = 34;
        public const int NodeCommentMaxLineCount = 30;
        public const float NodeCommentLineHeight = 28;
        public const int PathMaxLineCount = 40;
        public const float PathLineHeight = 35;



        public static List<StringEntry> allDialogsStrings = new List<StringEntry>();
        public static List<NodeProperties> CalculatedNodes = new List<NodeProperties>();
        public static Dictionary<NodeProperties, Position> nodePosition = new Dictionary<NodeProperties, Position>();

        public static HashSet<NodeProperties> visited = new HashSet<NodeProperties>();
        private static Dictionary<int, float> nodeDepth = new Dictionary<int, float>();

        public static async Task FindAllDialogues(string dialogName = "")
        {
            Locale[] localeList = Locale.Values;
            List<DialogsData> dialogsForSvg = StringManager.AllDialogs;

            if (dialogName != "")
                dialogsForSvg = dialogsForSvg.Where(w => w.Name == dialogName).ToList();

            foreach (var dialog in dialogsForSvg)
            {
                foreach (var locale in localeList)
                {
                    Calculate(dialog, locale);
                }
            }
        }

        public static void Calculate(DialogsData dialog, Locale locale)
        {
            var svgWriter = new SvgWriter();
            CalculatedNodes.Clear();
            float offsetX = 0;
            var dialogsStrings = StringManager.AllStrings.Where(d => d.DialogsDataList != null && d.DialogsDataList.Any(a => a.Name == dialog.Name)).ToList();

            foreach (var node in dialog.Nodes)
            {
                StringEntry? str = null;
                if (CalculatedNodes.Any(a => a.Node.Name == node.Name))
                    continue;

                if (node.Shared == true && AppConfig.Instance.Engine == StringManager.EngineType.Unreal)
                    str = dialogsStrings.Where(w => node.Text.Key != null && w.Key == $"{node.Text.Namespace}:{node.Text.Key}").FirstOrDefault();
                else if (node.Text != null && !string.IsNullOrEmpty(node.Text.Key))
                    str = dialogsStrings.Where(w => node.Text.Key != null && w.Key.Replace(":", "") == node.Text.Key).FirstOrDefault();


                CalculatedNodes.Add(CalculateNodeProperties(node, str, locale));
            }

            visited.Clear();
            nodeDepth.Clear();
            nodePosition.Clear();

            CalculatePositionRecursive(CalculatedNodes.First(n => n.Node.Kind == "root"));

            foreach (var prop in nodePosition)
            {
                svgWriter.DrawNode(prop.Key, prop.Value.X, prop.Value.Y, locale);
            }

            svgWriter.Save($"{Path.GetFileNameWithoutExtension(dialog.FileSource)}_{locale}.svg", dialog.FileSource);


        }

        public static NodeProperties CalculateNodeProperties(DialogsData.Node node, StringEntry? str, Locale locale)
        {
            int commentLineCount = 0;
            int textLineCount = 0;
            int pathLineCount = 0;
            string comment = string.Empty;
            string dialogsIds = string.Empty;
            int dialogsIdsLineCount = 0;

            if (str != null)
            {
                comment = SelectComment(node, str, locale);
                textLineCount = GetLineCount(str.Data.GetText(locale), NodeTextMaxLineCount) + 1;
                pathLineCount = GetLineCount($"File: {str.PathRelativeToStringsFolder}", PathMaxLineCount);

                if (node.Shared == true)
                {
                    StringBuilder stringBuilder = new StringBuilder();

                    foreach (var dialog in str.DialogsDataList)
                    {
                        var nodeKey = dialog.Nodes.FirstOrDefault(w => w.Text != null && $"{w.Text.Namespace}:{w.Text.Key}" == str.Key);
                        if (nodeKey != null)
                            stringBuilder.AppendLine(nodeKey.Id);
                    }

                    dialogsIds = stringBuilder.ToString();
                    dialogsIdsLineCount = GetLineCount($"Shared to: {dialogsIds}", PathMaxLineCount);
                }
            }
            else
            {
                comment = node.Comment;
            }

            if (!string.IsNullOrEmpty(comment))
                commentLineCount = GetLineCount(comment, NodeCommentMaxLineCount);

            var textHeight = textLineCount * NodeTextLineHeight;
            var commentHeight = commentLineCount * NodeCommentLineHeight;
            var pathHeight = pathLineCount * PathLineHeight;
            var dialogsIdsLineHeight = dialogsIdsLineCount * PathLineHeight;


            var totalHeight = HeaderHeight;

            if (!string.IsNullOrEmpty(comment))
                totalHeight += commentHeight;
            if (str != null && !string.IsNullOrEmpty(str.Speaker))
                totalHeight += SpeakerHeight;
            if (str != null && !string.IsNullOrEmpty(str.Data.GetText(locale)))
            {
                totalHeight += textHeight;

                if (node.Shared == true)
                {
                    totalHeight += pathHeight;
                    totalHeight += dialogsIdsLineHeight;
                }
            }

            if (node.Kind == "root")
                return new NodeProperties() { Node = node, Str = str, StrHeight = textHeight, OccupatedHeight = totalHeight, CommentHeight = commentHeight, PathHeight = 0, Comment = comment };

            return new NodeProperties() { Node = node, Str = str, StrHeight = textHeight, OccupatedHeight = totalHeight, CommentHeight = commentHeight, PathHeight = pathHeight, Comment = comment, DialogsIds = dialogsIds, DialogsIdsLineHeight = dialogsIdsLineHeight };
        }

        private static string SelectComment(DialogsData.Node node, StringEntry str, Locale locale)
        {
            var comment = str.Data.GetComment(locale);

            if (string.IsNullOrEmpty(comment))
                comment = node.Comment;

            return comment;
        }

        private static int GetLineCount(string text, int lineLength)
        {
            if (text == null)
                return 0;

            int rowCount = 1;
            int lineCounter = 0;
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\n')
                {
                    rowCount++;
                    lineCounter = 0;
                    continue;
                }
                lineCounter++;
                if (lineCounter > lineLength)
                {
                    rowCount++;
                    lineCounter = 0;
                    continue;
                }
            }
            return rowCount;
        }

        private static void CalculatePositionRecursive(NodeProperties node, NodeProperties? parentNode = null, int depth = 0)
        {
            if (!visited.Add(node))
                return;

            float yOffset = 0;
            if (node.Node.Kind == "root")
            {
                nodePosition[node] = new Position { X = 0, Y = 500 };
                nodeDepth.Add(0, 500);
            }
            else
            {
                if (parentNode == null)
                {
                    foreach (var parent in node.Node.Parents)
                    {
                        parentNode = CalculatedNodes.FirstOrDefault(n => n.Node.Id == parent);
                        if (parentNode != null && nodePosition.ContainsKey(parentNode))
                            break;
                    }
                }

                if (parentNode != null && !nodePosition.ContainsKey(node))
                {
                    var parentPosition = nodePosition[parentNode];
                    var targetY = parentPosition.Y + (parentNode.OccupatedHeight / 2 - node.OccupatedHeight / 2);
                    if (nodeDepth.TryGetValue(depth, out var lastDepthY))
                    {
                        if (lastDepthY > targetY)
                            yOffset = lastDepthY;
                        else
                            yOffset = targetY;
                    }
                    else
                    {
                        yOffset = targetY;
                    }

                    nodePosition[node] = new Position
                    {
                        X = parentPosition.X + NodeWidth + 100,
                        Y = yOffset
                    };

                    nodeDepth[depth] = nodePosition[node].Y + node.OccupatedHeight + 100;
                    node.CurrentDepth = depth;
                }
            }

            NodeProperties? firstNotCalculatedChild = null;
            for (int i = 0; i < node.Node.Children.Count; i++)
            {
                var childId = node.Node.Children[i];
                var childNode = CalculatedNodes.FirstOrDefault(f => f.Node.Id == childId);
                if (childNode == null || nodePosition.ContainsKey(childNode))
                    continue;

                firstNotCalculatedChild ??= childNode;

                CalculatePositionRecursive(childNode, node, depth + 1);
            }

            if (firstNotCalculatedChild != null)
            {
                nodePosition[node].Y = nodePosition[firstNotCalculatedChild].Y
                                       + firstNotCalculatedChild.OccupatedHeight / 2
                                       - node.OccupatedHeight / 2;
                nodeDepth[depth] = nodePosition[node].Y + node.OccupatedHeight + 100;
            }
        }
    }

}










