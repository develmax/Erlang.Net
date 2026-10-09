

namespace Erlang.Compiler;

internal static class BitPatternDiagnostics
{
    public const string UnsizedBinaryNotLast = "Unsized binary pattern segment must be last";
    public const string FloatLiteralOutOfRange = "Float pattern literal is outside the finite double range";
    public const string UnsupportedSegmentValue = "Bit pattern segments support variables and numeric literals only";
}
