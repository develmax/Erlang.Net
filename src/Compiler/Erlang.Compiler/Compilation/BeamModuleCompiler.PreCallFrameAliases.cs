using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardInitializedCallArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int frame = 0; frame + 6 < body.Count; frame++)
        {
            if (body[frame].Definition.Opcode is not (Allocate or AllocateHeap)
                || body[frame + 1] is not { Definition.Opcode: InitYRegisters } initial)
            {
                continue;
            }

            for (int store = frame + 2; store + 4 < body.Count && body[store].Definition.Opcode == Move; store++)
            {
                if (body[store] is not
                    {
                        Operands: [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } slot
                        ]
                    }
                    || body[store + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var saved, { Kind: BeamOperandParameters.X, TypeIndex: null } outgoing]
                    }
                    || saved != slot || source == outgoing
                    || Enumerable.Range(frame + 2, store - frame - 2)
                        .Any(at => body[at].Operands.Any(operand => SameProjectionRegister(operand, slot))))
                {
                    continue;
                }

                int kill = store + 2;
                while (kill < body.Count && body[kill].Definition.Opcode == Move
                    && body[kill].Operands.All(operand => operand.Kind != BeamOperandParameters.Y))
                {
                    kill++;
                }

                if (kill + 1 >= body.Count || body[kill] is not
                    { Definition.Opcode: InitYRegisters, Operands: [_, var cleared] }
                    || cleared != slot)
                {
                    continue;
                }

                int call = kill + 1;
                while (call < body.Count && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call >= body.Count || body[call] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] }
                    || outgoing.Value >= arity)
                {
                    continue;
                }

                // The pre-call copy begins and ends within the same initial
                // frame and its saved slot is dead at the first call. Keep
                // that slot initialized before making the argument directly.
                output.Write(code.Span[copied..initial.Offset]);
                WriteFrameInitialization(output, initial.Operands.Skip(1).Append(slot)
                    .Select(operand => (int)operand.Value).Distinct().Order().ToArray());
                output.Write(code.Span[body[frame + 2].Offset..body[store].Offset]);
                output.WriteByte(Move);
                WriteRegister(output, source);
                WriteRegister(output, outgoing);
                output.Write(code.Span[body[store + 2].Offset..body[kill].Offset]);
                copied = body[kill + 1].Offset;
                frame = call;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private byte[]? EliminatePreCallFrameAliases(
        ReadOnlyMemory<byte> code,
        int labelCount,
        bool allowCollectors = true
    ) => RewritePreCallFrameAliases(
        code,
        labelCount,
        allowCollectors,
        consumedBeforeCallOnly: false
    );

    private byte[]? EliminateConsumedPreCallRoots(ReadOnlyMemory<byte> code, int labelCount) =>
        RewritePreCallFrameAliases(
            code,
            labelCount,
            allowCollectors: true,
            consumedBeforeCallOnly: true
        );

    private byte[]? RewritePreCallFrameAliases(
        ReadOnlyMemory<byte> code,
        int labelCount,
        bool allowCollectors,
        bool consumedBeforeCallOnly
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
                || (consumedBeforeCallOnly
                    ? !IsConsumedPreCallFrame(body, plan.Call, end)
                    : !HasRetainedPreCallRoot(
                        body,
                        flow ??= new BeamProjectionFlow(body),
                        plan.Call,
                        end
                    )))
            {
                continue;
            }

            var aliases = plan.Aliases.Where(alias =>
                frame.Owners[alias.Value.Spill - start] == allocation
                && frame.Owners[alias.Value.LastUse - start] == allocation)
                .ToDictionary();
            var collectorRoots = PlanPreCallCollectorRoots(body, aliases);
            if (aliases.Count == 0 || consumedBeforeCallOnly && collectorRoots.Count == 0)
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

    private static bool IsConsumedPreCallFrame(IReadOnlyList<BeamInstruction> body, int call, int end)
    {
        // The first ordinary call still owns its return-address frame. Only
        // the argument roots consumed before that call can leave the frame.
        for (int index = call + 1; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode != InitYRegisters
                && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
            {
                return false;
            }
        }

        return true;
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
        if (ReadFirstPreCallFrameReferences(
            body,
            start,
            end,
            allowLaterCalls: true
        ) is not { } plan)
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
    )? ReadFirstPreCallFrameReferences(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        bool allowLaterCalls = false
    )
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
                    if (call < 0)
                    {
                        call = index;
                    }
                    else if (!allowLaterCalls)
                    {
                        return null;
                    }

                    // Later calls cannot extend the X lifetime. Continue
                    // counting Y uses so a surviving slot vetoes aliasing.
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
