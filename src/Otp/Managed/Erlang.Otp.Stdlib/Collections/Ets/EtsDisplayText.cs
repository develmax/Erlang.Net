namespace Erlang;

internal static class EtsDisplayText
{
    internal const string BrowsePrompt = "(c)ontinue (q)uit (p)Digits (k)ill /Regexp -->";
    internal const string ContinueCommand = "c\n";
    internal const string DeleteCommand = "k\n";
    internal const string EndPrompt = "EOT  (q)uit (p)Digits (k)ill /Regexp -->";
    internal const string InvalidNumber = "Bad digits\n";
    internal const string NewLine = "\n";
    internal const string NumberedObject = "<~-4w> ~s~n";
    internal const string QuitCommand = "q\n";
    internal const string RegexLineEnding = "$\n";
    internal const string TableRow = " ~s ~s ~s ~s ~s ~s\n";
    internal const string TableRule = " ----------------------------------------------------------------------------\n";
    internal const string Term = "~p";
    internal const string TermLine = "~p~n";
    internal const string TruncationMarker = "  ...";
    internal const string UnicodeLine = "~ts~n";
    internal const string UnreadableTable = "~-10s ... unreadable \n";
}
