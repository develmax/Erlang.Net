namespace Erlang.Tests;

internal static class BeamMissingHeaderLineNames
{
    internal const string Test = "runtime/beam/line-location/missing-header-does-not-inherit-prior-function";
    internal const string Failure = "Missing optional BEAM function line inherited another function location";
    internal const string Code = "Code";
    internal const string Functions = "Functions";
    internal const string Name = "Name";
    internal const string First = "FirstInstructionIndex";
    internal const string Locations = "Erlang.BeamInstructionLocations";
    internal const string Read = "Read";
    internal const string Line = "line";
    internal const string LineChunk = "Line";
}
