namespace Erlang.Tests;

internal static class BeamInitializedRootNames
{
    internal const string GcOnlyTest = "runtime/beam/initialized-roots391/GcOnly";
    internal const string TrimGcTest = "runtime/beam/initialized-roots391/TrimGc";
    internal const string TransportEscapeTest = "runtime/beam/initialized-roots391/TransportEscape";
    internal const string TupleEscapeTest = "runtime/beam/initialized-roots391/TupleEscape";
    internal const string TrimEscapeTest = "runtime/beam/initialized-roots391/TrimEscape";
    internal const string ReallocateUninitializedTest = "runtime/beam/initialized-roots391/ReallocateUninitialized";
    internal const string OverwriteThenReadTest = "runtime/beam/initialized-roots391/OverwriteThenRead";
    internal const byte Label = 1;
    internal const byte ExternalCall = 7;
    internal const byte Allocate = 12;
    internal const byte HeapTest = 16;
    internal const byte Deallocate = 18;
    internal const byte Return = 19;
    internal const byte EqualOpcode = 43;
    internal const byte Jump = 61;
    internal const byte Move = 64;
    internal const byte Try = 104;
    internal const byte TryEnd = 105;
    internal const byte TryCase = 106;
    internal const byte Trim = 136;
    internal const byte PutTuple = 164;
    internal const byte ArgumentList = 0x17;
    internal const int Arity = 1;
    internal const int HandlerSlot = 0;
    internal const int MixedSlot = 1;
    internal const int FrameSlots = 2;
    internal const int FailureLabel = 3;
    internal const int HandlerLabel = 4;
    internal const int JoinLabel = 5;
    internal const int LabelCount = 6;
    internal const string Failure = "BEAM initialized-root kind separation failed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
