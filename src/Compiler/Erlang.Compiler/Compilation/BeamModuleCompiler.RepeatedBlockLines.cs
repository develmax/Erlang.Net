using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ReadOnlyMemory<byte> RemoveRepeatedBlockLines(ReadOnlyMemory<byte> code)
    {
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                nextLabel - sharedReturnLabelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return code;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int removed = 0;
        int? previousLine = null;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Label or FunctionInfo or DebugLine or ExecutableLine)
            {
                // Keep the first location at a control-flow entry or after instrumentation.
                previousLine = null;
            }
            else if (opcode == Line)
            {
                int line = (int)instruction.Operands[0].Value;
                if (line == previousLine)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = body[index + 1].Offset;
                    removed++;
                }

                previousLine = line;
            }
        }

        if (removed == 0)
        {
            return code;
        }

        output.Write(code.Span[copied..]);
        lineInstructions -= removed;

        return output.ToArray();
    }
}
