namespace Erlang.Tests;

internal static class BeamSmallOpcodeControlNames
{
    internal const string NumericInequality = "runtime/beam/small393/numeric-inequality";
    internal const string SelectiveReceiveNumericInequality = "runtime/beam/small393/selective-receive-numeric-inequality";
    internal const string NegationBits = "runtime/beam/small393/negation-bits";
    internal const string NegationUninitialized = "runtime/beam/small393/negation-uninitialized";
    internal const string NegationStaleAfterCall = "runtime/beam/small393/negation-stale-after-call";
    internal const string NegationFreshAfterCall = "runtime/beam/small393/negation-fresh-after-call";
    internal const string NegationWrongDestination = "runtime/beam/small393/negation-wrong-destination";
    internal const string InequalityUninitializedY = "runtime/beam/small393/inequality-uninitialized-y";
    internal const byte ArithmeticNotEqual = 42;
    internal const byte FloatNegate = 102;
    internal const byte FloatConvert = 97;
    internal const byte FloatMove = 96;
    internal const byte FloatRegister = 0x27;
    internal const byte Label = 1;
    internal const byte Move = 64;
    internal const byte Return = 19;
    internal const byte Allocate = 12;
    internal const byte Deallocate = 18;
    internal const byte HeapTest = 16;
    internal const byte ExternalCall = 7;
    internal const byte LoopReceive = 23;
    internal const byte LoopReceiveEnd = 24;
    internal const byte WaitTimeout = 26;
    internal const byte ReceiveTimeoutOpcode = 22;
    internal const byte RemoveMessage = 21;
    internal const string Failure = "BEAM arithmetic inequality or float negation control failed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
