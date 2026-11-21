using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderAdjacentTupleProjections(ReadOnlyMemory<byte> code, int labelCount)
    {
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
        int copied = 0;
        for (int start = 0; start + 1 < body.Count; start++)
        {
            if (body[start] is not { Definition.Opcode: GetTupleElement, Operands: [var source, _, _] }
                || !IsBareProjectionRegister(source))
            {
                continue;
            }

            int end = start + 1;
            bool ordered = true;
            while (end + 1 < body.Count && body[end] is
                { Definition.Opcode: GetTupleElement, Operands: [var tuple, var field, _] } && tuple == source)
            {
                ordered &= body[end - 1].Operands[1].Value < field.Value;
                end++;
            }

            if (!ordered && AreIndependentTupleProjections(
                body,
                start,
                end,
                source
            ))
            {
                output.Write(code.Span[copied..body[start].Offset]);
                foreach (int index in Enumerable.Range(start, end - start)
                    .OrderBy(index => body[index].Operands[1].Value))
                {
                    output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                }

                copied = body[end].Offset;
            }

            start = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool AreIndependentTupleProjections(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        BeamOperand source
    )
    {
        // Unlike SSA variables, physical registers can alias. Keep a run
        // untouched if any projection overwrites the tuple or another result.
        var destinations = new HashSet<BeamOperand>();
        var fields = new HashSet<int>();
        for (int index = start; index < end; index++)
        {
            var field = body[index].Operands[1];
            var destination = body[index].Operands[2];
            if (!IsBareProjectionRegister(destination) || destination == source
                || !destinations.Add(destination) || !fields.Add((int)field.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsBareProjectionRegister(BeamOperand operand) =>
        operand is { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null };
}
