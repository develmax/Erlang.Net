namespace Erlang.Otp;

internal static class ProcLibReportFormatNames
{
    public const string Label = "label";
    public const string Crash = "crash";
    public const string Report = "report";
    public const string CharsLimit = "chars_limit";
    public const string Depth = "depth";
    public const string SingleLine = "single_line";
    public const string Encoding = "encoding";
    public const string Latin1 = "latin1";
    public const string Utf8 = "utf8";
    public const string Unlimited = "unlimited";
    public const string InitialCall = "initial_call";
    public const string ProcessLabel = "process_label";
    public const string ErrorInfo = "error_info";
    public const string Neighbour = "neighbour";
    public const string Pid = "pid";
    public const string Empty = "";
    public const string Space = " ";
    public const string Indent = "  ";
    public const string ExceptionIndent = "    ";
    public const string Newline = "\n";
    public const string ReportSeparator = "; ";
    public const string PartSeparator = ", ";
    public const string ZeroWidth = "0";
    public const string UnicodeModifier = "t";
    public const string Pretty = "p";
    public const string DepthPretty = "P";
    public const string TextControl = "~s";
    public const string UnicodeTextControl = "~ts";
    public const string CrasherHeading = "crasher:";
    public const string CrasherBudgetHeading = "crasher";
    public const string NeighboursHeading = "neighbours:";
    public const string NeighboursControl = "neighbours: ~";
    public const string NeighbourHeading = "neighbour:";
    public const string InitialCallHeading = "initial call: ";
    public const string ModuleSeparator = ":";
    public const char ModuleSeparatorCharacter = ':';
    public const char AritySeparatorCharacter = '/';
    public const string AritySeparator = "/";
    public const string ClassSeparator = ": ";
    public const string ControlPrefix = "~";
    public const string TagControlSuffix = "p: ~";
    public const string TermIndentControl = ".18";
    public const int BudgetParts = 3;
    public const int MinimumBudget = 1;
}
