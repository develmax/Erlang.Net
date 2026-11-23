using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardClearedCallResultToUnaryBif(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || !code.Span.Contains(BifOne)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var result = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        int nextEntry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = nextEntry;
            nextEntry = end + 1;
            for (int clear = entry; clear + 4 < end; clear++)
            {
                if (body[clear].Definition.Opcode != InitYRegisters)
                {
                    continue;
                }

                int call = clear + 1;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call + 3 >= end || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[call + 1] is not { Definition.Opcode: Move, Operands: [var from, var slot] }
                    || from != result || slot is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                    || !body[clear].Operands.Contains(slot)
                    || body[call + 2] is not
                    { Definition.Opcode: BifOne, Operands: [{ Value.IsZero: true }, _, var input, var destination] }
                    || input != slot || destination != result
                    || ReadDisjointFrameOwnership(
                        body,
                        entry,
                        end,
                        trackSiblingTrimEpochs: true
                    ) is not { } frame
                    || frame.Owners[clear - entry] < 0
                    || frame.Owners[call + 2 - entry] != frame.Owners[clear - entry]
                    || !(flow ??= new BeamProjectionFlow(body)).IsDead(
                        call + 3,
                        slot,
                        afterCall: true,
                        allowEntryFramePredicates: true
                    ))
                {
                    continue;
                }

                // The cleared slot stays initialized. The unary BIF reads the
                // ordinary call result directly before any other X clobber.
                output.Write(code.Span[copied..body[call + 1].Offset]);
                int bif = body[call + 2].Offset;
                var cursor = new BeamCursor(code[(bif + 1)..]);
                _ = BeamCompactReader.Read(cursor);
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[bif..(bif + 1 + cursor.Position)]);
                WriteRegister(output, result);
                WriteRegister(output, result);
                copied = body[call + 3].Offset;
                clear = call + 2;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
