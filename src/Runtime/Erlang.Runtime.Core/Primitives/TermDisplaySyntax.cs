namespace Erlang;

internal static class TermDisplaySyntax
{
    public const string Function = "display";
    public const string Comma = ",";
    public const string Bar = "|";
    public const string OpenList = "[";
    public const string CloseList = "]";
    public const string OpenTuple = "{";
    public const string CloseTuple = "}";
    public const string OpenMap = "#{";
    public const string Association = "=>";
    public const string EmptyList = "[]";
    public const string OpenBinary = "<<";
    public const string CloseBinary = ">>";
    public const string Quote = "\"";
    public const string AtomQuote = "'";
    public const string EscapeQuote = "\\\"";
    public const string EscapeAtomQuote = "\\'";
    public const string EscapeSlash = "\\\\";
    public const string NewLine = "\\n";
    public const string FormFeed = "\\f";
    public const string Tab = "\\t";
    public const string CarriageReturn = "\\r";
    public const string Backspace = "\\b";
    public const string VerticalTab = "\\v";
    public const string Escape = "\\";
    public const string Colon = ":";
    public const string Slash = "/";
    public const string Scientific = "e6";
    public const string LocalFunOpening = "#Fun<";
    public const string LocalFunSeparator = ".";
    public const string LocalFunClosing = ">";
    public const string ExternalFunOpening = "fun ";
    public const string Unsupported = "The term has no Erlang display representation";
}
