namespace Erlang;

internal static class SequenceTraceRootNames
{
    internal const string Invalid = "Invalid or uncovered sequence trace token storage.";
    internal const string PreparationUnderRuntimeGate = "Sequence trace preparation requires an outside-runtime-gate seam.";
    internal const int MaximumOutputWrapperNodes = 256;
    internal const int MaximumOutputWrapperDepth = 64;
    internal const int Arity = 5;
    internal const int Flags = 0;
    internal const int Label = 1;
    internal const int Serial = 2;
    internal const int Sender = 3;
    internal const int LastCount = 4;
}
