namespace Erlang.Tests;

internal static class BeamRaiseEmpty412Names
{
    internal const string ValidTest = "runtime/beam/raise-empty412/known-class-empty-trace-caught";
    internal const string NegativeTest = "runtime/beam/raise-empty412/class-trace-clobber-label-refused";
    internal const string ReturnTest = "runtime/beam/raise-empty412/ordinary-invalid-class-badarg-return";
    internal const string OverrideTest = "runtime/beam/raise-empty412/changed-native-binding-refused-before-invocation";
    internal const string RaiseFunction = "raise", ThrowClass = "throw", ErrorClass = "error";
    internal const string AtomChunk = "AtU8";
    internal const byte Label = 1, ExternalCall = 7, Allocate = 12, Deallocate = 18, Return = 19;
    internal const byte Jump = 61, Move = 64, Try = 104, TryEnd = 105, TryCase = 106, Line = 153, InitY = 172;
    internal const int ThrowAtom = 3, ErrorAtom = 4, MarkerAtom = 7;
}
