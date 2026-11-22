using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RematerializeOwnedLiteralSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Allocate)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body, StackOperands: var stackOperands })
        {
            return null;
        }

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
            if (start + 4 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var function] }
                || Enumerable.Range(start, end - start).Any(index => body[index].Definition.Opcode == ExecutableLine))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int definition = start + 2; definition + 2 < end; definition++)
            {
                if (body[definition] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var value, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    }
                    || !IsNonRegisterValue(value) || value.TypeIndex is not null)
                {
                    continue;
                }

                var loads = new List<int>();
                for (int index = start; index < end; index++)
                {
                    BeamInstruction instruction = body[index];
                    if (index == definition || !instruction.Operands.Any(operand => SameProjectionRegister(operand, saved))
                        || index < definition && instruction.Definition.Opcode == InitYRegisters)
                    {
                        continue;
                    }

                    if (index <= definition || !CanRematerializeLiteralRead(instruction, saved))
                    {
                        loads.Clear();
                        break;
                    }

                    loads.Add(index);
                }

                if (loads.Count == 0 || (ownership ??= ReadDisjointFrameOwnership(body, start, end)) is not
                    { Owners: var owners, Uses: var frames } || frames.Count != 1
                    || owners[definition - start] < 0 || body[owners[definition - start]].Definition.Opcode != Allocate
                    || !(flow ??= new BeamProjectionFlow(body)).HasOwnedCallResultHandoff(
                        start,
                        end,
                        definition,
                        saved
                    ))
                {
                    continue;
                }

                // The literal comes from the same immutable operand everywhere.
                // Removing the whole slot also removes its initial nil root.
                int literal = body[definition].Offset + 1;
                var cursor = new BeamCursor(code[literal..body[definition + 1].Offset]);
                _ = BeamCompactReader.Read(cursor);
                if (value.Kind == BeamOperandParameters.LiteralValue)
                {
                    _ = BeamCompactReader.Read(cursor);
                }

                ReadOnlySpan<byte> encoded = code.Span.Slice(literal, cursor.Position);
                output.Write(code.Span[copied..body[definition].Offset]);
                copied = body[definition + 1].Offset;
                foreach (int load in loads)
                {
                    int operand = FirstStackOperandAtOrAfter(stackOperands, body[load].Offset);
                    int endOfLoad = body[load + 1].Offset;
                    while (operand < stackOperands.Count && stackOperands[operand].Start < endOfLoad)
                    {
                        var (from, to, slot) = stackOperands[operand++];
                        if (slot != saved.Value)
                        {
                            continue;
                        }

                        output.Write(code.Span[copied..from]);
                        output.Write(encoded);
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
        byte[] rewritten = output.ToArray();
        if (DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } compacted)
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

    private byte[]? RematerializePostTrimLiteralSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var entries = new HashSet<int>();
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
            if (start + 8 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var function] }
                || ReadPostTrimLiteralSlot(body, start, end) is not { } plan)
            {
                continue;
            }

            int literal = body[plan.Definition].Offset + 1;
            var cursor = new BeamCursor(code[literal..body[plan.Definition + 1].Offset]);
            _ = BeamCompactReader.Read(cursor);
            if (body[plan.Definition].Operands[0].Kind == BeamOperandParameters.LiteralValue)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            ReadOnlySpan<byte> encoded = code.Span.Slice(literal, cursor.Position);
            output.Write(code.Span[copied..body[plan.Definition].Offset]);
            copied = body[plan.Definition + 1].Offset;
            foreach (int read in plan.Reads)
            {
                output.Write(code.Span[copied..(body[read].Offset + 1)]);
                output.Write(encoded);
                WriteRegister(output, body[read].Operands[1]);
                copied = body[read + 1].Offset;
            }

            // Keep the original Y0 in place. The remaining frame starts
            // at the preceding trim, so its closing width is two until compaction.
            output.Write(code.Span[copied..body[plan.Relocation].Offset]);
            copied = body[plan.Relocation + 2].Offset;
            output.Write(code.Span[copied..(body[plan.Deallocation].Offset + 1)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            copied = body[plan.Deallocation + 1].Offset;
            entries.Add((int)function.Value);
        }

        if (entries.Count == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();
        if (DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return null;
        }

        return RewriteDisjointFrameLayouts(
            rewritten,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            completeTrimsOnly: false,
            entriesToCompact: entries,
            reuseOccupiedEntrySlots: false,
            onlyAllocation: null,
            compactDeadTrimSlots: true
        );
    }

    private static (int Definition, int Relocation, int Deallocation, List<int> Reads)? ReadPostTrimLiteralSlot(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        int allocation = start + 1;
        if (body[allocation].Definition.Opcode != Allocate
            || ReadDisjointFrameOwnership(
                body,
                start,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } frame || frame.Uses.Count != 3)
        {
            return null;
        }

        int[] trims = frame.Uses.Keys.Where(index => body[index].Definition.Opcode == TrimFrame).Order().ToArray();
        if (trims is not [int first, int second]
            || !frame.Uses.TryGetValue(allocation, out bool[]? entrySlots) || entrySlots.Length < 3
            || frame.Owners[first - start] != allocation || frame.Uses[first].Length != 2
            || frame.Owners[second - start] != first || frame.Uses[second] is not [true])
        {
            return null;
        }

        var retained = new BeamOperand(BeamOperandParameters.Y, 0);
        var literalSlot = new BeamOperand(BeamOperandParameters.Y, 1);
        var initialLiteralSlot = new BeamOperand(BeamOperandParameters.Y, entrySlots.Length - 1);
        int relocation = second - 1;
        if (body[relocation] is not { Definition.Opcode: Move, Operands: [var source, var destination] }
            || source != retained || destination != literalSlot)
        {
            return null;
        }

        for (int index = allocation + 1; index < first; index++)
        {
            if (body[index].Definition.Opcode != InitYRegisters
                && body[index].Operands.Any(operand => SameProjectionRegister(operand, initialLiteralSlot)))
            {
                return null;
            }
        }

        int deallocation = end - 1;
        while (deallocation > second && body[deallocation].Definition.Opcode is Label or Line)
        {
            deallocation--;
        }

        if (deallocation <= second + 1 || body[deallocation].Definition.Opcode != Return)
        {
            return null;
        }

        deallocation--;
        if (body[deallocation].Definition.Opcode != Deallocate
            || frame.Owners[deallocation - start] != second)
        {
            return null;
        }

        int definition = -1;
        var reads = new List<int>();
        for (int index = first + 1; index < deallocation; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == Label
                || frame.Owners[index - start] != (index <= second ? first : second))
            {
                return null;
            }

            if (index >= relocation || !instruction.Operands.Any(operand => SameProjectionRegister(operand, literalSlot)))
            {
                continue;
            }

            if (instruction is not { Definition.Opcode: Move, Operands: [var from, var to] })
            {
                return null;
            }

            if (definition < 0 && to == literalSlot && from.TypeIndex is null && IsNonRegisterValue(from))
            {
                definition = index;
            }
            else if (definition >= 0 && from == literalSlot
                && to is { Kind: BeamOperandParameters.X, TypeIndex: null })
            {
                reads.Add(index);
            }
            else
            {
                return null;
            }
        }

        return definition >= 0 && reads.Count != 0 ? (definition, relocation, deallocation, reads) : null;
    }

    private static bool CanRematerializeLiteralRead(BeamInstruction instruction, BeamOperand saved)
    {
        IReadOnlyList<BeamOperand> operands = instruction.Operands;
        int destination = instruction.Definition.Opcode switch
        {
            Move => 1,
            PutList => 2,
            PutTuple => 0,
            _ => -1
        };
        if (destination < 0 || operands[destination] is not { Kind: BeamOperandParameters.X, TypeIndex: null })
        {
            return false;
        }

        foreach (BeamOperand operand in operands)
        {
            if (SameProjectionRegister(operand, saved) && operand != saved)
            {
                return false;
            }
        }

        return true;
    }

    private byte[]? RematerializeEntryLiteralFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var untrimmedEntries = new HashSet<int>();
        using var output = new MemoryStream();
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
            if (start + 4 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var label] })
            {
                continue;
            }

            int allocation = body[start + 1].Definition.Opcode == OnLoad ? start + 2 : start + 1;
            int definition = allocation + 1;
            int clear = definition + 1;
            if (clear + 1 >= end || body[allocation] is not { Definition.Opcode: Allocate, Operands: [var width, _] }
                || body[definition] is not { Definition.Opcode: Move, Operands: [var literal, var slot] }
                || !IsNonRegisterValue(literal) || literal.TypeIndex is not null
                || slot is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || slot.Value >= width.Value || body[clear].Definition.Opcode != InitYRegisters
                || !body[clear].Operands.Skip(1).Select(operand => operand.Value).SequenceEqual(
                    Enumerable.Range(0, (int)width.Value).Where(index => index != (int)slot.Value)
                        .Select(index => new BigInteger(index))
                ))
            {
                continue;
            }

            if (ReadDisjointFrameOwnership(
                body,
                start,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } frame
                || !frame.Uses.ContainsKey(allocation) || frame.Uses.Count > 2
                || frame.Uses.Keys.Any(other => other != allocation
                    && (body[other].Definition.Opcode != TrimFrame || body[other].Operands[0].Value <= slot.Value)))
            {
                continue;
            }

            List<int> reads = [];
            for (int index = clear + 1; index < end; index++)
            {
                if (frame.Owners[index - start] != allocation
                    || !body[index].Operands.Any(operand => SameProjectionRegister(operand, slot)))
                {
                    continue;
                }

                if (body[index] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var value, { Kind: BeamOperandParameters.X, TypeIndex: null }]
                    } || value != slot)
                {
                    reads.Clear();
                    break;
                }

                reads.Add(index);
            }

            if (reads.Count < 2)
            {
                continue;
            }

            // The original frame never redefines this slot or carries it
            // through a trim. Its immutable literal already has a table root.
            int offset = body[definition].Offset + 1;
            var cursor = new BeamCursor(code[offset..body[clear].Offset]);
            _ = BeamCompactReader.Read(cursor);
            if (literal.Kind == BeamOperandParameters.LiteralValue)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            ReadOnlySpan<byte> valueBytes = code.Span.Slice(offset, cursor.Position);
            output.Write(code.Span[copied..body[definition].Offset]);
            WriteFrameInitialization(output, Enumerable.Range(0, (int)width.Value).ToArray());
            copied = body[clear + 1].Offset;
            foreach (int read in reads)
            {
                output.Write(code.Span[copied..(body[read].Offset + 1)]);
                output.Write(valueBytes);
                WriteRegister(output, body[read].Operands[1]);
                copied = body[read + 1].Offset;
            }

            if (frame.Uses.Count == 1)
            {
                untrimmedEntries.Add((int)label.Value);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();
        if (untrimmedEntries.Count == 0 || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return rewritten;
        }

        return RewriteDisjointFrames(
            rewritten,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            entriesToCompact: untrimmedEntries
        ) ?? rewritten;
    }
}
