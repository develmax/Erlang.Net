

namespace Erlang.Compiler;

internal static class BitSyntaxDefaults
{
    // Default integer width and the number of bits in a byte happen to be equal.
    public const int IntegerWidth = 8;
    public const int FloatWidth = 64;
    public const int BinaryUnit = 8;
    public const int NumericUnit = 1;
}
