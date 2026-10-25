namespace Erlang.Tests;

internal static class ClosedWaitOriginal480Names
{
    internal const string Test = "oracle/compiled-module/closed-wait480-closure-record-whole-values-and-stacks";
    internal const string AdmissionTest = "compiler/closed-wait480/original-debugger-image-publication";
    internal const string Prefix = "Erlang.Tests.ClosedWaitOriginal480.";
    internal const string Observer = "otp_wait480_observer";
    internal const string ObserverImage = "otp_wait480_observer.beam";
    internal const string Worker = "worker";
    internal const string Golden = "original-values.etf";
    internal const string Manifest = "fixture.json";
    internal const string ManifestFiles = "Files";
    internal const string Managed = "managed-values.etf";
    internal const string ListsImage = "lists.beam";
    internal const string Output = "artifacts/library480/permanent";
    internal const string Failure = "Closed receive loop values,effects,full stacks or original image ownership differ";
    internal const int Outcomes = 12;
    internal static readonly string[] AdmissionImages = ["dbg_wx_trace.beam", "dbg_wx_view.beam"];
    internal static readonly string[] Imports = ["code", "dbg_iserver", "dbg_wx_break", "dbg_wx_trace", "dbg_wx_trace_win", "dbg_wx_view", "dbg_wx_win", "dbg_wx_winman", "erlang", "filename", "int", "io", "io_lib", "lists", "otp_wait480_observer", "wx"];
}
