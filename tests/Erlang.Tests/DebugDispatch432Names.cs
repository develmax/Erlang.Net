namespace Erlang.Tests;

internal static class DebugDispatch432Names
{
    internal const string Test = "runtime/debug432-real-dispatch-entry-pause-resume-kill-and-image-generation";
    internal const string Debug = "erts_debug", Breakpoint = "breakpoint", Instructions = "instructions", True = "true", False = "false";
    internal const string CloseTest = "runtime/debug432-real-stopped-entry-dispose-halt-and-initializer-retirement";
    internal const string HaltTransfer = "Erlang.ProcessHaltTransfer";
    internal const string Failure = "Actual debugger dispatch, entry trap or lifecycle differs";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
