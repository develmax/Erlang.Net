namespace Erlang.Otp.Gui.Library.Tests;

internal static class GuiClosedWait480Names
{
    internal const string Test = "oracle/compiled-module/closed-wait480-closure-record-whole-values-and-stacks";
    internal const string AdmissionTest = "compiler/closed-wait480/original-debugger-image-publication";
    internal const string Prefix = "Erlang.Otp.Gui.Library.Tests.ClosedWait480.";
    internal const string Observer = "otp_wait480_observer";
    internal const string ObserverImage = "otp_wait480_observer.beam";
    internal const string Worker = "worker";
    internal const string Golden = "original-values.etf";
    internal const string Manifest = "fixture.json";
    internal const string ManifestFiles = "Files";
    internal const string Managed = "managed-values.etf";
    internal const string ListsImage = "lists.beam";
    internal const string Output = "artifacts/otp-gui-managed/closed-wait480";
    internal const string Failure = "Closed receive loop values,effects,full stacks or original image ownership differ";
    internal const string OutputDirectory = "closed-wait480";
    internal const string TraceImage = "dbg_wx_trace.beam";
    internal const string ViewImage = "dbg_wx_view.beam";
    internal const string TraceModule = "dbg_wx_trace";
    internal const string ViewModule = "dbg_wx_view";
    internal const string ListsModule = "lists";
    internal const string Completion = "Original GUI closed wait480: 12 complete outcomes/stacks match OTP";
    internal const int Outcomes = 12;
    internal static readonly string[] AdmissionImages = ["dbg_wx_trace.beam", "dbg_wx_view.beam"];
    internal static readonly string[] Imports = ["code", "dbg_iserver", "dbg_wx_break", "dbg_wx_trace", "dbg_wx_trace_win", "dbg_wx_view", "dbg_wx_win", "dbg_wx_winman", "erlang", "filename", "int", "io", "io_lib", "lists", "otp_wait480_observer", "wx"];
}
