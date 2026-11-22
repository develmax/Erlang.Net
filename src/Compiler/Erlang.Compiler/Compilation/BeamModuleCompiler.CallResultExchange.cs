using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseRetiredInputForCallResult(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded
            || RewriteRetiredInputCallResults(
                code,
                decoded.Body,
                decoded.StackOperands
            ) is not { Code: var rewritten, Entries: var entries }
            || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } compacted)
        {
            return null;
        }

        return RewriteDisjointFrames(
            rewritten,
            compacted.Body,
            compacted.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );
    }

    private byte[]? ReuseRetiredClosureInputForCallResult(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(MakeFun)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        var entries = new HashSet<int>();
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (start + 9 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var function] })
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int spill = start + 2; spill + 7 < end; spill++)
            {
                if (body[spill - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[spill] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || result != x0
                    || body[spill + 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                    || body[spill + 2] is not
                    {
                        Definition.Opcode: MakeFun,
                        Operands: [var lambda, var closure, { Kind: BeamOperandParameters.Count, Value.IsZero: true }]
                    } || closure != x0
                    || body[spill + 3] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } retired, var second]
                    } || retired == saved || second != x1)
                {
                    continue;
                }

                int call = spill + 4;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call + 3 >= end || body[call] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] }
                    || arity != 2 || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { Owners: var owners, Uses: var frames } || frames.Count != 1
                    || owners[spill - start] < 0 || owners[call - start] != owners[spill - start]
                    || Enumerable.Range(start, spill - start).Any(index => body[index].Definition.Opcode != InitYRegisters
                        && body[index].Operands.Any(operand => SameProjectionRegister(operand, saved)))
                    || Enumerable.Range(call + 1, end - call - 1).Any(index => body[index].Operands
                        .Any(operand => SameProjectionRegister(operand, retired))))
                {
                    continue;
                }

                int construction = -1;
                for (int index = call + 1; index < end; index++)
                {
                    if (!body[index].Operands.Any(operand => SameProjectionRegister(operand, saved)))
                    {
                        continue;
                    }

                    if (construction >= 0 || body[index] is not { Definition.Opcode: PutList, Operands: [var head, var tail, var list] }
                        || head != saved || tail != x0 || list != x0 || owners[index - start] != owners[spill - start])
                    {
                        construction = -1;
                        break;
                    }

                    construction = index;
                }

                if (construction < 0 || !(flow ??= new BeamProjectionFlow(body)).HasOwnedCallResultHandoff(
                    start,
                    end,
                    spill,
                    saved
                ))
                {
                    continue;
                }

                // The closure has no captures and only the current result
                // survives this heap test. Save the old input before reusing its slot.
                output.Write(code.Span[copied..body[spill].Offset]);
                WriteHeapRoots(
                    output,
                    code,
                    body[spill + 1].Offset,
                    body[spill + 2].Offset,
                    0,
                    1
                );
                output.WriteByte(MakeFun);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, lambda.Value);
                WriteRegister(output, x1);
                output.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                MoveValue(retired, x2);
                MoveValue(x0, retired);
                MoveValue(x1, x0);
                MoveValue(x2, x1);
                output.Write(code.Span[body[spill + 4].Offset..body[construction].Offset]);
                output.WriteByte(PutList);
                WriteRegister(output, retired);
                WriteRegister(output, x0);
                WriteRegister(output, x0);
                copied = body[construction + 1].Offset;
                entries.Add((int)function.Value);
                break;
            }
        }

        if (entries.Count == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();
        if (DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteDisjointFrames(
            rewritten,
            decoded.Body,
            decoded.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );

        void MoveValue(BeamOperand from, BeamOperand to)
        {
            output.WriteByte(Move);
            WriteRegister(output, from);
            WriteRegister(output, to);
        }
    }

    private static (byte[] Code, HashSet<int> Entries)? RewriteRetiredInputCallResults(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var entries = new HashSet<int>();
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (start + 6 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var function] }
                || Enumerable.Range(start, end - start).Any(index => body[index].Definition.Opcode == ExecutableLine))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int spill = start + 2; spill + 3 < end; spill++)
            {
                if (body[spill - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[spill] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || result != x0)
                {
                    continue;
                }

                int call = spill + 1;
                while (call < end && (body[call].Definition.Opcode is Line or InitYRegisters
                    || body[call] is { Definition.Opcode: Move, Operands: [_, { Kind: BeamOperandParameters.X }] }))
                {
                    call++;
                }

                if (call >= end || body[call] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [var arity, _]
                    }
                    || Enumerable.Range(spill + 1, call - spill - 1).Any(index =>
                        body[index].Definition.Opcode != InitYRegisters
                        && body[index].Operands.Any(value => SameProjectionRegister(value, saved))))
                {
                    continue;
                }

                int selected = -1;
                for (int load = spill + 1; load < call; load++)
                {
                    if (body[load] is not
                        {
                            Definition.Opcode: Move,
                            Operands:
                            [
                            { Kind: BeamOperandParameters.Y, TypeIndex: null } old,
                            { Kind: BeamOperandParameters.X, TypeIndex: null } outgoing
                            ]
                        }
                        || old == saved || outgoing.Value >= arity.Value
                        || Enumerable.Range(spill + 1, load - spill - 1).Any(index =>
                            body[index].Operands.Any(value => SameProjectionRegister(value, outgoing)
                                || SameProjectionRegister(value, old)))
                        || Enumerable.Range(load + 1, call - load - 1).Any(index =>
                            body[index] is { Definition.Opcode: Move, Operands: [_, var target] }
                            && SameProjectionRegister(target, outgoing))
                        || Enumerable.Range(load + 1, end - load - 1).Any(index =>
                            body[index].Definition.Opcode != InitYRegisters
                            && body[index].Operands.Any(value => SameProjectionRegister(value, old)))
                        || Enumerable.Range(start, load - start + 1)
                            .Where(index => body[index].Definition.Opcode != InitYRegisters)
                            .Sum(index => body[index].Operands.Count(value => SameProjectionRegister(value, old))) != 2)
                    {
                        continue;
                    }

                    if (selected >= 0)
                    {
                        selected = -1;
                        break;
                    }

                    selected = load;
                }

                if (selected < 0
                    || (ownership ??= ReadDisjointFrameOwnership(body, start, end)) is not
                    { Owners: var owners, Uses: var frames }
                    || frames.Count != 1 || owners[spill - start] < 0
                    || owners[call - start] != owners[spill - start]
                    || body[owners[spill - start]].Definition.Opcode != Allocate
                    || !(flow ??= new BeamProjectionFlow(body)).HasOwnedCallResultHandoff(
                        start,
                        end,
                        spill,
                        saved
                    ))
                {
                    continue;
                }

                BeamOperand retired = body[selected].Operands[0];
                BeamOperand argument = body[selected].Operands[1];
                // The old slot's only future use is an outgoing argument.
                // Copy it before saving the first result into the same slot.
                output.Write(code.Span[copied..body[spill].Offset]);
                if (argument == x0)
                {
                    output.WriteByte(Swap);
                    WriteRegister(output, retired);
                    WriteRegister(output, x0);
                }
                else
                {
                    output.Write(code.Span[body[selected].Offset..body[selected + 1].Offset]);
                    output.WriteByte(Move);
                    WriteRegister(output, x0);
                    WriteRegister(output, retired);
                }

                copied = body[spill + 1].Offset;
                int stack = FirstStackOperandAtOrAfter(stackOperands, copied);
                for (int index = spill + 1; index < end; index++)
                {
                    int offset = body[index].Offset;
                    int next = body[index + 1].Offset;
                    if (index == selected || body[index].Definition.Opcode == InitYRegisters
                        && body[index].Operands.Skip(1).Any(slot => slot == retired || slot == saved))
                    {
                        output.Write(code.Span[copied..offset]);
                        if (index != selected)
                        {
                            WriteFrameInitialization(
                                output,
                                body[index].Operands.Skip(1)
                                .Where(slot => slot != retired)
                                .Select(slot => (int)(slot == saved ? retired.Value : slot.Value))
                                .Distinct().Order().ToArray()
                            );
                        }

                        copied = next;
                    }

                    while (stack < stackOperands.Count && stackOperands[stack].Start < next)
                    {
                        var (from, to, slot) = stackOperands[stack++];
                        if (from < copied || slot != saved.Value)
                        {
                            continue;
                        }

                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, YRegisterTag, retired.Value);
                        copied = to;
                    }
                }

                entries.Add((int)function.Value);
                break;
            }
        }

        if (entries.Count == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return (output.ToArray(), entries);
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasOwnedCallResultHandoff(
            int start,
            int end,
            int spill,
            BeamOperand saved
        )
        {
            var pending = new Stack<(int Index, bool Stored)>();
            var visited = new HashSet<(int Index, bool Stored)>();
            pending.Push((start, false));
            while (pending.TryPop(out var state))
            {
                if (!visited.Add(state))
                {
                    continue;
                }

                var (index, stored) = state;
                if (index < start || index >= end)
                {
                    if (stored)
                    {
                        return false;
                    }

                    continue;
                }

                if (stored && index <= spill)
                {
                    return false;
                }

                if (index == spill)
                {
                    stored = true;
                }
                else if (!stored && body[index].Definition.Opcode != InitYRegisters
                    && body[index].Operands.Any(value => SameProjectionRegister(value, saved)))
                {
                    return false;
                }

                foreach (int target in successors[index])
                {
                    pending.Push((target, stored));
                }
            }

            return true;
        }
    }

    private static byte[]? ExchangeCallResultWithSavedInput(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (PlanExchange(entry, end) is not { } plan)
            {
                continue;
            }

            var (allocation, spill, store, load, clear) = plan;
            for (int index = allocation; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (index == clear)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    WriteFrameInitialization(output, [0]);
                    copied = instruction.Offset;
                }

                int size = LinearFrameSizeOperand(instruction.Definition.Opcode);
                if (size >= 0 && instruction.Operands[size].Value == 2)
                {
                    int operandsStart = instruction.Offset + 1;
                    var cursor = new BeamCursor(code[operandsStart..]);
                    for (int position = 0; position < size; position++)
                    {
                        _ = BeamCompactReader.Read(cursor);
                    }

                    output.Write(code.Span[copied..(operandsStart + cursor.Position)]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    _ = BeamCompactReader.Read(cursor);
                    copied = operandsStart + cursor.Position;
                }
                else if (index == store)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(Swap);
                    WriteRegister(output, y0);
                    WriteRegister(output, x0);
                    index++;
                    copied = body[index + 1].Offset;
                }
                else if (index == spill || index == load)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(Move);
                    WriteRegister(output, index == spill ? instruction.Operands[0] : y0);
                    WriteRegister(output, index == spill ? y0 : x0);
                    copied = body[index + 1].Offset;
                }
                else if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    // Only obsolete initializations and post-use spills can remain.
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = body[index + 1].Offset;
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        (int Allocation, int Spill, int Store, int Load, int Clear)? PlanExchange(int entry, int end)
        {
            int allocation = FindEntryFrameAllocation(body, entry, end);
            if (allocation < 0 || body[allocation].Definition.Opcode != Allocate || body[allocation].Operands[0].Value != 2)
            {
                return null;
            }

            int call = allocation + 1;
            while (call < end && body[call].Definition.Opcode is Move or InitYRegisters or Line)
            {
                call++;
            }

            int store = call + 1;
            if (store + 2 >= end || !IsExchangeReturningCall(body[call], 1)
                || body[store] is not { Definition.Opcode: Move, Operands: [var result, var saved] } || result != x0
                || body[store + 1] is not { Definition.Opcode: Move, Operands: [var input, var argument] } || argument != x0
                || input is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || saved is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || saved.Value == input.Value || saved.Value > 1 || input.Value > 1)
            {
                return null;
            }

            int spill = -1;
            for (int index = allocation + 1; index < call; index++)
            {
                BeamInstruction setup = body[index];
                if (!setup.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    continue;
                }

                if (setup.Definition.Opcode == InitYRegisters
                    && (spill < 0 || !setup.Operands.Skip(1).Contains(input)))
                {
                    continue;
                }

                if (spill >= 0 || setup is not { Definition.Opcode: Move, Operands: [var source, var destination] }
                    || source is not { Kind: BeamOperandParameters.X, TypeIndex: null } || destination != input)
                {
                    return null;
                }

                spill = index;
            }

            int second = store + 2;
            while (second < end && (body[second].Definition.Opcode == Line
                || body[second] is { Definition.Opcode: Move, Operands: [var source, { Kind: BeamOperandParameters.X }] }
                    && source.Kind != BeamOperandParameters.Y))
            {
                second++;
            }

            int load = second + 2;
            if (spill < 0 || load + 1 >= end || !IsExchangeReturningCall(body[second], 1)
                || body[second + 1] is not { Definition.Opcode: Move, Operands: [var secondResult, var secondArgument] }
                || secondResult != x0 || secondArgument != x1
                || body[load] is not { Definition.Opcode: Move, Operands: [var retained, var firstArgument] }
                || retained != saved || firstArgument != x0)
            {
                return null;
            }

            int clear = load + 1;
            int consume = clear;
            while (consume < end && body[consume].Definition.Opcode == Line)
            {
                consume++;
            }

            if (consume == end || !IsExchangeReturningCall(body[consume], 2))
            {
                return null;
            }

            for (int index = consume + 1; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Name == "executable_line"
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y)
                        && !(instruction.Definition.Opcode == InitYRegisters
                            || instruction is { Definition.Opcode: Move, Operands: [var value, var destination] }
                                && value == x0 && destination == saved))
                {
                    return null;
                }
            }

            // The copy island has no entry labels or collector between spill/load pairs.
            // Prove that each frame exit and every branch join share the same allocation.
            if (ReadDisjointFrameOwnership(body, entry, end) is not { } ownership
                || ownership.Uses.Count != 1 || ownership.Owners[consume - entry] != allocation)
            {
                return null;
            }

            return (allocation, spill, store, load, clear);
        }
    }

    private static bool IsExchangeReturningCall(BeamInstruction instruction, int minimumArity) =>
        instruction.Definition.Opcode is LocalCall or ExternalCall && instruction.Operands[0].Value >= minimumArity;
}
