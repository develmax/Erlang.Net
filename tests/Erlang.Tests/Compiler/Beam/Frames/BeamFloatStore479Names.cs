namespace Erlang.Tests;

internal static class BeamFloatStore479Names
{
    internal const string Prefix = "compiler/float-store479/";
    internal const string Store = Prefix + "fills-frame-before-gc";
    internal const string Trimmed = Prefix + "trim-discards-unused-slot";
    internal const string Gc = Prefix + "gc-requires-initialized-roots";
    internal const string YRead = Prefix + "uninitialized-y-read-refused";
    internal const string FloatRead = Prefix + "uninitialized-float-refused";
    internal const string Bounds = Prefix + "destination-bounds-refused";
    internal const string Handler = Prefix + "handler-overwrite-refused";
    internal const string Failure = "BEAM floating store frame contract failed";
    internal const byte Trim = 136;
    internal const byte Catch = 62;
    internal const byte CatchEnd = 63;
    internal const byte Label = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
