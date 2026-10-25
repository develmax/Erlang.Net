namespace Erlang;

internal static class DistributionSequenceTraceLayout
{
    internal const int Arity = 5;
    internal const int FlagsIndex = 0;
    internal const int LabelIndex = 1;
    internal const int SerialIndex = 2;
    internal const int SenderIndex = 3;
    internal const int LastCountIndex = 4;
    internal const int SmallPayloadBits = 60;
    internal const long MinimumSmallInteger = -(1L << (SmallPayloadBits - 1));
    internal const long MaximumSmallInteger = (1L << (SmallPayloadBits - 1)) - 1;
}
