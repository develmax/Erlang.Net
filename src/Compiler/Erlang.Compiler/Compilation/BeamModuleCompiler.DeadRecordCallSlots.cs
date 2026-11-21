using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ClearDeadRecordCallSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || (!code.Span.Contains(UpdateRecord) && !code.Span.Contains(PutList)))
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

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 4 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int entry = info + 1;
            int end = entry;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int call = entry + 2; call + 1 < end; call++)
            {
                if ((ReadRecordCallSlotUses(body, entry, call)
                    ?? ReadTupleListCallSlotUses(body, entry, call)) is not { } setup)
                {
                    continue;
                }

                if ((ownership ??= ReadDisjointFrameOwnership(body, entry, end)) is not { } frame)
                {
                    break;
                }

                int owner = frame.Owners[call - entry];
                if (owner < 0 || frame.Owners[setup.Start - entry] != owner)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                var clear = new SortedSet<int>(setup.Existing);
                foreach (BeamOperand slot in setup.Reads)
                {
                    if (!clear.Contains((int)slot.Value) && flow.IsInitialized(setup.Start, slot)
                        && flow.IsDead(call, slot))
                    {
                        clear.Add((int)slot.Value);
                    }
                }

                if (clear.Count == setup.Existing.Length)
                {
                    continue;
                }

                output.Write(code.Span[copied..body[setup.Insert].Offset]);
                WriteFrameInitialization(output, clear.ToArray());
                copied = body[setup.Initialize ? setup.Insert + 1 : setup.Insert].Offset;
                highestOpcode = Math.Max(highestOpcode, InitYRegisters);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Start, int Insert, bool Initialize, int[] Existing, BeamOperand[] Reads)? ReadRecordCallSlotUses(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int call
    )
    {
        if (body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] })
        {
            return null;
        }

        int index = call - 1;
        while (index > entry && body[index].Definition.Opcode == Line)
        {
            index--;
        }

        bool initialize = body[index].Definition.Opcode == InitYRegisters;
        int insert = initialize ? index : index + 1;
        int[] existing = initialize
            ? body[index--].Operands.Skip(1).Select(operand => (int)operand.Value).ToArray()
            : [];
        var reads = new List<BeamOperand>();
        var destinations = new HashSet<int>();
        for (; index > entry && body[index] is
        {
            Definition.Opcode: Move,
            Operands: [var source, { Kind: BeamOperandParameters.X } dest]
        }; index--)
        {
            if (dest.Value >= arity.Value || !destinations.Add((int)dest.Value))
            {
                return null;
            }

            if (source.Kind == BeamOperandParameters.Y)
            {
                reads.Add(source);
            }
        }

        BeamInstruction update = body[index];
        if (update.Definition.Opcode != UpdateRecord
            || update.Operands.Count < 7
            || update.Operands[2] is not { Kind: BeamOperandParameters.Y } record
            || update.Operands[3] is not { Kind: BeamOperandParameters.X } value
            || value.Value >= arity.Value || destinations.Contains((int)value.Value))
        {
            return null;
        }

        // The update base and replacement terms now live in the finished argument.
        // No Y register is written until the optional existing initializations.
        reads.Add(record);
        reads.AddRange(update.Operands.Skip(5).Where(operand => operand.Kind == BeamOperandParameters.Y));

        return (index, insert, initialize, existing, reads.ToArray());
    }
}
