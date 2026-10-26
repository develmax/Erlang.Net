namespace Erlang;

internal static class EtfControlIntegerLayout
{
    internal const int SmallTupleArityOffset = 2;
    internal const int SmallTupleBodyOffset = 3;
    internal const int LargeTupleArityOffset = 2;
    internal const int LargeTupleBodyOffset = 6;
    internal const int ScalarBodyOffset = 1;
    internal const int SmallBigSignOffset = 2;
    internal const int SmallBigBodyOffset = 3;
    internal const int LargeBigSignOffset = 5;
    internal const int LargeBigBodyOffset = 6;
    internal const int NegativeSign = 1;
    internal const int MagnitudeBytes = sizeof(uint);
    internal const int BitsPerByte = 8;
}
