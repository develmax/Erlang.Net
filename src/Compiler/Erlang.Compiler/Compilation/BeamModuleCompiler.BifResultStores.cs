using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? StoreBifResultsDirectly(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction bif = body[index];
            if (bif.Definition.Opcode is not (BifZero or BifOne or BifTwo or BifThree
                or GcBifOne or GcBifTwo or GcBifThree)
                || bif.Operands[^1] is not { Kind: BeamOperandParameters.X, TypeIndex: null } result)
            {
                continue;
            }

            int next = index + 1;
            int copy = next;
            while (body[copy].Definition.Opcode == Line)
            {
                copy++;
            }

            if (body[copy] is not { Definition.Opcode: Move, Operands: [var source, var slot] }
                || source != result || slot is not { Kind: BeamOperandParameters.Y, TypeIndex: null })
            {
                continue;
            }

            int call = copy + 1;
            while (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || result.Value < body[call].Operands[0].Value)
            {
                continue;
            }

            // The failure edge skips the destination write, just as it skipped the move.
            // The next call overwrites the X registers and does not use this one as an argument.
            int registerWidth = result.Value < 1 << BeamWire.CompactSmallPayloadShift ? 1 : 2;
            int bifEnd = body[next].Offset;
            output.Write(code.Span[copied..(bifEnd - registerWidth)]);
            WriteRegister(output, slot);
            output.Write(code.Span[bifEnd..body[copy].Offset]);
            copied = body[copy + 1].Offset;
            index = copy;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
