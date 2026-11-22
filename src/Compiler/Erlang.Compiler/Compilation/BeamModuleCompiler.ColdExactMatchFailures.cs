using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ColdExactMatch(
        int Test,
        int Line,
        int Failure,
        int Success
    );

    private byte[]? MoveColdExactMatchFailures(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(ExactNotEqualTest) || !code.Span.Contains(BadMatch))
        {
            return null;
        }

        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        if (body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine))
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int boundary = 0; boundary < body.Count; boundary++)
        {
            byte opcode = body[boundary].Definition.Opcode;
            if (opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = boundary + 1;
            int end = boundary;
            if (opcode == FunctionInfo)
            {
                while (end > start && body[end - 1].Definition.Opcode == Line)
                {
                    end--;
                }

                if (end == start || body[end - 1].Definition.Opcode != Label)
                {
                    continue;
                }

                end--;
            }

            if (end < start + 4 || body[start].Definition.Opcode != Label
                || body.Skip(start).Take(end - start).Any(instruction => instruction.Definition.Opcode is TryEnter or CatchEnter))
            {
                continue;
            }

            int suffix = FindTrailingExceptionBlocks(body, start, end);
            if (suffix <= start + 1 || !EndsBeamBlock(body[suffix - 1].Definition.Opcode))
            {
                continue;
            }

            var matches = new List<ColdExactMatch>();
            for (int index = start + 2; index + 2 < suffix; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: ExactNotEqualTest,
                        Operands: [var target, { Kind: BeamOperandParameters.X, Value.IsZero: true } result, _]
                    })
                {
                    continue;
                }

                int line = index + 1;
                int failure = body[line].Definition.Opcode == Line ? line + 1 : line;
                int success = failure + 1;
                if (success >= suffix
                    || body[failure] is not { Definition.Opcode: BadMatch, Operands: [var rejected] } || result != rejected
                    || body[success] is not { Definition.Opcode: Label, Operands: [var label] } || target.Value != label.Value
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(index, (int)label.Value, out _))
                {
                    continue;
                }

                int producer = index - 1;
                if (body[producer].Definition.Opcode == InitYRegisters)
                {
                    producer--;
                }

                if (producer <= start || body[producer].Definition.Opcode is not (LocalCall or ExternalCall))
                {
                    continue;
                }

                if (line == failure)
                {
                    // The inline failure inherited the call position on its only incoming path.
                    line = producer - 1;
                    if (body[line].Definition.Opcode != Line)
                    {
                        continue;
                    }
                }

                matches.Add(new(
                    index,
                    line,
                    failure,
                    success
                ));
            }

            if (matches.Count == 0)
            {
                continue;
            }

            var existing = new Dictionary<int, int>();
            for (int first = suffix; first < end;)
            {
                int last = first + 1;
                while (body[last].Definition.Opcode == Line)
                {
                    last++;
                }

                existing.Add(first, last);
                first = last + 1;
            }

            var inline = matches.ToDictionary(match => match.Test + 1);
            if (flow!.OrderColdFailureBlocks(
                start,
                end,
                inline.Keys,
                existing.Keys
            ) is not { } order)
            {
                continue;
            }

            foreach (ColdExactMatch match in matches)
            {
                int test = body[match.Test].Offset;
                output.Write(code.Span[copied..test]);
                output.WriteByte(ExactEqualTest);
                output.Write(code.Span[(test + 1)..body[match.Test + 1].Offset]);
                copied = body[match.Success + 1].Offset;
            }

            output.Write(code.Span[copied..body[suffix].Offset]);
            foreach (int block in order)
            {
                if (inline.TryGetValue(block, out ColdExactMatch? match))
                {
                    WriteInstruction(match.Success);
                    WriteInstruction(match.Line);
                    WriteInstruction(match.Failure);
                }
                else
                {
                    output.Write(code.Span[body[block].Offset..body[existing[block] + 1].Offset]);
                }
            }

            copied = body[end].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return RebuildLineTable(output.ToArray(), labelCount);

        void WriteInstruction(int index) =>
            output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
    }

    private byte[]? OrderTerminalListRetryBlocks(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsNonemptyList)
            || !code.Span.Contains(TailLocalCallWithFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int boundary = 0; boundary < body.Count; boundary++)
        {
            byte opcode = body[boundary].Definition.Opcode;
            if (opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = boundary + 1;
            int end = boundary;
            if (opcode == FunctionInfo)
            {
                while (end > start && body[end - 1].Definition.Opcode == Line)
                {
                    end--;
                }

                if (end == start || body[end - 1].Definition.Opcode != Label)
                {
                    continue;
                }

                end--;
            }

            if (end - start < 12 || body[start] is not { Definition.Opcode: Label, Operands: [var function] }
                || body[start + 1] is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands: [var emptyLabel, { Kind: BeamOperandParameters.X, TypeIndex: null } input]
                }
                || !IsSelfTail(body[end - 1], function))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(start + 1, (int)emptyLabel.Value, out int empty)
                || empty + 7 >= end || empty <= start + 2 || !IsSelfTail(body[empty - 1], function)
                || body[empty + 1] is not { Definition.Opcode: IsNil, Operands: [var mismatch, var original] }
                || original != input || mismatch.Value != EntryFrameFailureLabel(body, start)
                || body[empty + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } value,
                    { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
                    ]
                }
                || body[start - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || value.Value >= arity.Value || body[empty + 3].Definition.Opcode != Return
                || body[empty + 4] is not { Definition.Opcode: Label, Operands: [var retryLabel] }
                || body[empty + 5].Definition.Opcode != TestHeap
                || body[empty + 6].Definition.Opcode != PutList)
            {
                continue;
            }

            int retry = empty + 4;
            int guard = empty - 2;
            while (guard > start + 2 && body[guard].Definition.Opcode == Move)
            {
                guard--;
            }

            if (body[guard] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var failed, { Kind: BeamOperandParameters.X, Value.IsZero: true }, { Kind: BeamOperandParameters.Atom }]
                }
                || failed.Value != retryLabel.Value || body[guard - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || !flow.TryGetSingleForwardTarget(guard, (int)retryLabel.Value, out int target) || target != retry
                || Enumerable.Range(retry + 3, end - retry - 4).Any(index => body[index].Definition.Opcode != Move)
                || ReadDisjointFrameOwnership(body, start, end) is not { } frame || frame.Uses.Count != 1
                || frame.Owners[empty - start] != UnframedOwner
                || frame.Owners[retry - start] < 0
                || flow.OrderColdFailureBlocks(
                    start,
                    end,
                    [],
                    [empty, retry]
                ) is not [var first, var last] || first != retry || last != empty)
            {
                continue;
            }

            // Both suffixes are terminal and are entered only by their own
            // checked edges. Retain label numbers in physical order.
            Retarget(start + 1, retryLabel);
            Retarget(guard, emptyLabel);
            output.Write(code.Span[copied..body[empty + 1].Offset]);
            output.Write(code.Span[body[retry + 1].Offset..body[end].Offset]);
            output.Write(code.Span[body[retry].Offset..body[retry + 1].Offset]);
            output.Write(code.Span[body[empty + 1].Offset..body[retry].Offset]);
            copied = body[end].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Retarget(int index, BeamOperand label)
        {
            int operand = body[index].Offset + 1;
            var cursor = new BeamCursor(code[operand..body[index + 1].Offset]);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[copied..operand]);
            BeamCompactWriter.Write(output, LabelTag, label.Value);
            copied = operand + cursor.Position;
        }

        static bool IsSelfTail(BeamInstruction instruction, BeamOperand function) =>
            instruction is { Definition.Opcode: TailLocalCallWithFrame, Operands: [_, var called, _] }
            && called.Value == function.Value;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal int[]? OrderColdFailureBlocks(
            int entry,
            int end,
            IEnumerable<int> inlineFailures,
            IEnumerable<int> coldFailures
        )
        {
            var inline = inlineFailures.ToHashSet();
            var failures = inline.Concat(coldFailures).ToHashSet();
            var order = new List<int>(failures.Count);
            var visited = new HashSet<int>();
            var pending = new Stack<(int Index, bool Exiting)>();
            pending.Push((entry, false));
            while (pending.TryPop(out var step))
            {
                if (step.Exiting)
                {
                    if (failures.Contains(step.Index))
                    {
                        order.Add(step.Index);
                    }

                    continue;
                }

                if (step.Index < entry || step.Index >= end || !visited.Add(step.Index))
                {
                    continue;
                }

                pending.Push((step.Index, true));
                int[] next = successors[step.Index];
                if (inline.Contains(step.Index + 1))
                {
                    // The inverted instruction encodes the success jump first. SSA visits failure first.
                    foreach (int target in next)
                    {
                        pending.Push((target, false));
                    }
                }
                else
                {
                    for (int index = next.Length - 1; index >= 0; index--)
                    {
                        pending.Push((next[index], false));
                    }
                }
            }

            order.Reverse();

            return order.Count == failures.Count ? order.ToArray() : null;
        }
    }
}
