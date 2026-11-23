using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveOwnedSendMessageSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Send)
            || !code.Span.Contains(PutTuple) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
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
            if (start + 5 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var label] }
                || Enumerable.Range(start, end - start).Any(index => body[index].Definition.Opcode == ExecutableLine))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int tuple = start + 1; tuple + 4 < end; tuple++)
            {
                if (body[tuple] is not { Definition.Opcode: PutTuple, Operands: [var destination, ..] }
                    || destination != x0
                    || body[tuple + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } message]
                    } || result != x0
                    || body[tuple + 2] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } recipient, var argument]
                    }
                    || recipient == message || argument != x0 || !IsMove(body[tuple + 3], message, x1))
                {
                    continue;
                }

                int send = tuple + 4;
                while (send < end && body[send].Definition.Opcode == Line)
                {
                    send++;
                }

                if (send >= end || body[send].Definition.Opcode != Send
                    || (ownership ??= ReadDisjointFrameOwnership(body, start, end)) is not
                    { Owners: var owners, Uses: var frames }
                    || frames.Count != 1 || owners[tuple - start] < 0
                    || owners[send - start] != owners[tuple - start]
                    || body[owners[tuple - start]].Definition.Opcode != Allocate
                    || Enumerable.Range(start, end - start)
                        .Where(index => body[index].Definition.Opcode != InitYRegisters)
                        .Sum(index => body[index].Operands.Count(value => SameProjectionRegister(value, message))) != 2)
                {
                    continue;
                }

                // The constructor reads its inputs before X1 is overwritten.
                // The receiver is a different saved value, loaded only after
                // construction. The old message slot has no other definition
                // or consumer, including across a receive or collecting call.
                output.Write(code.Span[copied..body[tuple].Offset]);
                output.WriteByte(PutTuple);
                WriteRegister(output, x1);
                int destinationOffset = body[tuple].Offset + 1;
                var cursor = new BeamCursor(code[destinationOffset..]);
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[(destinationOffset + cursor.Position)..body[tuple + 1].Offset]);
                output.Write(code.Span[body[tuple + 2].Offset..body[tuple + 3].Offset]);
                copied = body[tuple + 4].Offset;
                entries.Add((int)label.Value);
                break;
            }
        }

        if (copied == 0)
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

    private byte[]? BuildRetainedResultSendTuple(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Send)
            || !code.Span.Contains(PutTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
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
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int tuple = start + 2; tuple + 6 < end; tuple++)
            {
                if (body[tuple] is not { Definition.Opcode: PutTuple, Operands: [var destination, var count, ..] } constructor
                    || destination != x0
                    || body[tuple - 2] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } scratch]
                    } || result != x0
                    || body[tuple - 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                    || ReadHeapWords(code, body[tuple - 1]) is not { End: var live, Words: var words }
                    || words != count.Value + 1
                    || !constructor.Operands.Skip(2).Contains(scratch)
                    || constructor.Operands.Skip(2).Any(operand => operand.TypeIndex is not null
                        || operand.Kind is not (BeamOperandParameters.Y or BeamOperandParameters.Atom
                            or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue))
                    || !IsMove(body[tuple + 1], x0, scratch)
                    || body[tuple + 2] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } receiver, var target]
                    } || receiver == scratch || target != x0
                    || !IsMove(body[tuple + 3], scratch, x1))
                {
                    continue;
                }

                int send = SkipLines(tuple + 4, end);
                if (send >= end || body[send].Definition.Opcode != Send)
                {
                    continue;
                }

                int release = SkipLines(send + 1, end);
                if (release < end && body[release] is
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Atom, TypeIndex: null }, var returned]
                    } && returned == x0)
                {
                    release++;
                }

                if (release + 1 >= end
                    || body[release] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                    || width.Value <= 0 || body[release + 1].Definition.Opcode != Return
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { Owners: var owners, Uses: var frames })
                {
                    continue;
                }

                int frame = owners[tuple - 2 - start];
                if (frame < 0 || owners[release - start] != frame || frames[frame].Length != width.Value)
                {
                    continue;
                }

                // X0 remains a GC root until its value is inside the tuple.
                // The receiver is the final Y read on this terminal branch.
                output.Write(code.Span[copied..body[tuple - 2].Offset]);
                output.Write(code.Span[body[tuple - 1].Offset..live]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                output.WriteByte(PutTuple);
                WriteRegister(output, x1);
                output.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, count.Value);
                foreach (BeamOperand field in constructor.Operands.Skip(2))
                {
                    if (field.Kind == BeamOperandParameters.Y)
                    {
                        WriteRegister(output, field == scratch ? x0 : field);
                    }
                    else
                    {
                        WriteTupleJoinOperand(output, field);
                    }
                }

                output.Write(code.Span[body[tuple + 2].Offset..body[tuple + 3].Offset]);
                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width.Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                output.Write(code.Span[body[tuple + 4].Offset..body[release].Offset]);
                output.WriteByte(Deallocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                copied = body[release + 1].Offset;
                tuple = release;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int SkipLines(int index, int end)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }
    }

    private static byte[]? ConstructSendTupleInArgument(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        const int OneSavedRecipient = 1;
        const int EmptySendFrame = 0;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 7 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: AllocateHeap, Operands: [{ Value.IsOne: true }, _, _] }
                || body[index + 1] is not
                {
                    Definition.Opcode: Move, Operands:
                    [
                    { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null } recipient,
                    { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null } savedRecipient
                    ]
                }
                || body[index + 2] is not { Definition.Opcode: PutTuple } tuple
                || tuple.Operands[0] != recipient
                || tuple.Operands.Skip(2).Any(operand => operand.Kind == BeamOperandParameters.Y)
                || body[index + 3] is not
                {
                    Definition.Opcode: Move, Operands:
                    [
                        var constructed,
                    { Kind: BeamOperandParameters.X, Value.IsOne: true, TypeIndex: null } messageArgument
                    ]
                }
                || constructed != recipient
                || body[index + 4] is not { Definition.Opcode: Move, Operands: [var saved, var restored] }
                || saved != savedRecipient
                || restored != recipient)
            {
                continue;
            }

            int send = index + 5;
            if (body[send].Definition.Opcode == Line)
            {
                send++;
            }

            if (body[send].Definition.Opcode != Send)
            {
                continue;
            }

            int deallocate = send + 1;
            if (body[deallocate].Definition.Opcode == Line)
            {
                deallocate++;
            }

            if (deallocate < body.Count && body[deallocate] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom, TypeIndex: null }, var returned]
                } && returned == recipient)
            {
                deallocate++;
            }

            if (deallocate + 1 >= body.Count
                || body[deallocate] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                || width.Value != OneSavedRecipient
                || body[deallocate + 1].Definition.Opcode != Return)
            {
                continue;
            }

            // The one slot protects only the original recipient. With no Y reads in
            // the constructor or suffix, retaining X0 and writing X1 removes that slot.
            // The send body call still requires a frame.
            int widthOffset = body[index].Offset + 1;
            output.Write(code.Span[copied..widthOffset]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, EmptySendFrame);
            output.Write(code.Span[OperandEnd(widthOffset)..body[index + 1].Offset]);
            output.WriteByte(PutTuple);
            WriteRegister(output, messageArgument);
            output.Write(code.Span[OperandEnd(tuple.Offset + 1)..body[index + 3].Offset]);
            output.Write(code.Span[body[index + 5].Offset..body[deallocate].Offset]);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, EmptySendFrame);
            copied = body[deallocate + 1].Offset;
            index = deallocate;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int OperandEnd(int offset)
        {
            var cursor = new BeamCursor(code[offset..]);
            _ = BeamCompactReader.Read(cursor);

            return offset + cursor.Position;
        }
    }
}
