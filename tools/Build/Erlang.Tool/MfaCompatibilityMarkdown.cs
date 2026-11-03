internal static class MfaCompatibilityMarkdown
{
    public static string Code(string value)
    {
        string content = value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        string delimiter = "`";
        while (content.Contains(delimiter, StringComparison.Ordinal))
            delimiter += "`";
        if (content.StartsWith('`') || content.EndsWith('`'))
            content = " " + content + " ";

        return delimiter + content + delimiter;
    }

    public static string Escape(string value) => value.Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|").Replace("<", "&lt;").Replace(">", "&gt;");

}
