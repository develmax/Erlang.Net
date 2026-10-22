namespace Erlang.Tests;

internal static class BeamDelayBinary411Names
{
    internal const byte Label = 1, Allocate = 12, Deallocate = 18, Return = 19;
    internal const byte Remove = 21, TimeoutOpcode = 22, Loop = 23, Wait = 25, WaitTimeout = 26;
    internal const byte Jump = 61, Move = 64, EqualOpcode = 43, SavePosition = 167;
    internal const string SafeTest = "runtime/beam/delay-binary411/disjoint-binary-and-finite-wait";
    internal const string OpaqueTest = "runtime/beam/delay-binary411/opaque-x-y-context-position-refused";
    internal const string JoinTest = "runtime/beam/delay-binary411/join-uninitialized-backedge-and-matcher-composition-still-refused";
}
