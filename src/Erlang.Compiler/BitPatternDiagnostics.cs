namespace Erlang.Compiler;

internal static class BitPatternDiagnostics
{
    public const string StringModifiers = "String pattern segments allow only default syntax or unsized UTF types";
    public const string UnsizedBinaryNotLast = "Unsized binary pattern segment must be last";
    public const string UnsizedGeneratorField = "Unsized binary fields are not allowed in binary generator patterns";
    public const string EmptyGeneratorPattern = "Empty binary generator patterns are not supported";
    public const string FloatLiteralOutOfRange = "Float pattern literal is outside the finite double range";
    public const string UnsupportedSegmentValue = "Bit pattern segments support variables and numeric literals only";
}
