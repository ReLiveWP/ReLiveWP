using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace ReLiveWP.Services.Activity.Providers.Mastodon;

public static class StatusHtmlConverter
{
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var parser = new HtmlParser();
        using var document = parser.ParseDocument(html);

        if (document.Body == null)
            return "";

        var writer = new PlainTextWriter();
        writer.WriteChildren(document.Body);

        return writer.ToString();
    }

    private sealed class PlainTextWriter
    {
        private const char NoBreakSpace = (char)0xA0;
        private const string ParagraphBreak = "\n\n";
        private const string LineBreak = "\n";

        private static readonly HashSet<string> BlockElements =
            ["p", "div", "blockquote", "pre", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6"];

        private static readonly HashSet<string> SkippedElements = ["script", "style", "template"];

        private readonly StringBuilder output = new();
        private string pendingBreak = "";
        private int preformattedDepth;

        public void WriteChildren(INode parent)
        {
            foreach (var child in parent.ChildNodes)
            {
                switch (child)
                {
                    case IText text:
                        WriteText(text.Data);
                        break;
                    case IElement element:
                        WriteElement(element);
                        break;
                }
            }
        }

        public override string ToString() => output.ToString().Trim();

        private void WriteElement(IElement element)
        {
            var name = element.LocalName;

            if (SkippedElements.Contains(name))
                return;

            if (name == "br")
            {
                FlushBreak();
                output.Append('\n');
                return;
            }

            if (name == "li")
            {
                RequestBreak(LineBreak);
                FlushBreak();
                output.Append("- ");
                WriteChildren(element);
                RequestBreak(LineBreak);
                return;
            }

            var isBlock = BlockElements.Contains(name);
            var isPreformatted = name == "pre";

            if (isBlock)
                RequestBreak(ParagraphBreak);

            if (isPreformatted)
                preformattedDepth++;

            WriteChildren(element);

            if (isPreformatted)
                preformattedDepth--;

            if (isBlock)
                RequestBreak(ParagraphBreak);
        }

        private void WriteText(string text)
        {
            var preformatted = preformattedDepth > 0;
            if (!preformatted)
                text = CollapseWhitespace(text);

            if (!preformatted && (output.Length == 0 || output[^1] == '\n' || pendingBreak.Length > 0))
                text = text.TrimStart();

            if (text.Length == 0)
                return;

            FlushBreak();
            output.Append(text);
        }

        private static string CollapseWhitespace(string text)
        {
            var collapsed = new StringBuilder(text.Length);
            var inWhitespace = false;

            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c) && c != NoBreakSpace)
                {
                    if (!inWhitespace)
                        collapsed.Append(' ');

                    inWhitespace = true;
                    continue;
                }

                collapsed.Append(c);
                inWhitespace = false;
            }

            return collapsed.ToString();
        }

        private void RequestBreak(string wanted)
        {
            if (output.Length == 0)
                return;

            if (wanted.Length > pendingBreak.Length)
                pendingBreak = wanted;
        }

        private void FlushBreak()
        {
            if (pendingBreak.Length == 0)
                return;

            while (output.Length > 0 && output[^1] == ' ')
                output.Length--;

            var trailing = 0;
            for (var i = output.Length - 1; i >= 0 && output[i] == '\n'; i--)
                trailing++;

            if (trailing < pendingBreak.Length)
                output.Append('\n', pendingBreak.Length - trailing);

            pendingBreak = "";
        }
    }
}
