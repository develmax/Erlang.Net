using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? MoveHeapTestsBeforeNeutralInstructions(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 1; index + 1 < body.Count; index++)
        {
            BeamInstruction heap = body[index];
            if (heap.Definition.Opcode != TestHeap || heap.Operands[^1].Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int original = (int)heap.Operands[^1].Value;
            int live = original;
            int first = index;
            while (first > 0)
            {
                int? source = null;
                int destination;
                switch (body[first - 1])
                {
                    case
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [
                            { Kind: BeamOperandParameters.X, TypeIndex: null } tuple,
                            _,
                            { Kind: BeamOperandParameters.X, TypeIndex: null } field
                        ]
                    } when tuple.Value < live:
                        source = (int)tuple.Value;
                        destination = (int)field.Value;
                        break;
                    case
                    {
                        Definition.Opcode: BifZero,
                        Operands: [var import, { Kind: BeamOperandParameters.X, TypeIndex: null } result]
                    } when IsHeapNeutralNullaryBif(importsByIndex[(int)import.Value], atomNames):
                        destination = (int)result.Value;
                        break;
                    default:
                        destination = -1;
                        break;
                }

                if (destination < 0)
                {
                    break;
                }

                if (destination == live - 1 && source != destination)
                {
                    live--;
                }
                else if (destination < live && source != destination)
                {
                    // Dropping an interior register would leave an unproven slot in the GC root prefix.
                    break;
                }

                first--;
            }

            if (live == original)
            {
                continue;
            }

            int start = body[first].Offset;
            int end = body[index + 1].Offset;
            int width = original < 1 << BeamWire.CompactSmallPayloadShift ? 1 : 2;
            output.Write(code.Span[copied..start]);
            output.Write(code.Span[heap.Offset..(end - width)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
            output.Write(code.Span[start..heap.Offset]);
            copied = end;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        return output.ToArray();
    }

    private static bool IsHeapNeutralNullaryBif(
        (int Module, int Function, int Arity) import,
        string[] atomNames
    ) => import.Arity == 0 && atomNames[import.Module - 1] == ErlangModuleNames.Module
        && atomNames[import.Function - 1] is AutoImportNames.Self or AutoImportNames.Node;
}
