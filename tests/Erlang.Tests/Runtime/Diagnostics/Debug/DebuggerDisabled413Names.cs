using System.Reflection;

namespace Erlang.Tests;

internal static class DebuggerDisabled413Names
{
    internal const string Test = "HOST/library413/real-original-debugger-disabled-thirteen-owner-lifecycle";
    internal const string Module = "erl_debugger";
    internal const string NativeType = "Erlang.DebuggerDisabledNative413";
    internal const string Capture = "TryCapture";
    internal const string Stop = "Stop";
    internal const string Resource = "Erlang.Tests.Resources.DebuggerDisabled413.erl_debugger.beam";
    internal const string Manifest = "Erlang.Tests.Resources.DebuggerDisabled413.manifest.json";
    internal const string Failure = "Debugger disabled source-profile HOST control failed";
    internal const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
    internal static readonly (string Function, int Arity)[] Signatures =
    [("supported",0),("instrumentations",0),("toggle_instrumentations",1),("register",1),
     ("unregister",2),("whereis",0),("breakpoint",3),("breakpoints",1),("breakpoints",3),
     ("stack_frames",2),("peek_stack_frame_slot",4),("xregs_count",1),("peek_xreg",3)];
}
