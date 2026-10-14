namespace Erlang;

internal static class BeamAllocationWire
{
    internal const int MaximumEntries = 3;
    internal const int Words = 0;
    internal const int Floats = 1;
    internal const int Funs = 2;
    internal const int NativeFloatWords64 = 2;
    internal const int NativeFunWords64 = 2;
    internal const int NativeMaximumNumber = int.MaxValue / (NativeFunWords64 + 1);
}
