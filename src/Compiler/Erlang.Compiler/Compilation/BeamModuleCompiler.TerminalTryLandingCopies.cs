using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveTerminalTryLandingCopies(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TryCase)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand exceptionClass = new(BeamOperandParameters.X, BeamTryParameters.CaughtClassRegister);
        BeamOperand reason = new(BeamOperandParameters.X, BeamTryParameters.ReasonRegister);
        BeamOperand trace = new(BeamOperandParameters.X, BeamTryParameters.SavedTraceRegister);
        using var output = new MemoryStream();
        int copied = 0;
        for (int landing = 0; landing + 7 < body.Count; landing++)
        {
            if (body[landing] is not { Definition.Opcode: TryCase, Operands: [var token] }
                || body[landing + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var incomingClass, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedClass]
                } || incomingClass != exceptionClass
                || body[landing + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var incomingReason, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedReason]
                } || incomingReason != reason
                || !ReadsLandingCopy(body[landing + 3], trace, exceptionClass)
                || body[landing + 4] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var incomingTrace, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedTrace]
                } || incomingTrace != exceptionClass
                || savedClass == savedReason || savedClass == savedTrace || savedReason == savedTrace
                || body[landing + 5] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil },
                        var result]
                } || result != exceptionClass
                || body[landing + 6] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                || token.Value >= width.Value || savedClass.Value >= width.Value || savedReason.Value >= width.Value
                || savedTrace.Value >= width.Value || body[landing + 7].Definition.Opcode != Return)
            {
                continue;
            }

            // The class register is overwritten and the three saved values
            // die with this frame. No intervening label or GC can read them.
            output.Write(code.Span[copied..body[landing + 1].Offset]);
            copied = body[landing + 5].Offset;
            landing += 7;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
