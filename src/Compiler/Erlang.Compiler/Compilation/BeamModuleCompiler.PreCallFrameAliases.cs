using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? EliminatePreCallFrameAliases(
        ReadOnlyMemory<byte> code,
        int labelCount,
        bool allowCollectors = true
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Move)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, operands) = decoded;
        var edits = new List<CodeEdit>();
        var entries = new HashSet<int>();
        BeamProjectionFlow? flow = null;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (start >= end || body[start] is not { Definition.Opcode: Label, Operands: [var label] })
            {
                continue;
            }

            if (ReadPreCallFrameAliases(
                body,
                start,
                end,
                allowCollectors
            ) is not { } plan
                || ReadDisjointFrameOwnership(body, start, end) is not { } frame || frame.Uses.Count != 1)
            {
                continue;
            }

            int allocation = frame.Uses.Keys.Single();
            if (frame.Owners[plan.Call - start] != allocation
                || !HasRetainedPreCallRoot(
                    body,
                    flow ??= new BeamProjectionFlow(body),
                    plan.Call,
                    end
                ))
            {
                continue;
            }

            var aliases = plan.Aliases.Where(alias =>
                frame.Owners[alias.Value.Spill - start] == allocation
                && frame.Owners[alias.Value.LastUse - start] == allocation)
                .ToDictionary();
            var collectorRoots = PlanPreCallCollectorRoots(body, aliases);
            if (aliases.Count == 0)
            {
                continue;
            }

            entries.Add((int)label.Value);
            var spills = aliases.Values.Select(alias => alias.Spill).ToHashSet();
            int operand = FirstStackOperandAtOrAfter(operands, body[start].Offset);
            for (int index = start; index < end; index++)
            {
                var instruction = body[index];
                int next = body[index + 1].Offset;
                if (collectorRoots.TryGetValue(index, out int live))
                {
                    int from = instruction.Offset + 1;
                    var cursor = new BeamCursor(code[from..next]);
                    var first = BeamCompactReader.Read(cursor);
                    if (first.WireTag == ExtendedTag && first.Value == AllocationList)
                    {
                        int count = (int)BeamCompactReader.Read(cursor).Value;
                        for (int item = 0; item < count; item++)
                        {
                            _ = BeamCompactReader.Read(cursor);
                            _ = BeamCompactReader.Read(cursor);
                        }
                    }

                    int liveStart = from + cursor.Position;
                    _ = BeamCompactReader.Read(cursor);
                    edits.Add(new(
                        liveStart,
                        from + cursor.Position,
                        BeamWire.UnsignedTag,
                        live
                    ));
                }

                bool removesMove = spills.Contains(index) || instruction is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } old, var target]
                }
                    && aliases.TryGetValue((int)old.Value, out var copy)
                    && target == copy.Source;
                if (removesMove)
                {
                    edits.Add(new(
                        instruction.Offset,
                        next,
                        0,
                        0,
                        IsRemoval: true
                    ));
                }

                while (operand < operands.Count && operands[operand].Start < next)
                {
                    var (from, to, slot) = operands[operand++];
                    if (!removesMove && instruction.Definition.Opcode != InitYRegisters
                        && aliases.TryGetValue(slot, out var alias))
                    {
                        edits.Add(new(
                            from,
                            to,
                            XRegisterTag,
                            (int)alias.Source.Value
                        ));
                    }
                }
            }
        }

        if (ApplyDuplicatedFrameReadEdits(code, edits) is not { } rewritten
            || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return null;
        }

        // The old initialization can omit a slot defined by the removed spill.
        // Publish the rewrite only together with removal of that physical slot.
        return RewriteDisjointFrames(
            rewritten,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );
    }

    private static Dictionary<int, int> PlanPreCallCollectorRoots(
        IReadOnlyList<BeamInstruction> body,
        Dictionary<int, (int Spill, int LastUse, BeamOperand Source, int Collector)> aliases
    )
    {
        var widened = new Dictionary<int, int>();
        var groups = aliases.Where(alias => alias.Value.Collector >= 0)
            .GroupBy(alias => alias.Value.Collector).ToArray();
        foreach (var group in groups)
        {
            int live = (int)body[group.Key].Operands[1].Value;
            int prefix = live;
            foreach (int register in group.Select(alias => (int)alias.Value.Source.Value).Distinct().Order())
            {
                if (register > prefix)
                {
                    break;
                }

                if (register == prefix)
                {
                    prefix++;
                }
            }

            // Every newly rooted register must replace an original Y root.
            foreach (var (slot, alias) in group)
            {
                if (alias.Source.Value >= prefix)
                {
                    aliases.Remove(slot);
                }
            }

            if (prefix > live)
            {
                widened.Add(group.Key, prefix);
            }
        }

        return widened;
    }

    private static (
        int Call,
        Dictionary<int, (int Spill, int LastUse, BeamOperand Source, int Collector)> Aliases
    )? ReadPreCallFrameAliases(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        bool allowCollectors
    )
    {
        if (ReadSinglePreCallFrameReferences(body, start, end) is not { } plan)
        {
            return null;
        }

        var (call, references) = plan;
        var aliases = new Dictionary<int, (int Spill, int LastUse, BeamOperand Source, int Collector)>();
        foreach (var (slot, (first, last, _)) in references)
        {
            if (last >= call || first == last || body[first] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                    ]
                }
                || saved.Value != slot || ReadPreCallAliasCollector(
                    body,
                    first,
                    last,
                    source,
                    saved,
                    allowCollectors
                ) is not { } collector)
            {
                continue;
            }

            aliases.Add(slot, (first, last, source, collector));
        }

        return aliases.Count == 0 ? null : (call, aliases);
    }

    private static (
        int Call,
        Dictionary<int, (int First, int Last, int Count)> References
    )? ReadSinglePreCallFrameReferences(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        int call = -1;
        var references = new Dictionary<int, (int First, int Last, int Count)>();
        for (int index = start; index < end; index++)
        {
            var instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case LocalCall:
                case ExternalCall:
                    if (call >= 0)
                    {
                        return null;
                    }

                    call = index;
                    break;
                case TryEnter:
                case CatchEnter:
                case CallFun:
                case CallFunTyped:
                case DynamicApply:
                case LoopReceive:
                case LoopReceiveEnd:
                case Wait:
                case WaitTimeout:
                case TrimFrame:
                case DebugLine:
                case ExecutableLine:
                    return null;
                case InitYRegisters:
                    continue;
            }

            foreach (var operand in instruction.Operands)
            {
                if (operand.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                int slot = (int)operand.Value;
                references[slot] = references.TryGetValue(slot, out var range)
                    ? (range.First, index, range.Count + 1)
                    : (index, index, 1);
            }
        }

        return call < 0 ? null : (call, references);
    }

    private static int? ReadPreCallAliasCollector(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int last,
        BeamOperand source,
        BeamOperand saved,
        bool allowCollectors
    )
    {
        int collector = -1;
        for (int index = first + 1; index <= last; index++)
        {
            var instruction = body[index];
            int writes = ReadPreCallAliasWriteMask(instruction);
            if (writes < 0)
            {
                byte opcode = instruction.Definition.Opcode;
                if (!allowCollectors || collector >= 0 || opcode is not
                    (TestHeap or GcBifOne or GcBifTwo or GcBifThree))
                {
                    return null;
                }

                collector = index;
                writes = opcode == TestHeap ? 0 : 1 << (instruction.Operands.Count - 1);
            }

            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                if (position >= sizeof(int) * 8 || (writes & (1 << position)) == 0)
                {
                    continue;
                }

                var operand = instruction.Operands[position];
                if (SameProjectionRegister(operand, saved)
                    || index < last && SameProjectionRegister(operand, source))
                {
                    return null;
                }
            }
        }

        return collector;
    }

    private static bool HasRetainedPreCallRoot(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int call,
        int end
    )
    {
        var redefined = new HashSet<int>();
        var knownClears = new HashSet<(int Index, int Slot)>();
        for (int index = call + 1; index < end; index++)
        {
            var instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode == Label || EndsBeamBlock(opcode))
            {
                break;
            }

            int writes = ReadPreCallAliasWriteMask(instruction);
            if (writes < 0)
            {
                int destination = StackAllocationDestination(instruction);
                if (opcode == InitYRegisters)
                {
                    redefined.UnionWith(instruction.Operands.Skip(1).Select(operand => (int)operand.Value));
                    continue;
                }

                if (opcode == TestHeap)
                {
                    continue;
                }

                if (destination < 0 || destination >= sizeof(int) * 8)
                {
                    break;
                }

                writes = 1 << destination;
            }

            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                var operand = instruction.Operands[position];
                if (operand.Kind == BeamOperandParameters.Y
                    && (position >= sizeof(int) * 8 || (writes & (1 << position)) == 0)
                    && !redefined.Contains((int)operand.Value)
                    && flow.HasLiveSharedFrameRoot(call, operand, knownClears))
                {
                    return true;
                }
            }

            for (int position = 0; position < instruction.Operands.Count && position < sizeof(int) * 8; position++)
            {
                if ((writes & (1 << position)) != 0 && instruction.Operands[position] is
                    { Kind: BeamOperandParameters.Y } destination)
                {
                    redefined.Add((int)destination.Value);
                }
            }
        }

        return false;
    }

    private static int ReadPreCallAliasWriteMask(BeamInstruction instruction) => instruction.Definition.Opcode switch
    {
        Move or GetHead or GetTail or MakeFun => 1 << 1,
        GetTupleElement or PutList => 1 << 2,
        GetList => (1 << 1) | (1 << 2),
        PutTuple => 1,
        Swap => (1 << 0) | (1 << 1),
        BifZero or BifOne or BifTwo or BifThree => 1 << (instruction.Operands.Count - 1),
        Line or IsAtom or IsInteger or IsTuple or IsList or IsNil or IsNonemptyList or ExactEqualTest
            or ExactNotEqualTest or NumericEqualTest or NumericNotEqualTest or IsTaggedTuple
            or TestArity or IsGreaterOrEqual or IsLess => 0,
        _ => -1
    };
}
