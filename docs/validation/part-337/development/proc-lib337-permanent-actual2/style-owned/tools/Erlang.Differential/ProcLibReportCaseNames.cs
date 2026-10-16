namespace Erlang.Differential;

public static class ProcLibReportCaseNames
{
    public const string Prefix = "proc-lib-report-";
    public const string Module = "proc_lib";
    public const string Format = "format";
    public const string ReportCallback = "report_cb";
    public const string EmptyDefault = "empty-default";
    public const string EmptyLatin1 = "empty-explicit-latin1";
    public const string EmptyDepth = "empty-depth-unused";
    public const string EmptyCallback = "empty-limited-report";
    public const string Text = "text";
    public const string Latin1 = "latin1";
    public const string Unicode = "unicode";
    public const string Label = "label";
    public const string Crash = "crash";
    public const string Report = "report";
    public const string CharsLimit = "chars_limit";
    public const int LimitedCharacters = 80;
    public const int DirectDepth = 2;
    public const string Mismatch = "The proc_lib report differs from the independently executed OTP result.";
}
