namespace Erlang.Compiler;

internal static class RecordPredicateSyntax
{
    public static bool IsBuiltin(string? module, string name) => module is null or ErlangModuleNames.Module && name == RecordPredicateNames.IsRecord;

    public static bool HasLiteralNonpositiveSize(Expr.Call call) => IsBuiltin(call.Module, call.Function) && call.Arguments.Count == 3 && call.Arguments[2] switch
    {
        Expr.Literal { Value: Integer size } => size.Value <= 0,
        Expr.Unary { Operator: ErlangOperators.Minus, Operand: Expr.Literal { Value: Integer size } } => size.Value >= 0,
        Expr.Unary { Operator: ErlangOperators.Plus, Operand: Expr.Literal { Value: Integer size } } => size.Value <= 0,
        _ => false
    };
}
