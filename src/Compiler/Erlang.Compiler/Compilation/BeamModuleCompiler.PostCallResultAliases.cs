using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? EliminatePostCallResultAliases(ReadOnlyMemory<byte> code, int labelCount)
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

            var result = ReadPostCallResultAlias(body, start, end);
            var list = result is null ? ReadPostCallResultListBranch(body, start, end) : null;
            if (result is null && list is null)
            {
                result = ReadOwnedPostCallResultAlias(
                    body,
                    start,
                    end,
                    ref flow
                );
            }

            if (result is null && list is null
                || ReadDisjointFrameOwnership(body, start, end) is not { } frame || frame.Uses.Count != 1)
            {
                continue;
            }

            int owner = frame.Uses.Keys.Single();
            if (list is { } branch)
            {
                if (frame.Owners[branch.Spill - start] == owner
                    && frame.Owners[branch.Compare - start] == owner
                    && frame.Owners[branch.Tail - start] == owner
                    && frame.Owners[branch.ReturnMove - start] == owner
                    && (flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                        branch.Compare,
                        (int)body[branch.Compare].Operands[0].Value,
                        out int fallback
                    ) && fallback == branch.ReturnMove - 1)
                {
                    entries.Add((int)label.Value);
                    AddPostCallResultListEdits(
                        body,
                        operands,
                        branch,
                        edits
                    );
                }

                continue;
            }

            var plan = result!.Value;
            if (frame.Owners[plan.Spill - start] != owner
                || plan.Reads.Any(index => frame.Owners[index - start] != owner)
                || (flow ??= new BeamProjectionFlow(body)).ReadRetainedResultHeapRoots(
                    plan.Spill,
                    end,
                    plan.Reads
                ) is not { } heaps)
            {
                continue;
            }

            entries.Add((int)label.Value);
            edits.Add(new(
                body[plan.Spill].Offset,
                body[plan.Spill + 1].Offset,
                0,
                0,
                IsRemoval: true
            ));

            int slot = (int)body[plan.Spill].Operands[1].Value;
            int operand = FirstStackOperandAtOrAfter(operands, body[plan.Spill + 1].Offset);
            foreach (int index in plan.Reads)
            {
                var instruction = body[index];
                int next = body[index + 1].Offset;
                while (operand < operands.Count && operands[operand].Start < instruction.Offset)
                {
                    operand++;
                }

                if (instruction is
                    {
                        Definition.Opcode: Move,
                        Operands: [{ TypeIndex: null }, var destination]
                    }
                    && IsBareX0(destination))
                {
                    edits.Add(new(
                        instruction.Offset,
                        next,
                        0,
                        0,
                        IsRemoval: true
                    ));
                    continue;
                }

                while (operand < operands.Count && operands[operand].Start < next)
                {
                    var (from, to, readSlot) = operands[operand++];
                    if (readSlot == slot)
                    {
                        edits.Add(new(
                            from,
                            to,
                            XRegisterTag,
                            0
                        ));
                    }
                }
            }

            foreach (int heap in heaps)
            {
                // The original Live operand is the single-byte compact encoding of zero.
                int next = body[heap + 1].Offset;
                edits.Add(new(
                    next - CompactRegisterCountWidth(0),
                    next,
                    BeamWire.UnsignedTag,
                    1
                ));
            }
        }

        edits.Sort((left, right) => left.Start.CompareTo(right.Start));
        if (ApplyDuplicatedFrameReadEdits(code, edits) is not { } rewritten
            || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return null;
        }

        // Keep the continuation's frame and remove its dead physical slots.
        return RewriteDisjointFrames(
            rewritten,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );
    }

    private static (int Spill, List<int> Reads)? ReadPostCallResultAlias(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        int call = -1;
        for (int index = start; index < end; index++)
        {
            switch (body[index].Definition.Opcode)
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
            }
        }

        if (call < 0)
        {
            return null;
        }

        int spill = call + 1;
        while (spill < end && body[spill].Definition.Opcode == Line)
        {
            spill++;
        }

        if (spill >= end || body[spill] is not
            {
                Definition.Opcode: Move,
                Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } alias]
            }
            || !IsBareX0(source))
        {
            return null;
        }

        var reads = new List<int>();
        for (int index = start; index < end; index++)
        {
            var instruction = body[index];
            if (index == spill || instruction.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            int writes = ReadPostCallAliasWriteMask(instruction);
            bool read = false;
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                var operand = instruction.Operands[position];
                if (operand.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                if (index < spill || !SameProjectionRegister(operand, alias) || writes < 0
                    || position < sizeof(int) * 8 && (writes & (1 << position)) != 0)
                {
                    return null;
                }

                read = true;
            }

            if (read)
            {
                reads.Add(index);
            }
        }

        return reads.Count == 0 ? null : (spill, reads);
    }

    private static (int Spill, List<int> Reads)? ReadOwnedPostCallResultAlias(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        int allocation = FindEntryFrameAllocation(body, start, end);
        if (allocation < 0 || Enumerable.Range(allocation + 1, end - allocation - 1).Any(index =>
            body[index].Definition.Opcode is TryEnter or CatchEnter or TrimFrame or LoopReceive or LoopReceiveEnd
                or Wait or WaitTimeout or CallFun or CallFunTyped or DynamicApply or DebugLine or ExecutableLine))
        {
            return null;
        }

        int producer = allocation;
        for (int spill = allocation + 1; spill < end; spill++)
        {
            BeamInstruction instruction = body[spill];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            bool call = body[producer].Definition.Opcode is LocalCall or ExternalCall;
            producer = spill;
            if (instruction is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                } || !IsBareX0(source)
                || !call && (spill + 2 >= end || body[spill + 1].Definition.Opcode != TestHeap
                    || body[spill + 2].Definition.Opcode is not (PutList or PutTuple)
                    || !body[spill + 2].Operands.Any(operand => SameProjectionRegister(operand, slot))
                    || !(flow ??= new BeamProjectionFlow(body)).HasPostCallResultAt(spill, allocation))
                || ReadResultSlotConsumers(
                    body,
                    start,
                    end,
                    spill,
                    slot,
                    call
                ) is not { } reads)
            {
                continue;
            }

            // All reads must be dominated by this definition and retain the
            // call's X0 on every incoming path, including a failed guard.
            if ((flow ??= new BeamProjectionFlow(body)).ReadRetainedResultHeapRoots(spill, end, reads) is not null)
            {
                return (spill, reads);
            }
        }

        return null;
    }

    private static List<int>? ReadResultSlotConsumers(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int spill,
        BeamOperand slot,
        bool immediateCallResult
    )
    {
        var reads = new List<int>();
        bool consumed = false;
        for (int index = start; index < end; index++)
        {
            var instruction = body[index];
            if (index == spill || instruction.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            bool readsSlot = false;
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (!SameProjectionRegister(operand, slot))
                {
                    continue;
                }

                int writes = ReadPostCallAliasWriteMask(instruction);
                bool checkedUpdateField = writes < 0 && immediateCallResult
                    && IsCheckedPostCallRecordUpdate(body, spill, index)
                    && position > StackWriteOperand(instruction);
                if (index < spill || operand.TypeIndex is not null || !checkedUpdateField
                    && (writes < 0 || position < sizeof(int) * 8 && (writes & (1 << position)) != 0))
                {
                    return null;
                }

                readsSlot = true;
            }

            if (readsSlot)
            {
                consumed |= instruction.Definition.Opcode != Move;
                reads.Add(index);
            }
        }

        if (consumed || immediateCallResult && reads is [var reload]
            && IsPostCallConstructorArgument(
                body,
                spill,
                reload,
                end
            ))
        {
            return reads;
        }

        return null;
    }

    private static bool IsCheckedPostCallRecordUpdate(
        IReadOnlyList<BeamInstruction> body,
        int spill,
        int update
    )
    {
        // A checked update reads its field values from the old registers.
        // Its separate reservation is the only collector in this corridor.
        return update == spill + 3
            && body[update] is
            {
                Definition.Opcode: UpdateRecord,
                Operands: [_, var size, { Kind: BeamOperandParameters.Y } record, var destination, ..]
            }
            && IsBareX0(destination)
            && body[spill + 1] is
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [_, var checkedRecord, var checkedSize, _]
            }
            && checkedSize.Value == size.Value && SameProjectionRegister(record, checkedRecord)
            && body[spill + 2] is
            {
                Definition.Opcode: TestHeap,
                Operands: [_, { Value.IsZero: true }]
            };
    }

    private static bool IsPostCallConstructorArgument(
        IReadOnlyList<BeamInstruction> body,
        int spill,
        int reload,
        int end
    )
    {
        if (body[reload] is not { Definition.Opcode: Move, Operands: [_, var destination] }
            || !IsBareX0(destination))
        {
            return false;
        }

        bool hasHeap = false;
        bool hasConstructor = false;
        for (int at = spill + 1; at < reload; at++)
        {
            switch (body[at].Definition.Opcode)
            {
                case TestHeap:
                    hasHeap = true;
                    break;
                case PutList:
                case PutTuple:
                case MakeFun:
                    hasConstructor = true;
                    break;
                case Line:
                case Move:
                    break;
                default:
                    return false;
            }
        }

        if (!hasHeap || !hasConstructor)
        {
            return false;
        }

        for (int at = reload + 1; at < end; at++)
        {
            var next = body[at];
            if (next.Definition.Opcode == Line
                || next is
                {
                    Definition.Opcode: Move,
                    Operands: [_, { Kind: BeamOperandParameters.X, Value.IsZero: false }]
                })
            {
                continue;
            }

            // The sibling constructor does not consume the saved slot. X0 becomes
            // live through its heap check because the outgoing call consumes it.
            return next.Definition.Opcode is (LocalCall or ExternalCall
                or TailLocalCallWithFrame or TailExternalCallWithFrame)
                && !next.Operands[0].Value.IsZero;
        }

        return false;
    }

    private static int ReadPostCallAliasWriteMask(BeamInstruction instruction) =>
        instruction.Definition.Opcode is Label or Jump or SelectValue or SelectTupleArity
            or BadMatch or BadRecord or CaseEnd or IfEnd or TestHeap
        || IsEntryFramePredicate(instruction.Definition.Opcode) ? 0
            : ReadPreCallAliasWriteMask(instruction);

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasPostCallResultAt(int spill, int allocation)
        {
            int producer = -1;
            var seen = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(spill);
            while (pending.TryPop(out int current))
            {
                foreach (int previous in predecessors[current])
                {
                    if (previous <= allocation || previous >= current)
                    {
                        return false;
                    }

                    byte opcode = body[previous].Definition.Opcode;
                    if (opcode is LocalCall or ExternalCall)
                    {
                        if (producer >= 0 && producer != previous)
                        {
                            return false;
                        }

                        producer = previous;
                    }
                    else if (opcode is Label or Line or Jump or SelectValue or SelectTupleArity || IsEntryFramePredicate(opcode))
                    {
                        if (seen.Add(previous))
                        {
                            pending.Push(previous);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                if (predecessors[current].Count == 0)
                {
                    return false;
                }
            }

            // Every path to the save is a non-clobbering continuation of one call.
            return producer >= 0;
        }

        internal IReadOnlySet<int>? ReadRetainedResultHeapRoots(int spill, int end, IReadOnlyList<int> reads)
        {
            var heaps = new HashSet<int>();
            var seen = new HashSet<int>();
            var pending = new Stack<int>(reads.SelectMany(index => predecessors[index]));
            while (pending.TryPop(out int index))
            {
                if (!seen.Add(index) || index == spill)
                {
                    continue;
                }

                if (index < spill || index >= end || predecessors[index].Count == 0)
                {
                    return null;
                }

                var instruction = body[index];
                int writes = ReadPostCallAliasWriteMask(instruction);
                if (writes < 0)
                {
                    return null;
                }

                for (int position = 0; position < instruction.Operands.Count && position < sizeof(int) * 8; position++)
                {
                    if ((writes & (1 << position)) != 0 && instruction.Operands[position] is
                        { Kind: BeamOperandParameters.X, Value.IsZero: true })
                    {
                        return null;
                    }
                }

                if (instruction.Definition.Opcode == TestHeap && instruction.Operands[^1].Value.IsZero)
                {
                    // This heap lies on a path to a later read of the original value.
                    heaps.Add(index);
                }

                foreach (int previous in predecessors[index])
                {
                    pending.Push(previous);
                }
            }

            return seen.Contains(spill) ? heaps : null;
        }
    }
}
