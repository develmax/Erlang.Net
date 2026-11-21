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
            if (start >= end || body[start] is not { Definition.Opcode: Label, Operands: [var label] }
                || ReadPostCallResultAlias(body, start, end) is not { } plan
                || ReadDisjointFrameOwnership(body, start, end) is not { } frame || frame.Uses.Count != 1)
            {
                continue;
            }

            int owner = frame.Uses.Keys.Single();
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
                    var (from, to, _) = operands[operand++];
                    edits.Add(new(
                        from,
                        to,
                        XRegisterTag,
                        0
                    ));
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

    private static int ReadPostCallAliasWriteMask(BeamInstruction instruction) =>
        instruction.Definition.Opcode is Label or Jump or SelectValue or SelectTupleArity
            or BadMatch or BadRecord or CaseEnd or IfEnd or TestHeap
        || IsEntryFramePredicate(instruction.Definition.Opcode) ? 0
            : ReadPreCallAliasWriteMask(instruction);

    private sealed partial class BeamProjectionFlow
    {
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
