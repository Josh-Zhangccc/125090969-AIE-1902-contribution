using System.Text;
using System.Text.RegularExpressions;

public static class MarkdownParser
{
    private static readonly Regex BoldRegex   = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicRegex = new Regex(@"\*(.+?)\*",     RegexOptions.Compiled);

    public static string ToTmpRichText(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return markdown;

        var lines = markdown.Split('\n');
        var sb = new StringBuilder(markdown.Length + 128);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.StartsWith("## "))
                sb.Append("<size=+4><b>").Append(line.Substring(3)).Append("</b></size>");
            else if (line.StartsWith("# "))
                sb.Append("<size=+6><b>").Append(line.Substring(2)).Append("</b></size>");
            else if (line.StartsWith("- "))
                sb.Append("  • ").Append(ProcessInline(line.Substring(2)));
            else
                sb.Append(ProcessInline(line));

            if (i < lines.Length - 1)
                sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string ProcessInline(string text)
    {
        text = BoldRegex.Replace(text, "<b>$1</b>");
        text = ItalicRegex.Replace(text, "<i>$1</i>");
        return text;
    }
}
