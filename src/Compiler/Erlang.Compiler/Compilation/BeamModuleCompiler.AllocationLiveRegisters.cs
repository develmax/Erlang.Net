using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? TrimAllocationLiveRegisters(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is not (Allocate or AllocateHeap))
            {
                continue;
            }

            int original = (int)instruction.Operands[^1].Value;
            int live = original;
            while (live > 0 && (flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(
                index,
                new BeamOperand(BeamOperandParameters.X, live - 1)
            ))
            {
                live--;
            }

            if (live == original)
            {
                continue;
            }

            int end = body[index + 1].Offset;
            int width = original < 1 << BeamWire.CompactSmallPayloadShift ? 1 : 2;
            output.Write(code.Span[copied..(end - width)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
            copied = end;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
