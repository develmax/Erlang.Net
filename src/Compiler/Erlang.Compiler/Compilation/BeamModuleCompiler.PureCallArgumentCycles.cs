using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FoldPureCallArgumentCycles(ReadOnlyMemory<byte> code, int labelCount) =>
        !ModuleOptimizationDisabled && code.Span.Contains(Move)
            && DecodeLateAtomicProjectionSlots(code, labelCount) is { } decoded
            ? RewritePureCallArgumentCycles(code, decoded.Body)
            : null;

    private static byte[]? RewritePureCallArgumentCycles(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index < body.Count; index++)
        {
            if (ReadPureCallArgumentCycle(body, index) is not { } plan)
            {
                continue;
            }

            output.Write(code.Span[copied..body[index].Offset]);
            var registers = plan.Registers;
            int first = registers.IndexOf(registers.Min());
            // beam_ssa_codegen orders the parallel argument copies by destination,
            // then walks the cycle from its lowest destination.
            for (int position = 1; position + 1 < registers.Count; position++)
            {
                WriteSwap(registers[(first + position + 1) % registers.Count],
                    registers[(first + position) % registers.Count]);
            }

            WriteSwap(registers[first], registers[(first + registers.Count - 1) % registers.Count]);
            index = plan.End - 1;
            copied = body[plan.End].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteSwap(int source, int destination)
        {
            output.WriteByte(Swap);
            BeamCompactWriter.Write(output, XRegisterTag, source);
            BeamCompactWriter.Write(output, XRegisterTag, destination);
        }
    }

    private static (int End, List<int> Registers)? ReadPureCallArgumentCycle(
        IReadOnlyList<BeamInstruction> body,
        int start
    )
    {
        if (body[start] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                { Kind: BeamOperandParameters.X, TypeIndex: null } scratch]
            } || source == scratch || source.Value >= FirstReservedXRegister)
        {
            return null;
        }

        int destination = (int)source.Value;
        var registers = new List<int> { destination };
        for (int index = start + 1; index < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } input,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } output]
                } || output.Value != destination)
            {
                return null;
            }

            if (input == scratch)
            {
                int afterMoves = index + 1;
                int call = afterMoves;
                while (call < body.Count && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                // A direct call uses only its declared X prefix and leaves the scratch dead.
                if (registers.Count > 1 && call < body.Count && body[call].Definition.Opcode
                    is LocalCall or ExternalCall or TailLocalCall or TailExternalCall
                        or TailLocalCallWithFrame or TailExternalCallWithFrame)
                {
                    var arity = body[call].Operands[0].Value;
                    if (scratch.Value >= arity && registers.All(register => register < arity))
                    {
                        return (afterMoves, registers);
                    }
                }

                return null;
            }

            if (input.Value >= FirstReservedXRegister || registers.Contains((int)input.Value))
            {
                return null;
            }

            destination = (int)input.Value;
            registers.Add(destination);
        }

        return null;
    }
}
