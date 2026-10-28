namespace Erlang.Tests;

internal static class OwnedTrampoline506Names
{
    internal const string CarriersTest = "runtime/owned-trampoline506/all-carriers-survive-collection-before-next-admission";
    internal const string PlateauTest = "runtime/owned-trampoline506/fixed-tail-control-root-plateau";
    internal const string CancelTest = "runtime/owned-trampoline506/cancel-held-handoff-retires-real-roots";
    internal const string CancelFunctionTest = "runtime/owned-trampoline506/cancel-admitted-function-target-releases-roots";
    internal const string GuestModule = "owned_handoff506";
    internal const string RemoteEntry = "remote";
    internal const string PrivateEntry = "private";
    internal const string RuntimeEntry = "runtime_tail";
    internal const string FunctionEntry = "fun_tail";
    internal const string DeepEntry = "deep";
    internal const string ProbeType = "Erlang.Compiler.OwnedTrampolineCheckpoint506";
    internal const string ProbeEnter = "Enter";
    internal const string Heap = "TermHeap506";
    internal const string Snapshot = "Snapshot";
    internal const string Collect = "Collect";
    internal const string Words = "LivePayloadWords";
    internal const string Roots = "RootFrames";
    internal const string Capacity = "CapacityWords";
    internal const string Unwound = "Unwound";
    internal const string TaskProperty = "Task";
    internal const string Private = "TailCall";
    internal const string Source = "SourceTailCall";
    internal const string Runtime = "RuntimeTailCall";
    internal const string Function = "FunctionTailCall";
    internal const string SourceFunction = "SourceFunctionTransfer";
    internal const string Failure = "Owned trampoline506 root handoff control failed.";
    internal const int Iterations = 128;
    internal const long PlateauPayloadBudget = 64;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string GuestSource = """
        -module(owned_handoff506).
        -export([remote/1, private/1, runtime_tail/1, fun_tail/1, deep/1]).
        remote(X) -> owned_handoff_target506:echo({X,[1,2]}).
        private(X) -> local_echo({X,[1,2]}).
        local_echo(X) -> X.
        runtime_tail(X) -> erlang:get_cookie({X,[1,2]}).
        fun_tail(X) -> A={X,[1,2]}, F=fun(Y) -> {A,Y} end, F(X).
        deep(N) -> loop(N,{0,[1,2]}).
        loop(0,A) -> A;
        loop(N,_) -> loop(N-1,{N,[1,2]}).
        """;
    internal const string TargetSource = """
        -module(owned_handoff_target506).
        -export([echo/1]).
        echo(X) -> X.
        """;
}
