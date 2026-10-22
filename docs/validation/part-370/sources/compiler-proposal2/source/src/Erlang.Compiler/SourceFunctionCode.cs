using System.Globalization;

namespace Erlang.Compiler;

internal static class SourceFunctionCode
{
    private const string CallType = "new global::Erlang.Compiler.Expr.Call(";
    private const string ApplyType = "new global::Erlang.Compiler.Expr.Apply(";
    private const string AnonymousType = "new global::Erlang.Compiler.Expr.Fun(";
    private const string SourceLineMember = "){SourceLine=";
    private const string BooleanTrue = "true";
    private const string BooleanFalse = "false";

    internal static string Apply(Expr.Apply expression, string function, string arguments)
        => ApplyType + function + "," + arguments + "," + (expression.CanDiscardContinuation ? BooleanTrue : BooleanFalse)
            + SourceLineMember + expression.SourceLine.ToString(CultureInfo.InvariantCulture) + "}";

    internal static string Call(Expr.Call expression, string module, string function, string arguments)
        => CallType + module + "," + function + "," + arguments + ","
            + (expression.CanDiscardBifContinuation ? BooleanTrue : BooleanFalse)
            + SourceLineMember + expression.SourceLine.ToString(CultureInfo.InvariantCulture) + "}";

    internal static string Anonymous(Expr.Fun expression, string clauses)
        => AnonymousType + clauses
            + (expression.CoreOrdinal is { } ordinal ? "," + ordinal.ToString(CultureInfo.InvariantCulture) : string.Empty) + ")";
}
