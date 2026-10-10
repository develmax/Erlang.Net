// Modified: CLR list-comprehension subset of OTP-29.1.1 erl_eval/erl_lint.
// Fresh generator bindings, original key/size scopes and filter error distinctions.
namespace Erlang.Compiler;

internal static class ComprehensionGuard
{
    public static bool IsTest(Expr expression, ModuleDefinition? module) => expression switch
    {
        Expr.Literal or Expr.Variable => true,
        Expr.Tuple t => t.Items.All(e => IsTest(e, module)),
        Expr.List l => l.Items.All(e => IsTest(e, module)) && (l.Tail is null || IsTest(l.Tail, module)),
        Expr.Map m => (m.Base is null || IsTest(m.Base, module)) && m.Fields.All(f => IsTest(f.Key, module) && IsTest(f.Value, module)),
        Expr.Bits b => b.Segments.All(s => IsTest(s.Value, module) && (s.Size is null || IsTest(s.Size, module))),
        Expr.Unary u => IsTest(u.Operand, module),
        Expr.Binary b => b.Operator is not (ErlangOperators.Send or ErlangOperators.Append or ErlangOperators.SubtractList) && IsTest(b.Left, module) && IsTest(b.Right, module),
        Expr.Call c => (c.Module == ErlangModuleNames.Module || c.Module is null && module?.Functions.Any(f => f.Name == c.Function && f.Arity == c.Arguments.Count) != true) && (Semantics.GuardBifs.Contains((c.Function, c.Arguments.Count)) || c.Module == ErlangModuleNames.Module && IsOperator(c.Function, c.Arguments.Count)) && c.Arguments.All(e => IsTest(e, module)),
        _ => false
    };

    public static bool IsOperator(string name, int arity) => arity switch
    {
        1 => name is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Not or ErlangOperators.BitwiseNot,
        2 => name is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply or ErlangOperators.Divide or ErlangOperators.IntegerDivide or ErlangOperators.Remainder or ErlangOperators.BitwiseAnd or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight or ErlangOperators.And or ErlangOperators.Or or ErlangOperators.Xor or ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual or ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual or ErlangOperators.Less or ErlangOperators.Greater or ErlangOperators.LessOrEqual or ErlangOperators.GreaterOrEqual,
        _ => false
    };
}
