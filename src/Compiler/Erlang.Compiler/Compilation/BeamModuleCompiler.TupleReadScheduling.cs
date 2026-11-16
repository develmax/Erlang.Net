using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? SinkTupleReadsPastGuards(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 1; index + 2 < body.Count; index++)
        {
            if (body[index - 1] is not { Definition.Opcode: GetTupleElement, Operands: [var tuple, _, var tested] }
                || body[index] is not { Definition.Opcode: GetTupleElement, Operands: [var source, _, var destination] }
                || source != tuple || source.Kind != BeamOperandParameters.X || destination.Kind != BeamOperandParameters.X
                || SameProjectionRegister(tested, destination)
                || !IsTypeTest(body[index + 1], tested))
            {
                continue;
            }

            // On failure, the source register still holds an initialized term either way.
            // Other destinations may be newly live and must preserve the original heap roots.
            bool replacesTuple = SameProjectionRegister(source, destination);
            if (!(flow ??= new BeamProjectionFlow(body)).IsGuardTemporaryDeadOnFailure(
                index + 1, destination, preserveHeapRoots: !replacesTuple))
            {
                continue;
            }

            int read = body[index].Offset;
            int guard = body[index + 1].Offset;
            int next = body[index + 2].Offset;
            output.Write(code.Span[copied..read]);
            output.Write(code.Span[guard..next]);
            output.Write(code.Span[read..guard]);
            copied = next;
            index++;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        return output.ToArray();

        static bool IsTypeTest(BeamInstruction instruction, BeamOperand tested) =>
            instruction.Definition.Opcode is IsInteger or IsFloat or IsNumber or IsAtom
                or IsTuple or IsList or IsNonemptyList or IsNil or IsBinary or IsMap
            && instruction.Operands is [{ Kind: BeamOperandParameters.Label }, var value]
            && SameProjectionRegister(value, tested);
    }
}
