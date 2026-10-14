using System.Numerics;

namespace Erlang.Compiler;

internal static class BasedIntegerDiagnostics
{
    public const string IllegalInteger = "Illegal integer literal";
    public const string IllegalFloat = "Illegal float literal";
    public const string BasedFloatUnsupported = "Based floating-point literals are not implemented yet";

    public static string InvalidBase(BigInteger value) => "Invalid integer base " + value;
}
