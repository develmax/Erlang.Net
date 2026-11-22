using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PlaceReturnedStatusCaptureBeforeCheck(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteReturnedStatusCapture(code, body);
    }

    private static byte[]? RewriteReturnedStatusCapture(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            for (int check = start + 1; check + 10 < end; check++)
            {
                if (body[check - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[check] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [{ Kind: BeamOperandParameters.Label } rejected, var tested, var expected]
                    } || tested != x0 || !IsNonRegisterValue(expected)
                    || body[check + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } success, var answer]
                    } || answer != x0 || body[check + 2] is not
                    { Definition.Opcode: Deallocate, Operands: [var width] }
                    || body[check + 3].Definition.Opcode != Return)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.TryGetSingleForwardTarget(check, (int)rejected.Value, out int failure)
                    || failure <= check + 3 || failure + 6 >= end
                    || body[failure + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var returned, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                    } || returned != x0 || slot == success || slot.Value + 1 != width.Value
                    || body[failure + 2] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } cleanupInput, var target]
                    } || target != x0 || cleanupInput == slot
                    || body[failure + 3] is not
                    {
                        Definition.Opcode: TrimFrame,
                        Operands: [var removed, { Value.IsOne: true }]
                    } || removed.Value != slot.Value)
                {
                    continue;
                }

                int cleanup = failure + 4;
                while (cleanup < end && body[cleanup].Definition.Opcode == Line)
                {
                    cleanup++;
                }

                if (cleanup + 3 >= end || body[cleanup] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [{ Value.IsOne: true }, _]
                    } || body[cleanup + 1] is not { Definition.Opcode: Move, Operands: [var retained, var result] }
                    || retained != y0 || result != x0 || body[cleanup + 2] is not
                    { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                    || body[cleanup + 3].Definition.Opcode != Return
                    || !HasUnprotectedCallKillSchedule(body, start)
                    || ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    ) is not { } frame)
                {
                    continue;
                }

                int owner = frame.Owners[check - start];
                if (owner < 0 || frame.Owners[failure - start] != owner)
                {
                    continue;
                }

                // Ordinary calls save their result before a non-collecting
                // check. Success discards this slot and the sole failure
                // edge still retains the same value through the trim.
                int position = body[check].Offset;
                int former = body[failure + 1].Offset;
                int following = body[failure + 2].Offset;
                output.Write(code.Span[copied..position]);
                output.Write(code.Span[former..following]);
                output.Write(code.Span[position..former]);
                copied = following;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
