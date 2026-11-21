using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record OptionalPairEnumeration(Expr.Case Decision, Atom Missing, Term Default);

        private sealed record RequiredPairEnumeration(
            IReadOnlyList<(Atom Key, Expr.Call Call, Term[] Arguments)> Branches,
            Expr.Call Failure,
            List<Atom> Tags,
            int Entry
        );

        private RequiredPairEnumeration? ReadRequiredPairEnumeration(
            Expr expression,
            string source,
            IReadOnlySet<string> bindings
        )
        {
            if (expression is not Expr.Case { Value: Expr.Variable tested, Clauses.Count: >= 3 } decision
                || tested.Name != source || decision.Clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable rejected], Body: var failure
                }
                || rejected.Name == VariableScopeNames.Wildcard || bindings.Contains(rejected.Name)
                || ReadTaggedExit(failure, rejected.Name) is not { } error || !IsDecoderIntrinsic(error.Call))
            {
                return null;
            }

            var keys = new HashSet<Atom>();
            var branches = new List<(Atom Key, Expr.Call Call, Term[] Arguments)>();
            int entry = 0;
            foreach (var clause in decision.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Atom key }],
                        Body: Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 3 } call
                    }
                    || !keys.Add(key) || !entries.TryGetValue((call.Function, 3), out int target)
                    || entry != 0 && target != entry
                    || !ProducesNumericPair(call))
                {
                    return null;
                }

                var arguments = new Term[call.Arguments.Count];
                for (int index = 0; index < arguments.Length; index++)
                {
                    if (!AttributeLiteral.TryNormalizeConstant(call.Arguments[index], out arguments[index]))
                    {
                        return null;
                    }
                }

                entry = target;
                branches.Add((key, call, arguments));
            }

            return new(
                branches,
                error.Call,
                error.Tags,
                entry
            );
        }

        private void EmitRequiredPairEnumeration(RequiredPairEnumeration plan, int slot, int failure)
        {
            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var branches = plan.Branches.OrderByDescending(branch => branch.Key, order)
                .Select(branch => (Branch: branch, Label: Label())).ToArray();
            int joined = Label();
            EmitAritySelect(0, branches.Length, failure);
            foreach (var item in branches.Reverse())
            {
                owner.LiteralOperand(item.Branch.Key);
                owner.Operand(LabelTag, item.Label);
            }

            for (int index = 0; index < branches.Length; index++)
            {
                var (branch, label) = branches[index];
                Mark(label);
                foreach (int argument in IndependentCallMoveOrder(branch.Arguments.Length))
                {
                    owner.EmitConstant(branch.Arguments[argument], argument);
                }

                owner.EmitCallLine(branch.Call, location);
                owner.EmitFieldCall(branch.Arguments.Length, plan.Entry);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slot
                );
                if (index + 1 < branches.Length)
                {
                    JumpTo(joined);
                }
            }

            Mark(joined);
        }

        private static OptionalPairEnumeration? ReadOptionalPairEnumeration(Expr value)
        {
            if (value is not Expr.Case
                {
                    Value: Expr.Variable field,
                    Clauses:
                    [
                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom absent }], Body: var empty },
                    {
                        Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                        Body: Expr.Case { Value: Expr.Variable tested } decision
                    }
                    ]
                }
                || tested.Name != field.Name
                || !AttributeLiteral.TryNormalizeConstant(empty, out Term omitted)
                || omitted is not TupleTerm { Items.Count: 2 }
                || decision.Clauses.SkipLast(1).Any(branch => branch.Patterns is [Pattern.Literal literal]
                    && literal.Value.Equals(absent)))
            {
                return null;
            }

            return new(decision, absent, omitted);
        }

        private (int Label, Expr.Call Call, List<Atom> Tags) EmitOptionalPairEnumeration(
            OptionalPairEnumeration plan,
            int source,
            int failure
        )
        {
            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var branches = plan.Decision.Clauses.SkipLast(1)
                .Select(clause => (Key: (Atom)((Pattern.Literal)clause.Patterns[0]).Value, Call: (Expr.Call?)clause.Body))
                .Append((Key: plan.Missing, Call: null))
                .OrderByDescending(branch => branch.Key, order)
                .Select(branch => (branch.Key, branch.Call, Label: Label())).ToArray();
            int joined = Label();
            owner.Op(SelectValue, YRegisterTag, source);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, branches.Length * 2);
            foreach (var branch in branches.Reverse())
            {
                owner.LiteralOperand(branch.Key);
                owner.Operand(LabelTag, branch.Label);
            }

            for (int index = 0; index < branches.Length; index++)
            {
                var branch = branches[index];
                Mark(branch.Label);
                if (branch.Call is { } call)
                {
                    foreach (int argument in IndependentCallMoveOrder(call.Arguments.Count))
                    {
                        if (argument == 0)
                        {
                            owner.InitStackSlot(source);
                        }

                        owner.EmitConstant(call.Arguments[argument], argument);
                    }

                    owner.EmitCallLine(call, location);
                    Call(call.Module, call.Function, call.Arguments.Count);
                }
                else
                {
                    owner.EmitConstant(plan.Default, 0);
                }

                if (index + 1 < branches.Length)
                {
                    JumpTo(joined);
                }
            }

            Mark(joined);
            Clause fallback = plan.Decision.Clauses[^1];
            var error = ReadTaggedExit(fallback.Body, ((Pattern.Variable)fallback.Patterns[0]).Name)
                ?? throw new NotSupportedException(Unsupported);

            return (failure, error.Call, error.Tags);
        }
    }
}
