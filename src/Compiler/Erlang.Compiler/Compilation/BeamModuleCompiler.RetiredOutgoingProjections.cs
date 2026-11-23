using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardRetiredOutgoingProjections(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
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
            if (ReadOutgoingProjectionEntryHomes(body, entry, end) is not { } homes)
            {
                continue;
            }

            bool? owned = null;
            for (int index = homes.AfterSaves; index < end; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands:
                        [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                        { Kind: BeamOperandParameters.Unsigned } field,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                        ]
                    } || !homes.Slots.Contains((int)saved.Value)
                    || FindOutgoingSlotReload(
                        body,
                        index,
                        end,
                        (int)saved.Value
                    ) is not { } reload
                    || body[reload.Call].Definition.Opcode is not (LocalCall or ExternalCall)
                    || !(owned ??= ReadDisjointFrameOwnership(body, entry, end)?.Uses.Count == 1)
                    || !(flow ??= new BeamProjectionFlow(body)).IsDead(reload.Call, saved, allowBadRecordExit: true))
                {
                    continue;
                }

                int clear = reload.Call;
                while (body[clear - 1].Definition.Opcode == Line)
                {
                    clear--;
                }

                // The projected value only reaches this outgoing X register.
                // Clear the old frame home at the first GC boundary after the transfer.
                output.Write(code.Span[copied..body[index].Offset]);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, source);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                WriteRegister(output, reload.Destination);
                output.Write(code.Span[body[index + 1].Offset..body[reload.Index].Offset]);
                output.Write(code.Span[body[reload.Index + 1].Offset..body[clear].Offset]);
                WriteFrameInitialization(output, [(int)saved.Value]);
                copied = body[clear].Offset;
                index = reload.Call;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, InitYRegisters);
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int AfterSaves, HashSet<int> Slots)? ReadOutgoingProjectionEntryHomes(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        int allocation = -1;
        for (int index = start; index < end; index++)
        {
            byte opcode = body[index].Definition.Opcode;
            if (opcode is TrimFrame or CatchEnter or CatchEnd or TryEnter or TryEnd or TryCase
                or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout or RemoveMessage or ReceiveTimeout
                or DebugLine or ExecutableLine || body[index].Definition.Name is "recv_mark" or "recv_set")
            {
                return null;
            }

            if (opcode is Allocate or AllocateHeap)
            {
                if (allocation >= 0)
                {
                    return null;
                }

                allocation = index;
            }
        }

        if (allocation < 0)
        {
            return null;
        }

        var slots = new HashSet<int>();
        int next = allocation + 1;
        for (; next < end; next++)
        {
            BeamInstruction instruction = body[next];
            if (instruction.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            if (instruction is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.Y } slot]
                })
            {
                break;
            }

            slots.Add((int)slot.Value);
        }

        return slots.Count == 0 ? null : (next, slots);
    }
}
