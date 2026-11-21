using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RestoreCapturedArgumentAfterListHead(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(GetList) || !code.Span.Contains(Swap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var input = new BeamOperand(BeamOperandParameters.X, 0);
        var argument = new BeamOperand(BeamOperandParameters.X, 1);
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 3 < body.Count; index++)
        {
            if (body[index] is not
                { Definition.Opcode: Move, Operands: [var saved, { Kind: BeamOperandParameters.Y, TypeIndex: null } capture] }
                || saved != argument
                || body[index + 1] is not
                {
                    Definition.Opcode: GetList,
                    Operands: [var source, var head, { Kind: BeamOperandParameters.Y, TypeIndex: null } rest]
                }
                || source != input || head != input || rest == capture)
            {
                continue;
            }

            int transfer = index + 2;
            while (transfer + 1 < body.Count && body[transfer] is
                {
                    Definition.Opcode: Move,
                    Operands: [var constant, { Kind: BeamOperandParameters.X } register]
                }
                && IsNonRegisterValue(constant) && register.Value > argument.Value)
            {
                transfer++;
            }

            if (transfer + 1 == body.Count || body[transfer] is not { Definition.Opcode: Swap, Operands: [var first, var second] }
                || (first != input || second != argument) && (first != argument || second != input))
            {
                continue;
            }

            // The capture has already been saved. Project directly to the call
            // register and recover the capture after preparing the other operands.
            output.Write(code.Span[copied..body[index + 1].Offset]);
            output.WriteByte(GetList);
            WriteRegister(output, input);
            WriteRegister(output, argument);
            WriteRegister(output, rest);
            output.Write(code.Span[body[index + 2].Offset..body[transfer].Offset]);
            output.WriteByte(Move);
            WriteRegister(output, capture);
            WriteRegister(output, input);
            copied = body[transfer + 1].Offset;
            index = transfer;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
