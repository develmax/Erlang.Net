using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? FoldTerminalCalls(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            bool framed = body[index + 1].Definition.Opcode == Deallocate;
            int terminal = index + (framed ? 2 : 1);
            if (terminal + 1 >= body.Count || body[terminal].Definition.Opcode != Return)
            {
                continue;
            }

            byte opcode = instruction.Definition.Opcode switch
            {
                LocalCall => framed ? TailLocalCallWithFrame : TailLocalCall,
                ExternalCall => framed ? TailExternalCallWithFrame : TailExternalCall,
                DynamicApply when framed => TailDynamicApply,
                _ => 0
            };
            if (opcode == 0)
            {
                continue;
            }

            output.Write(code.Span[copied..instruction.Offset]);
            output.WriteByte(opcode);
            output.Write(code.Span[(instruction.Offset + 1)..body[index + 1].Offset]);
            if (framed)
            {
                output.Write(code.Span[(body[index + 1].Offset + 1)..body[terminal].Offset]);
            }

            copied = body[terminal + 1].Offset;
            index = terminal;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
