using System.Globalization;

namespace Erlang.Compiler;

internal static class SourceOperatorCode
{
    private const string UnaryType = "new global::Erlang.Compiler.Expr.Unary(";
    private const string BinaryType = "new global::Erlang.Compiler.Expr.Binary(";
    private const string SourceLineMember = "){SourceLine=";

    internal static string Unary(Expr.Unary expression, string operation, string operand)
        => UnaryType + operation + "," + operand + SourceLineMember + expression.SourceLine.ToString(CultureInfo.InvariantCulture) + "}";

    internal static string Binary(
        Expr.Binary expression,
        string operation,
        string left,
        string right
    )
        => BinaryType + operation + "," + left + "," + right + SourceLineMember + expression.SourceLine.ToString(CultureInfo.InvariantCulture) + "}";
}
