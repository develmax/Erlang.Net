namespace Erlang;

internal static class ErrorLoggerFormats
{
    internal const string BadReportCallback = "REPORT_CB ERROR: ~tp; Returned: ~tp";
    internal const string Characters = "~ts";
    internal const string InvalidFormat = "ERROR: ~tp - ~tp";
    internal const string MapReport = "~tp\n";
    internal const string ReportCallbackCrash = "REPORT_CB CRASH: ~tp; Reason: ~tp";
    internal const string Term = "~p";
}
