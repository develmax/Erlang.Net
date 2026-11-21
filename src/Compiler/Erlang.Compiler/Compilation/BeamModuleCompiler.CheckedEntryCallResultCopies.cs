using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DelayCheckedEntryCallResultCopies(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        foreach (var schedule in ReadCheckedEntryCallSchedules(body))
        {
            var spill = body[schedule.Spill];
            var projection = body[schedule.Projection];
            int next = body[schedule.Projection + 1].Offset;
            output.Write(code.Span[copied..spill.Offset]);
            if (schedule.IsUnary)
            {
                // The previous ordinary call left only its result in X0.
                // Keep that result live while the checked field is projected into X1.
                output.WriteByte(GetTupleElement);
                WriteRegister(output, projection.Operands[0]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, projection.Operands[1].Value);
                BeamCompactWriter.Write(output, XRegisterTag, 1);
            }
            else
            {
                output.Write(code.Span[projection.Offset..next]);
            }

            output.Write(code.Span[spill.Offset..projection.Offset]);
            if (schedule.IsUnary)
            {
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, XRegisterTag, 1);
                BeamCompactWriter.Write(output, XRegisterTag, 0);
            }

            copied = next;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
