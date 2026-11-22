using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool UsesAutomaticCoreInlining => automaticCoreInlining;

    private sealed partial class GeneralEmitter
    {
        private sealed record ClosedClosureTupleArm(
            Atom Key,
            Term Value,
            Expr.Fun? Callback,
            int Ordinal
        );

        private bool TryEmitClosedAtomClosureTuples(FunctionDefinition function, int failure)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.UsesAutomaticCoreInlining
                || function.Arity == 0 || function.Clauses.Count < 3 || owner.IsDeclaredNative((function.Name, function.Arity)))
            {
                return false;
            }

            var keys = new HashSet<Atom>();
            var arms = new List<ClosedClosureTupleArm>();
            int ordinal = 0;
            foreach (Clause clause in function.Clauses)
            {
                if (clause is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }, ..] }
                    || !keys.Add(key)
                    || !HasUnconditionalParameters(clause with { Patterns = clause.Patterns.Skip(1).ToArray() }))
                {
                    return false;
                }

                if (AttributeLiteral.TryNormalizeConstant(clause.Body, out Term constant))
                {
                    arms.Add(new(
                        key,
                        constant,
                        null,
                        -1
                    ));
                    continue;
                }

                if (clause.Body is not Expr.Tuple
                    {
                        Items: [
                            var head,
                            Expr.Fun
                        {
                            SelfName: null,
                            Clauses: [{ Guard: null, Body: Expr.Variable returned } identity]
                        } callback
                        ]
                    } || !AttributeLiteral.TryNormalizeConstant(head, out Term literal)
                    || !HasUnconditionalParameters(identity) || returned.Name == VariableScopeNames.Wildcard
                    || !identity.Patterns.OfType<Pattern.Variable>().Any(formal => formal.Name == returned.Name))
                {
                    return false;
                }

                arms.Add(new(
                    key,
                    literal,
                    callback,
                    ordinal++
                ));
            }

            var descending = arms.OrderByDescending(arm => arm.Key, Comparer<Atom>.Create(
                (left, right) => TermOrder.Compare(left, right, exact: true)
            )).ToArray();
            // The incoming type and closure source order must remain stable
            // when independent terminal arms are emitted in reverse key order.
            if (ordinal < 2 || !descending.Where(arm => arm.Callback is not null).Select(arm => arm.Ordinal)
                    .SequenceEqual(Enumerable.Range(0, ordinal))
                || ReadFiniteAtomArguments(function, 0, admitGuardFacts: true) is not null
                || ReadAtomEntryType(function, 0) != 0)
            {
                return false;
            }

            var scheduled = descending.Select(arm => (Arm: arm, Label: Label())).ToArray();
            owner.EmitMembershipSelect(0, failure, scheduled.Reverse().Select(step => ((Term)step.Arm.Key, step.Label)));
            variables = new(StringComparer.Ordinal);
            foreach (var (arm, label) in scheduled)
            {
                Mark(label);
                if (arm.Callback is { } callback)
                {
                    owner.EmitClosureAllocation(3, 0, 1);
                    EmitGeneralClosure(callback with { CoreOrdinal = callback.CoreOrdinal ?? arm.Ordinal }, heapChecked: true);
                    owner.Op(PutTuple, XRegisterTag, 0);
                    owner.instructions.WriteByte(ExtendedListOperand);
                    owner.Operand(BeamWire.UnsignedTag, 2);
                    owner.LiteralOperand(arm.Value);
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    owner.EmitConstant(arm.Value, 0);
                }

                owner.instructions.WriteByte(Return);
            }

            return true;
        }
    }
}
