namespace Erlang.Tests;

internal static class BeamTupleSelectHostNames
{
    internal const string Branches = "runtime/beam/tuple-select381/branches-and-fallback";
    internal const string Rejections = "runtime/beam/tuple-select381/malformed-and-unproven-rejections";
    internal const string Receive = "runtime/beam/tuple-select381/selective-receive-preserves-unmatched";
    internal const string Failure = "Tuple arity selection check failed";
    internal const byte SelectOpcode = 60;
    internal const byte TupleTestOpcode = 57;
    internal const byte ExtendedList = 0x17;
}
