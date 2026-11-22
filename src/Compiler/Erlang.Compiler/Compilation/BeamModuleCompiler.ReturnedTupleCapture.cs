using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PlaceReturnedTupleCaptureBeforeCheck(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteReturnedTupleCapture(code, body);
    }

    private static byte[]? RewriteReturnedTupleCapture(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = -1;
        bool? unprotected = null;
        for (int check = 1; check + 4 < body.Count; check++)
        {
            if (body[check].Definition.Opcode == FunctionInfo)
            {
                entry = check + 1;
                unprotected = null;
            }

            if (entry < 0 || body[check - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[check] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var tuple, _, _]
                } || tuple != x0 || body[check + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var returned, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || returned != x0 || body[check + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var projected, _, _]
                } || projected != x0 || !(unprotected ??= HasUnprotectedCallKillSchedule(body, entry)))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetForwardLabel(check, (int)rejected.Value, out int fallback)
                || fallback <= check + 2 || fallback + 2 >= body.Count
                || body[fallback + 1] is not
                {
                    Definition.Opcode: Deallocate,
                    Operands: [var width]
                } || saved.Value >= width.Value || body[fallback + 2].Definition.Opcode != Return)
            {
                continue;
            }

            // The fallback returns the same X0 directly. Capturing it before
            // the test only writes a slot that the fallback discards.
            int test = body[check].Offset;
            int save = body[check + 1].Offset;
            int next = body[check + 2].Offset;
            output.Write(code.Span[copied..test]);
            output.Write(code.Span[save..next]);
            output.Write(code.Span[test..save]);
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
