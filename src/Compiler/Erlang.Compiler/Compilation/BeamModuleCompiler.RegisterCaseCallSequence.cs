using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const string BeamSystemInfoFunction = "system_info";

        private sealed record RegisterCaseCallSequence(
            SavedCallStep[] Calls,
            Dictionary<int, int> LastReads,
            CaseTuplePlan? ReturnTuple, Term? ReturnConstant, bool KeepFrame);

        private RegisterCaseCallSequence? ReadRegisterCaseCallSequence(
            Expr expression,
            string? selector,
            IReadOnlyDictionary<string, int> saved,
            bool allowConstant = false,
            int selectorRegister = 0,
            int extraSlots = 0
        )
        {
            IReadOnlyList<Expr> statements;
            Expr.Tuple? tuple = null;
            Term? constant = null;
            if (expression is Expr.Sequence { Items: [_, .., Expr.Call or Expr.Tuple] } sequence)
            {
                tuple = sequence.Items[^1] as Expr.Tuple;
                statements = tuple is null ? sequence.Items : sequence.Items.SkipLast(1).ToArray();
            }
            else if (allowConstant && expression is Expr.Sequence { Items.Count: > 1 } effects
                && AttributeLiteral.TryNormalizeConstant(effects.Items[^1], out Term returnedConstant))
            {
                constant = returnedConstant;
                statements = effects.Items.SkipLast(1).ToArray();
            }
            else if (expression is Expr.Call)
            {
                statements = [expression];
            }
            else
            {
                return null;
            }

            var flattened = FlattenCallArguments(statements);
            if (flattened.Count == 1 && expression is Expr.Call)
            {
                return null;
            }

            int savedSlots = saved.Values.DefaultIfEmpty(-1).Max() + 1;
            var names = Enumerable.Repeat(VariableScopeNames.Wildcard, savedSlots + extraSlots).ToArray();
            foreach (var (name, slot) in saved)
            {
                names[slot] = name;
            }

            string[] bindings = selector is null ? names : [.. names, selector];
            if (ReadStraightCalls(flattened, bindings) is not { } calls)
            {
                return null;
            }

            var lastUses = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < calls.Length; index++)
            {
                RecordReads(calls[index].Call, index);
            }

            if (tuple is not null)
            {
                RecordReads(tuple, calls.Length);
            }

            if (selectorRegister != 0 && (selector is null || lastUses.GetValueOrDefault(selector, -1) != 0))
            {
                return null;
            }

            var availableSlots = saved.Values.Distinct().Order().Concat(Enumerable.Range(savedSlots, extraSlots)).ToArray();
            var active = saved.Where(binding => lastUses.ContainsKey(binding.Key))
                .ToDictionary(binding => binding.Key, binding => binding.Value, StringComparer.Ordinal);
            var plans = new SavedCallStep[calls.Length];
            var lastReads = new Dictionary<int, int>();
            string? previous = selector;
            for (int index = 0; index < calls.Length; index++)
            {
                var (call, result) = calls[index];
                Array.Fill(names, VariableScopeNames.Wildcard);
                foreach (var (name, slot) in active)
                {
                    names[slot] = name;
                }

                string[] registerInputs = previous is null ? [] : [previous];
                if (index == 0 && previous is not null && selectorRegister != 0)
                {
                    registerInputs = Enumerable.Repeat(VariableScopeNames.Wildcard, selectorRegister + 1).ToArray();
                    registerInputs[selectorRegister] = previous;
                }

                if ((ReadEntryCall(call, registerInputs, names)
                    ?? (registerInputs.Length <= 1 ? ReadStraightConstructedCall(call, previous, active) : null)) is not { } step)
                {
                    return null;
                }

                foreach (var (name, slot) in active)
                {
                    lastReads[slot] = lastUses[name];
                }

                var released = active.Where(binding => lastUses[binding.Key] == index).ToArray();
                var occupied = active.Values.ToHashSet();
                int? reuseSlot = previous is not null && lastUses.GetValueOrDefault(previous, -1) > index
                    ? FindReusableCallSlot(
                        ref step,
                        previous,
                        active,
                        released.Select(binding => binding.Value)
                    )
                    : null;
                foreach (var (name, _) in released)
                {
                    active.Remove(name);
                }

                int? spill = null;
                bool reuse = false;
                if (previous is not null && lastUses.GetValueOrDefault(previous, -1) > index)
                {
                    int slot = reuseSlot ?? availableSlots.FirstOrDefault(candidate => !occupied.Contains(candidate), -1);
                    if (slot < 0)
                    {
                        return null;
                    }

                    reuse = occupied.Contains(slot);
                    spill = slot;
                    active.Add(previous, slot);
                    lastReads[slot] = lastUses[previous];
                }

                plans[index] = new SavedCallStep(
                    step,
                    spill,
                    reuse,
                    []
                );
                previous = result;
            }

            var returned = tuple is null ? null : ReadCaseTuple(tuple, previous, active);

            return tuple is null || returned is not null ? new RegisterCaseCallSequence(plans, lastReads, returned, constant, KeepFrame: extraSlots != 0) : null;

            void RecordReads(Expr value, int index)
            {
                foreach (Expr.Variable read in LocalFunctionEntries.Walk(value).OfType<Expr.Variable>())
                {
                    lastUses[read.Name] = index;
                }
            }
        }

        private void EmitRegisterCaseCallSequence(
            RegisterCaseCallSequence sequence,
            BeamSourceLocation previous,
            int frame,
            bool compact = false
        )
        {
            compact &= sequence.Calls.All(step => step.Spill is null);
            var slots = Enumerable.Range(0, frame).ToArray();
            var remaining = sequence.LastReads.ToDictionary(read => read.Key, read => read.Value);
            var cleared = new HashSet<int>();
            int trimmed = 0;
            for (int index = 0; index < sequence.Calls.Length; index++)
            {
                SavedCallStep step = sequence.Calls[index];
                EntryCall call = step.Call;
                EmitSavedCallArguments(step, slots);

                if (sequence is { ReturnTuple: null, ReturnConstant: null } && index == sequence.Calls.Length - 1)
                {
                    EmitPreparedFrameTail(call, previous, frame - trimmed);
                    break;
                }

                foreach (int expired in remaining.Where(read => read.Value == index).Select(read => read.Key).ToArray())
                {
                    remaining.Remove(expired);
                }

                var live = remaining.Keys.Select(slot => slots[slot]).ToHashSet();
                if (compact && live.Count == 1 && live.Single() is int retained
                    && retained < frame - trimmed - 1)
                {
                    int destination = frame - trimmed - 1;
                    owner.MoveRegister(YRegisterTag, retained, YRegisterTag, destination);
                    int logical = remaining.Keys.Single();
                    slots[logical] = destination;
                    live.Remove(retained);
                    live.Add(destination);
                }
                int remove = 0;
                while (!sequence.KeepFrame && remove < frame - trimmed && !live.Contains(remove))
                {
                    remove++;
                }

                int[] dead = Enumerable.Range(remove, frame - trimmed - remove)
                    .Where(slot => !sequence.KeepFrame && !live.Contains(slot) && cleared.Add(slot + trimmed)).ToArray();
                if (dead.Length != 0)
                {
                    owner.EmitSlotKills(dead);
                }

                if (remove != 0)
                {
                    trimmed += remove;
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, remove);
                    owner.Operand(BeamWire.UnsignedTag, frame - trimmed);
                    for (int slot = 0; slot < slots.Length; slot++)
                    {
                        slots[slot] -= remove;
                    }
                }

                // OTP omits the call-site line for this non-failing BIF.
                if (call.Module != ErlangModuleNames.Module
                    || (call.Call.Function, call.Call.Arguments.Count) != (AutoImportNames.Processes, 0))
                {
                    owner.EmitCallLine(call.Call, previous);
                    previous = CallLocation(call.Call, previous);
                }

                Call(call.Module, call.Call.Function, call.Call.Arguments.Count);
            }

            if (sequence.ReturnTuple is { } tuple)
            {
                EmitCaseTuple(tuple, trimmed: trimmed);
                owner.EmitFrameReturn(frame - trimmed);
            }
            else if (sequence.ReturnConstant is { } constant)
            {
                owner.EmitConstant(constant, 0);
                owner.EmitFrameReturn(frame - trimmed);
            }
        }
    }
}
