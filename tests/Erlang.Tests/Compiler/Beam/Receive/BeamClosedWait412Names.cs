namespace Erlang.Tests;

internal static class BeamClosedWait412Names
{
    internal const string ExecutionTest = "runtime/beam/closed-wait412/caught-wait-error-retains-rejected-message";
    internal const string ComponentTest = "runtime/beam/closed-wait412/mutual-tail-wait-component-admission";
    internal const string NegativeTest = "runtime/beam/closed-wait412/return-handler-unknown-tail-roots-still-refused";
    internal const byte Label = 1, Call = 4, CallOnly = 6, ExternalCall = 7, Allocate = 12, Deallocate = 18;
    internal const byte Return = 19, Remove = 21, Loop = 23, LoopEnd = 24, Wait = 25, EqualOpcode = 43, Move = 64;
    internal const byte ExternalTail = 78, Try = 104, TryCase = 106;
    internal const string ErrorFunction = "error";
    internal const string LengthFunction = "length";
    internal const int RunInfo = 1, RunEntry = 2, Handler = 3, MiddleInfo = 4, MiddleEntry = 5;
    internal const int Scan = 6, Empty = 7, Rejected = 8, LeafInfo = 9, LeafEntry = 10;
    internal const int RunAtom = 2, MiddleAtom = 3, LeafAtom = 4, MarkerAtom = 7;
}
