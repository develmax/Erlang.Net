using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDisjointRangePredicate(FunctionDefinition function)
        {
            var clauses = function.Clauses;
            if (function.Arity != 1 || clauses.Count < 6
                || clauses[^1] is not
                {
                    Guard: null, Patterns: [Erlang.Pattern.Variable],
                    Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } rejected }
                })
            {
                return false;
            }

            var keys = new HashSet<Term>();
            foreach (Clause clause in clauses.Take(clauses.Count - 4))
            {
                if (clause is not
                    {
                        Guard: null, Patterns: [Erlang.Pattern.Literal { Value: Integer key }],
                        Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }
                    }
                    || !keys.Add(key))
                {
                    return false;
                }
            }

            var ranges = new List<(Integer Lower, Integer Upper)>();
            foreach (Clause clause in clauses.Skip(clauses.Count - 4).Take(3))
            {
                if (clause is not
                    {
                        Guard: { } guard, Patterns: [Erlang.Pattern.Variable input],
                        Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }
                    }
                    || input.Name == VariableScopeNames.Wildcard
                    || PredicateConjuncts(guard).ToArray() is not
                    [
                        Expr.Binary { Operator: ErlangOperators.GreaterOrEqual,
                            Left: Expr.Variable start, Right: Expr.Literal { Value: Integer lower } },
                        Expr.Binary { Operator: ErlangOperators.LessOrEqual,
                            Left: Expr.Variable end, Right: Expr.Literal { Value: Integer upper } }
                    ]
                    || start.Name != input.Name || end.Name != input.Name
                    || lower.Value < long.MinValue || upper.Value > long.MaxValue
                    || lower.Value >= upper.Value || ranges.Count > 0 && ranges[^1].Upper.Value >= lower.Value)
                {
                    return false;
                }

                ranges.Add((lower, upper));
            }

            int rangeEntry = Label();
            int success = Label();
            int finalRange = Label();
            int failure = Label();
            var order = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            owner.EmitMembershipSelect(0, rangeEntry, keys.Order(order).Select(key => (key, success)));

            Mark(rangeEntry);
            owner.Op(IsGreaterOrEqual, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(ranges[0].Lower);
            owner.Op(IsLess, LabelTag, success);
            owner.LiteralOperand(ranges[0].Upper);
            owner.EmitTypedRegister(0, AnyTypeBits, lower: (long)ranges[0].Lower.Value);
            EmitTypedLowerBound(ranges[1].Lower, ranges[0].Upper);
            owner.Op(IsGreaterOrEqual, LabelTag, finalRange);
            owner.LiteralOperand(ranges[1].Upper);
            owner.EmitTypedRegister(0, AnyTypeBits, lower: (long)ranges[1].Lower.Value);

            Mark(success);
            owner.EmitConstant(Term.A(ErlangBooleanAtoms.True), 0);
            owner.instructions.WriteByte(Return);
            Mark(finalRange);
            EmitTypedLowerBound(ranges[2].Lower, ranges[1].Upper);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.GreaterOrEqual, 2));
            owner.LiteralOperand(ranges[2].Upper);
            owner.EmitTypedRegister(0, AnyTypeBits, lower: (long)ranges[2].Lower.Value);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitConstant(rejected, 0);
            owner.instructions.WriteByte(Return);

            return true;

            void EmitTypedLowerBound(Integer bound, Integer previousUpper)
            {
                owner.Op(IsGreaterOrEqual, LabelTag, failure);
                owner.EmitTypedRegister(0, AnyTypeBits, lower: (long)previousUpper.Value);
                owner.LiteralOperand(bound);
            }
        }
    }
}
