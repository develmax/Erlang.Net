namespace Erlang.Tests;

internal static class BeamInfallibleBifControlNames
{
    internal const string Failure = "Non-GC infallible BIF contract control failed";
    internal const string Get = "get";
    internal const string Min = "min";
    internal const string Max = "max";
    internal const string Length = "length";
    internal const string TupleSize = "tuple_size";
    internal const string OverrideFailure = "untrusted_bif_override394";
    internal const byte Allocate = 12;
    internal const byte Unary = 10;
    internal const byte Binary = 11;
    internal const byte UnaryGc = 124;
    internal const byte Catch = 62;
    internal const byte CatchEnd = 63;
    internal const byte Move = 64;
    internal const byte Label = 1;
    internal const byte Deallocate = 18;
    internal const byte Return = 19;
    internal const byte ExternalTail = 78;
}
