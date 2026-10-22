namespace Erlang.Tests;

internal static class BeamCatchProtocolControlNames
{
    internal const string Test0 = "runtime/beam-catch/protocol-refusals";
    internal const string Test1 = "runtime/beam-catch/normal-exception-retained-trim";
    internal const string Test2 = "runtime/beam-receive/pure-node-range-queue-preserved";
    internal const string Test3 = "runtime/beam-receive/non-node-bif-matcher-refused";
    internal const string Failure = "Synthetic catch protocol control failed";
    internal const string Payload = "catch_payload392";
    internal const string Exit = "EXIT";
    internal const byte Allocate = 12;
    internal const byte InitializeY = 172;
    internal const byte ArgumentList = 0x17;
    internal const byte Deallocate = 18;
    internal const byte Return = 19;
    internal const byte Jump = 61;
    internal const byte Catch = 62;
    internal const byte CatchEnd = 63;
    internal const byte Move = 64;
    internal const byte CallFun = 178;
    internal const byte UnsafeAtomIndex = 9;
    internal const int MaximumOpcodeOffset = 8;
    internal const string Unsafe = "unsafe";
    internal const byte Try = 104;
    internal const byte TryEnd = 105;
    internal const byte TryCase = 106;
    internal const byte Trim = 136;
    internal const byte Label = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
