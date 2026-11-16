using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedIntegerGuardResult(FunctionDefinition function)
        {
            var clauses = function.Clauses;
            if (function.Arity != 1 || clauses.Count < 4
                || clauses[^2] is not { Patterns: [Erlang.Pattern.Variable input], Guard: { } guard } guarded
                || input.Name == VariableScopeNames.Wildcard
                || PredicateConjuncts(guard).ToArray() is not
                [
                    Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.IsInteger, Arguments: [Expr.Variable tested] } type,
                    Expr.Binary { Operator: ErlangOperators.Greater or ErlangOperators.GreaterOrEqual,
                        Left: Expr.Variable compared, Right: Expr.Literal { Value: Integer bound } } comparison
                ]
                || tested.Name != input.Name || compared.Name != input.Name || !owner.ResolvesErlangBif(type)
                || !AttributeLiteral.TryNormalizeConstant(guarded.Body, out Term result)
                || clauses[^1] is not { Patterns: [Erlang.Pattern.Variable fallback], Guard: null, Body: Expr.Call call }
                || fallback.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            var keys = new HashSet<Term>();
            foreach (Clause clause in clauses.Take(clauses.Count - 2))
            {
                if (clause is not { Guard: null, Patterns: [Erlang.Pattern.Literal { Value: Atom key }] }
                    || !keys.Add(key) || !AttributeLiteral.TryNormalizeConstant(clause.Body, out Term value)
                    || !value.Equals(result))
                {
                    return false;
                }
            }

            var registers = new Dictionary<string, int>(StringComparer.Ordinal) { [fallback.Name] = 0 };
            var tail = ReadEntryCall(call, [fallback.Name], allowControlTransfer: true);
            if (tail is null && !TryEmitRegisterConstructedArgument(
                call,
                registers,
                validateOnly: true,
                allowControlTransfer: true
            ))
            {
                return false;
            }

            int integer = Label();
            int accepted = Label();
            int rejected = Label();
            var order = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            owner.EmitMembershipSelect(0, integer, keys.Order(order).Select(key => (key, accepted)));
            Mark(integer);
            owner.Op(IsInteger, LabelTag, rejected);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsGreaterOrEqual, LabelTag, rejected);
            owner.EmitTypedRegister(0, IntegerTypeBits);
            owner.LiteralOperand(new Integer(bound.Value + (comparison.Operator == ErlangOperators.Greater ? 1 : 0)));
            Mark(accepted);
            owner.EmitConstant(result, 0);
            owner.instructions.WriteByte(Return);
            Mark(rejected);
            if (tail is not null)
            {
                EmitRegisterTailCall(tail);
            }
            else
            {
                TryEmitRegisterConstructedArgument(call, registers, allowControlTransfer: true);
            }

            return true;
        }
    }
}
