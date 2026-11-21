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
                || frame.Operands[0].Value < 1 || frame.Operands[^1].Value != oldLive)
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
            ) is not { } copies || copies is { Head: null, Tail: null })
            {
                continue;
            }

            int call = copies.End;
            while (body[call].Definition.Opcode is Line or Move)
            {
                call++;
            }

            if (body[call].Definition.Opcode is not (LocalCall or ExternalCall))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            bool mixedDestinations = copies.Head is null || copies.Tail is null;
            BeamOperand? directTemporary = mixedDestinations ? copies.Head is null ? head : tail : null;
            int fusedCopy = -1;
            BeamOperand? fusedDestination = null;
            IReadOnlyDictionary<int, BeamOperand>? outgoingCopies = null;
            if (mixedDestinations || !flow.IsEntryTemporaryDead(copies.End, head) || !flow.IsEntryTemporaryDead(copies.End, tail))
            {
                if (!flow.IsEntryTemporaryDead(call, head) || !flow.IsEntryTemporaryDead(call, tail)
                    || ReadProjectedFrameOutgoingCopies(
                        body,
                        copies.End,
                        call,
                        (head, copies.Head),
                        (tail, copies.Tail),
                        directTemporary
                    ) is not { } outgoing)
                {
                    continue;
                }

                (outgoingCopies, fusedCopy, fusedDestination) = outgoing;
            }

            if ((copies.Head ?? fusedDestination) is not { } headDestination
                || (copies.Tail ?? fusedDestination) is not { } tailDestination)
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
            WriteRegister(output, headDestination);
            WriteRegister(output, tailDestination);
            int end = copies.End;
            if (outgoingCopies is not null)
            {
                for (int index = end; index < call; index++)
                {
                    if (index == fusedCopy)
                    {
                        continue;
                    }

                    if (outgoingCopies.TryGetValue(index, out var slot))
                    {
                        output.WriteByte(Move);
                        WriteRegister(output, slot);
                        WriteRegister(output, body[index].Operands[1]);
                    }
                    else
                    {
                        output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                    }
                }

                end = call;
            }

            copied = body[end].Offset;
            info = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (IReadOnlyDictionary<int, BeamOperand> Aliases, int FusedCopy, BeamOperand? Destination)?
        ReadProjectedFrameOutgoingCopies(
            IReadOnlyList<BeamInstruction> body,
            int start,
            int call,
            (BeamOperand Temporary, BeamOperand? Slot) head,
            (BeamOperand Temporary, BeamOperand? Slot) tail,
            BeamOperand? directTemporary
        )
    {
        var copies = new Dictionary<int, BeamOperand>();
        HashSet<BeamOperand>? earlierUses = directTemporary is null ? null : [];
        BeamOperand? fusedDestination = null;
        int fusedCopy = -1;
        for (int index = start; index < call; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            // The initial saves own both values throughout argument setup.
            // Reject definitions that change either the scratch or saved value.
            if (instruction is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                }
                || SameProjectionRegister(destination, head.Temporary)
                || SameProjectionRegister(destination, tail.Temporary))
            {
                return null;
            }

            // A direct X definition moves before the argument setup. Its old
            // value must be untouched until the original copy, and the new
            // value must reach the call without another definition.
            if (directTemporary is { } temporary && SameProjectionRegister(source, temporary))
            {
                if (source.TypeIndex is not null || fusedCopy >= 0
                    || destination.Value >= body[call].Operands[0].Value || earlierUses!.Contains(destination))
                {
                    return null;
                }

                fusedDestination = destination;
                fusedCopy = index;
                continue;
            }

            if (fusedCopy >= 0 && destination == fusedDestination)
            {
                return null;
            }

            earlierUses?.Add(source with { TypeIndex = null });
            earlierUses?.Add(destination);
            if (SameProjectionRegister(source, head.Temporary) || SameProjectionRegister(source, tail.Temporary))
            {
                BeamOperand? slot = SameProjectionRegister(source, head.Temporary) ? head.Slot : tail.Slot;
                if (source.TypeIndex is not null || slot is null)
                {
                    return null;
                }

                copies.Add(index, slot);
            }
        }

        if (directTemporary is not null ? fusedCopy < 0 : copies.Count == 0)
        {
            return null;
        }

        return (copies, fusedCopy, fusedDestination);
    }

    private static (int End, BeamOperand? Head, BeamOperand? Tail)? ReadProjectedFrameListCopies(
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

        return (index, headSlot, tailSlot);
    }
}
