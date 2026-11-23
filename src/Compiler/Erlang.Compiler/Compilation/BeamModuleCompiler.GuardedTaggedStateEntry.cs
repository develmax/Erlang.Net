using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record GuardedTaggedStateEntry(
            Clause Clause,
            Atom Role,
            Atom[] Keys,
            int PayloadField,
            Dictionary<string, int> LiveRegisters
        );

        private static GuardedTaggedStateEntry? ReadGuardedTaggedStateEntry(IReadOnlyList<Clause> clauses, int input)
        {
            int arity = clauses[0].Patterns.Count;
            int state = input + 1;
            if (state >= arity || arity + 1 >= FirstReservedXRegister || clauses is not
                [
                {
                    Guard: { } guard,
                    Body: Expr.Sequence { Items: [Expr.Case, Expr.Literal { Value: Atom }] }
                } clause
                ]
                || clause.Patterns[input] is not global::Erlang.Pattern.Tuple message || !HasTupleTag(message, out _)
                || clause.Patterns[state] is not global::Erlang.Pattern.Tuple
                {
                    Items: [global::Erlang.Pattern.Variable value, global::Erlang.Pattern.Literal { Value: Atom role }]
                }
                || value.Name == VariableScopeNames.Wildcard
                || LocalFunctionEntries.Walk(clause.Body).Any(expression =>
                    expression is Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe or Expr.Fun))
            {
                return null;
            }

            var keys = new List<Atom>();
            if (!ReadAtomGuardKeys(guard, value.Name, keys) || keys.Count < 3 || keys.Distinct().Count() != keys.Count)
            {
                return null;
            }

            int payload = -1;
            var bindings = new Dictionary<string, int>(StringComparer.Ordinal) { [value.Name] = state };
            for (int field = 1; field < message.Items.Count; field++)
            {
                if (IsSharedEntryWildcard(message.Items[field]))
                {
                    continue;
                }

                if (payload != -1 || message.Items[field] is not global::Erlang.Pattern.Variable variable
                    || !bindings.TryAdd(variable.Name, arity + 1))
                {
                    return null;
                }

                payload = field;
            }

            if (payload < 0)
            {
                return null;
            }

            for (int register = 0; register < arity; register++)
            {
                if (register == input || register == state || IsSharedEntryWildcard(clause.Patterns[register]))
                {
                    continue;
                }

                if (clause.Patterns[register] is not global::Erlang.Pattern.Variable parameter
                    || !bindings.TryAdd(parameter.Name, register))
                {
                    return null;
                }
            }

            var parameters = bindings.Keys.Select(name => new global::Erlang.Pattern.Variable(name)).ToArray();
            if (ReadUsedEntryParameters(clause with { Patterns = parameters, Guard = null }) is not { } used
                || used.Contains(value.Name) || message.Items[payload] is not global::Erlang.Pattern.Variable usedPayload
                || !used.Contains(usedPayload.Name))
            {
                return null;
            }

            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var live = bindings.Where(binding => used.Contains(binding.Key))
                .ToDictionary(binding => binding.Key, binding => binding.Value, StringComparer.Ordinal);

            return new(
                clause,
                role,
                keys.Order(order).ToArray(),
                payload,
                live
            );
        }

        private void EmitGuardedTaggedStateEntry(
            GuardedTaggedStateEntry plan,
            int state,
            int failure,
            Dictionary<int, int> frames,
            List<(int Label, BeamSourceLocation Location)> recordFailures,
            List<CheckedRecordEffectExits> checkedEffectExits,
            ref int nextFrameKey
        )
        {
            int scratch = plan.Clause.Patterns.Count;
            EmitTupleShapeTest(
                XRegisterTag,
                state,
                2,
                failure
            );
            owner.EmitTuplePart(state, 1, scratch);
            owner.EmitAtomTest(scratch, plan.Role.Name, failure);
            owner.EmitTuplePart(state, 0, scratch);
            int accepted = Label();
            owner.EmitMembershipSelect(scratch, failure, plan.Keys.Select(key => ((Term)key, accepted)));
            Mark(accepted);

            int frameKey = nextFrameKey--;
            if (TryEmitCheckedRecordBooleanEffect(plan, checkedEffectExits))
            {
                frames.Add(frameKey, 0);

                return;
            }

            int previousFrame = frameSize;
            int previousPeak = peakSlot;
            int previousSlot = nextSlot;
            var previousVariables = variables;
            var previousRecords = checkedRecordBindings;
            frameSize = plannedTupleFrames?.GetValueOrDefault(frameKey, previousFrame) ?? previousFrame;
            nextSlot = 0;
            peakSlot = 0;
            variables = new(StringComparer.Ordinal);
            checkedRecordBindings = new(StringComparer.Ordinal);

            EmitNestedTupleLeaf(
                new(
                    plan.Clause,
                    0,
                    plan.Role,
                    plan.LiveRegisters,
                    [],
                    null
                ),
                frameSize,
                recordFailures,
                checkedPatterns: true
            );
            frames.Add(frameKey, peakSlot);
            frameSize = previousFrame;
            peakSlot = Math.Max(previousPeak, peakSlot);
            nextSlot = previousSlot;
            variables = previousVariables;
            checkedRecordBindings = previousRecords;
        }
    }
}
