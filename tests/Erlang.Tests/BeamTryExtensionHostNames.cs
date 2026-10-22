namespace Erlang.Tests;

internal static class BeamTryExtensionHostNames
{
    internal const string AsyncTest = "runtime/beam-try/async-host-erlang-exception-caught";
    internal const string TailErrorsTest = "runtime/beam-fun/tail-rejection-retains-source-caller";
    internal const string TailReturnTest = "runtime/beam-fun/tail-return-restores-local-continuation";
    internal const string CaptureSourceFailure = "CaptureSourceFailure";
    internal const byte LocalCallOpcode = 4;
    internal const byte PutTupleOpcode = 164;
    internal const byte ExtendedOperand = 0x17;
    internal const int TupleArity = 2;
    internal const int FunctionCount = 2;
    internal const int WrongCallbackArity = 1;
    internal const int MiddleInfoLabel = 3;
    internal const int MiddleEntryLabel = 4;
    internal const int MiddleAtomIndex = 3;
    internal const int TwoFunctionLabelCount = 5;
}
