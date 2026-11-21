using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ProjectEntryListDirectlyIntoFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 6 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var incoming] }
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } source]
                }
                || source.Value >= incoming.Value)
            {
                continue;
            }

            int projection = info + 3;
            while (body[projection].Definition.Opcode == Line)
            {
                projection++;
            }

            if (projection + 2 >= body.Count || body[projection] is not
                {
                    Definition.Opcode: GetList,
                    Operands: [var list, { Kind: BeamOperandParameters.X, TypeIndex: null } head,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } tail]
                }
                || list != source || head == tail || head.Value < incoming.Value || tail.Value < incoming.Value
                || head.Value + tail.Value != 2 * incoming.Value + 1)
            {
                continue;
            }

            BeamInstruction frame = body[projection + 1];
            int oldLive = (int)incoming.Value + 2;
            if (frame.Definition.Opcode is not (Allocate or AllocateHeap)
                || frame.Operands[0].Value < 2 || frame.Operands[^1].Value != oldLive)
            {
                continue;
            }

            int firstCopy = projection + 2;
            while (body[firstCopy].Definition.Opcode == InitYRegisters)
            {
                firstCopy++;
            }

            if (ReadProjectedFrameListCopies(
                body,
                firstCopy,
                frame,
                incoming,
                head,
                tail
            ) is not { } copies)
            {
                continue;
            }

            int call = copies.End;
            while (body[call].Definition.Opcode is Line or Move)
            {
                call++;
            }

            if (body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || !(flow ??= new BeamProjectionFlow(body)).IsEntryTemporaryDead(copies.End, head)
                || !flow.IsEntryTemporaryDead(copies.End, tail))
            {
                continue;
            }

            output.Write(code.Span[copied..body[projection].Offset]);
            output.Write(code.Span[frame.Offset..(body[projection + 2].Offset - CompactRegisterCountWidth(oldLive))]);
            // Before the projection, the original list roots both values.
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, incoming.Value);
            output.Write(code.Span[body[projection + 2].Offset..body[firstCopy].Offset]);
            for (int index = firstCopy; index < copies.End; index++)
            {
                if (body[index].Operands[0].Value < incoming.Value)
                {
                    output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                }
            }

            output.WriteByte(GetList);
            WriteRegister(output, source);
            WriteRegister(output, copies.Head);
            WriteRegister(output, copies.Tail);
            copied = body[copies.End].Offset;
            info = copies.End - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int End, BeamOperand Head, BeamOperand Tail)? ReadProjectedFrameListCopies(
        IReadOnlyList<BeamInstruction> body,
        int first,
        BeamInstruction frame,
        BeamOperand incoming,
        BeamOperand head,
        BeamOperand tail
    )
    {
        var written = new HashSet<BeamOperand>();
        BeamOperand? headSlot = null;
        BeamOperand? tailSlot = null;
        int index = first;
        for (; body[index] is
        {
            Definition.Opcode: Move,
            Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } source,
            { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
        }; index++)
        {
            if (slot.Value >= frame.Operands[0].Value || !written.Add(slot))
            {
                return null;
            }

            if (source == head && headSlot is null)
            {
                headSlot = slot;
            }
            else if (source == tail && tailSlot is null)
            {
                tailSlot = slot;
            }
            else if (source.Value >= incoming.Value)
            {
                return null;
            }
        }

        return headSlot is not null && tailSlot is not null ? (index, headSlot, tailSlot) : null;
    }
}
