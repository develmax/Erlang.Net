namespace Erlang.Tests;

internal static class MathLog2OtpNames
{
    internal const string Test = "runtime/math/pinned-windows-log2-rounding-both-backends";
    internal const string Module = "math";
    internal const string Function = "log2";
    internal const string OriginalImage = "math.beam";
    internal const ulong AboveOneInput = 0x3ff0000000000001;
    internal const ulong AboveOneResult = 0x3cb71547652b82fe;
    internal const long PowerOfTwoInput = 36028797018963968;
    internal const ulong PowerOfTwoResult = 0x404b800000000001;
}
