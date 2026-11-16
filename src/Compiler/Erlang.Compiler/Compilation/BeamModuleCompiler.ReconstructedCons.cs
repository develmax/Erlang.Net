using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ReuseReconstructedCons(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 3 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: GetList, Operands: [var list, var head, var tail] }
                || body[index + 1] is not { Definition.Opcode: TestHeap } heap
                || body[index + 2] is not
                    { Definition.Opcode: PutList, Operands: [var first, var rest, var result] }
                || head != first || tail != rest || head == tail
                || list.TypeIndex is not null || head.TypeIndex is not null || tail.TypeIndex is not null
                || result is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                || SameProjectionRegister(list, head) || SameProjectionRegister(list, tail)
                || ReadHeapWords(code, heap) is not { Words: ConsHeapWords })
            {
                continue;
            }

            int next = index + 3;
            while (next < body.Count && body[next].Definition.Opcode == Line)
            {
                next++;
            }

            if (next == body.Count || body[next].Definition.Opcode is not
                (LocalCall or ExternalCall or TailLocalCall or TailExternalCall
                    or TailLocalCallWithFrame or TailExternalCallWithFrame or Return))
            {
                continue;
            }

            // The call or return ends the allocation region, and the original
            // cons still contains exactly the pair that would be allocated here.
            output.Write(code.Span[copied..heap.Offset]);
            if (list != result)
            {
                output.WriteByte(Move);
                WriteRegister(output, list);
                WriteRegister(output, result);
            }

            copied = body[index + 3].Offset;
            index += 2;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        return output.ToArray();
    }
}
