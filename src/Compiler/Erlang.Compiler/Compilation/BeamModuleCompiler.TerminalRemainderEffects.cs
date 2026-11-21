using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RemainderEffect(EntryCall Primary, EntryCall? Preparation);

        private bool TryEmitTerminalRemainderEffects(Clause clause)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Patterns.Count < 2 || clause.Patterns.Count >= FirstReservedXRegister
                || clause.Body is not Expr.Sequence { Items: [Expr.Call first, _, .., Expr.Case decision] } sequence
                || decision.Value is not Expr.Binary
                {
                    Operator: ErlangOperators.Remainder,
                    Left: Expr.Variable dividend,
                    Right: Expr.Literal { Value: Integer divisor },
                } arithmetic
                || divisor.Value <= 1 || divisor.Value > long.MaxValue
                || decision.Clauses.Count < 4 || decision.Clauses[^1].Patterns is not
                    [Pattern.Variable { Name: VariableScopeNames.Wildcard }])
            {
                return false;
            }

            var arguments = clause.Patterns.Cast<Pattern.Variable>().Select(variable => variable.Name).ToArray();
            if (!arguments.Contains(dividend.Name) || arguments.Contains(VariableScopeNames.Wildcard)
                || ReadEntryCall(first, arguments) is not { Module: not null, ConstructedArgument: null, SavedInputs: null, FunctionArgument: null, RegisterConstructions: null } entry
                || !TryResolveOrdinaryCallTarget(first, out _))
            {
                return false;
            }

            string[] saved = arguments.AsEnumerable().Reverse().ToArray();
            if (ReadRemainderEffects(sequence.Items.Skip(1).SkipLast(1), saved) is not { Length: > 0 } prelude)
            {
                return false;
            }

            var arms = new SortedDictionary<int, RemainderEffect[]>();
            RemainderEffect[]? other = null;
            long maximum = (long)divisor.Value - 1;
            foreach (var arm in decision.Clauses)
            {
                IReadOnlyList<Expr> expressions = arm.Body is Expr.Sequence inner ? inner.Items : [arm.Body];
                if (arm.Guard is not null || ReadRemainderEffects(expressions, saved) is not { Length: > 0 } effects)
                {
                    return false;
                }

                if (arm.Patterns is [Pattern.Literal { Value: Integer literal }]
                    && literal.Value >= Math.Max(-maximum, int.MinValue) && literal.Value <= Math.Min(maximum, int.MaxValue))
                {
                    if (other is not null || !arms.TryAdd((int)literal.Value, effects))
                    {
                        return false;
                    }
                }
                else if (arm.Patterns is [Pattern.Variable { Name: VariableScopeNames.Wildcard }] && other is null)
                {
                    other = effects;
                }
                else
                {
                    return false;
                }
            }

            if (other is null || arms.Count < 3
                || arms.Values.Any(effects => effects.SelectMany(ReadEffectSlots).Distinct().Count() >= saved.Length)
                || other.SelectMany(ReadEffectSlots).Distinct().Count() != saved.Length)
            {
                return false;
            }

            var slots = Enumerable.Range(0, saved.Length).ToList();
            EmitFrameAllocation(arguments.Length, saved.Length);
            for (int index = 0; index < saved.Length; index++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    saved.Length - 1 - index,
                    YRegisterTag,
                    index
                );
            }

            EmitEntryCall(entry);
            var previous = CallLocation(first, location);
            foreach (var effect in prelude)
            {
                previous = EmitRemainderEffect(
                    effect,
                    slots,
                    null,
                    previous,
                    tail: false
                );
            }

            var prior = variables;
            variables = saved.Select((name, index) => (name, index))
                .ToDictionary(value => value.name, value => value.index, StringComparer.Ordinal);
            EmitArithmetic(arithmetic, guardFailure: null, previousLine: previous);
            variables = prior;

            int otherwise = Label();
            var targets = arms.Reverse().Select(arm => (arm.Key, arm.Value, Label: Label())).ToArray();
            owner.instructions.WriteByte(SelectValue);
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                lower: -maximum,
                upper: maximum
            );
            owner.Operand(LabelTag, otherwise);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, targets.Length * 2);
            foreach (var arm in targets.Reverse())
            {
                owner.Operand(BeamWire.IntegerTag, arm.Key);
                owner.Operand(LabelTag, arm.Label);
            }

            foreach (var arm in targets)
            {
                Mark(arm.Label);
                EmitRemainderArm(arm.Value, slots);
            }

            Mark(otherwise);
            EmitRemainderArm(other, slots);

            return true;
        }

        private RemainderEffect[]? ReadRemainderEffects(IEnumerable<Expr> expressions, string[] saved)
        {
            const string prepared = "\0prepared-remainder-effect";
            var effects = new List<RemainderEffect>();
            foreach (Expr expression in expressions)
            {
                if (expression is not Expr.Call { Module: not null, DynamicRemote: false } call
                    || !TryResolveOrdinaryCallTarget(call, out _))
                {
                    return null;
                }

                EntryCall? operand = null;
                Expr.Call target = call;
                if (call.Arguments.Count != 0 && call.Arguments[^1] is Expr.Call nested)
                {
                    if (nested.Module is null || !TryResolveOrdinaryCallTarget(nested, out _)
                        || !nested.Arguments.All(argument => AttributeLiteral.TryNormalizeConstant(argument, out _))
                        || ReadEntryCall(nested, []) is not { Module: not null, ConstructedArgument: null } preparation)
                    {
                        return null;
                    }

                    operand = preparation;
                    target = call with { Arguments = [.. call.Arguments.Take(call.Arguments.Count - 1), new Expr.Variable(prepared)] };
                }

                if (ReadEntryCall(target, operand is null ? [] : [prepared], saved) is not
                    { Module: not null, ConstructedArgument: null, RegisterConstructions: null, FunctionArgument: null } primary)
                {
                    return null;
                }

                effects.Add(new(primary with { Call = call }, operand));
            }

            return effects.ToArray();
        }

        private static IEnumerable<int> ReadEffectSlots(RemainderEffect effect) =>
            (effect.Primary.SavedInputs ?? []).Select(load => load.Source);

        private void EmitRemainderArm(RemainderEffect[] effects, List<int> original)
        {
            var slots = original.ToList();
            TrimRemainderFrame(slots, effects.SelectMany(ReadEffectSlots).ToHashSet());
            var previous = location;
            for (int index = 0; index < effects.Length; index++)
            {
                bool tail = index == effects.Length - 1;
                var remaining = tail ? null : effects.Skip(index + 1).SelectMany(ReadEffectSlots).ToHashSet();
                previous = EmitRemainderEffect(
                    effects[index],
                    slots,
                    remaining,
                    previous,
                    tail
                );
            }
        }

        private BeamSourceLocation EmitRemainderEffect(
            RemainderEffect effect,
            List<int> slots,
            HashSet<int>? remaining,
            BeamSourceLocation previous,
            bool tail
        )
        {
            if (effect.Preparation is { } operand)
            {
                EmitEntryCall(operand, previous);
                previous = CallLocation(operand.Call, previous);
            }

            var primary = effect.Primary;
            var loads = (primary.SavedInputs ?? []).Select(load => (load.Destination, Source: slots.IndexOf(load.Source))).ToList();
            int? receiver = primary.SavedInputs?.Where(load => load.Destination == 0).Select(load => (int?)load.Source).SingleOrDefault();
            bool trim = remaining is not null && slots.Any(slot => !remaining.Contains(slot));
            bool deferReceiver = trim && receiver is int receiverSlot && remaining!.Contains(receiverSlot);
            owner.EmitOrderedTailMoves(
                primary.Moves,
                primary.Constants,
                loads,
                deferX0StackLoad: deferReceiver
            );
            if (trim)
            {
                TrimRemainderFrame(slots, remaining!);
                if (deferReceiver)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        slots.IndexOf(receiver!.Value),
                        XRegisterTag,
                        0
                    );
                }
            }

            owner.EmitCallLine(primary.Call, previous);
            if (tail)
            {
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, primary.Call.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(primary.Module!, primary.Call.Function, primary.Call.Arguments.Count));
                owner.Operand(BeamWire.UnsignedTag, slots.Count);
            }
            else
            {
                Call(primary.Module, primary.Call.Function, primary.Call.Arguments.Count);
            }

            return CallLocation(primary.Call, previous);
        }

        private void TrimRemainderFrame(List<int> slots, HashSet<int> live)
        {
            int remove = slots.Count(slot => !live.Contains(slot));
            if (remove == 0)
            {
                return;
            }

            var free = new Queue<int>(Enumerable.Range(remove, slots.Count - remove).Where(index => !live.Contains(slots[index])));
            for (int source = 0; source < remove; source++)
            {
                if (live.Contains(slots[source]))
                {
                    int destination = free.Dequeue();
                    owner.MoveRegister(
                        YRegisterTag,
                        source,
                        YRegisterTag,
                        destination
                    );
                    slots[destination] = slots[source];
                }
            }

            slots.RemoveRange(0, remove);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, remove);
            owner.Operand(BeamWire.UnsignedTag, slots.Count);
        }
    }
}
