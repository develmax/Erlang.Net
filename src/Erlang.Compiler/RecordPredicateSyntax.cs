using System.Numerics;

namespace Erlang.Compiler;

internal static class RecordPredicateSyntax
{
    public static bool IsBuiltin(string? module, string name) => module is null or ErlangModuleNames.Module && name == RecordPredicateNames.IsRecord;

    public static bool IsLegalGuard(Expr.Call call) => !IsBuiltin(call.Module, call.Function) ||
        call.Arguments.Count == 3 &&
        call.Arguments[1] is Expr.Literal { Value: Atom } &&
        call.Arguments[2] is Expr.Literal { Value: Integer or Atom };

    public static bool HasLiteralNonpositiveSize(Expr.Call call) => IsBuiltin(call.Module, call.Function) && call.Arguments.Count == 3 && call.Arguments[2] is Expr.Literal { Value: Integer size } && size.Value <= 0;

    public static CompiledRecordPredicate? TupleTest(Expr.Call call)
    {
        IReadOnlyList<Expr> arguments = call.Arguments;
        bool staticApply = false;
        if (!IsBuiltin(call.Module, call.Function))
        {
            if (call.Module is not (null or ErlangModuleNames.Module) || call.Function != ErlangBasicBifNames.Apply || arguments.Count != 3 ||
                arguments[0] is not Expr.Literal { Value: Atom { Name: ErlangModuleNames.Module } } ||
                arguments[1] is not Expr.Literal { Value: Atom { Name: RecordPredicateNames.IsRecord } } ||
                arguments[2] is not Expr.List { Items.Count: 3, Tail: null } list)
                return null;
            arguments = list.Items;
            staticApply = true;
        }
        if (arguments.Count != 3 || arguments[1] is not Expr.Literal { Value: Atom tag })
            return null;
        BigInteger? size = arguments[2] switch
        {
            Expr.Literal { Value: Integer literal } => literal.Value,
            Expr.Unary { Operator: ErlangOperators.Minus, Operand: Expr.Literal { Value: Integer literal } } => -literal.Value,
            Expr.Unary { Operator: ErlangOperators.Plus, Operand: Expr.Literal { Value: Integer literal } } => literal.Value,
            _ => null
        };

        return size is { } value && value > 0 ? new(tag, value, staticApply) : null;
    }

    public static Term Test(Term value, CompiledRecordPredicate predicate) => value is TupleTerm { Items.Count: > 0 } tuple && tuple.Items.Count == predicate.Size && tuple.Items[0].Equals(predicate.Tag) ? Term.A(ErlangBooleanAtoms.True) : Term.A(ErlangBooleanAtoms.False);
}
