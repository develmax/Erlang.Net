using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveUnusedTryLandingCopies(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TryCase)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

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
            if (ReadUnusedTryLandingCopies(
                body,
                start,
                end,
                ref flow
            ) is not { } copies)
            {
                continue;
            }

            output.Write(code.Span[copied..body[copies.Start].Offset]);
            copied = body[copies.End].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Start, int End)? ReadUnusedTryLandingCopies(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        int frame = -1;
        int installation = -1;
        int landing = -1;
        for (int index = start; index < end; index++)
        {
            byte opcode = body[index].Definition.Opcode;
            switch (opcode)
            {
                case Allocate or AllocateHeap when frame < 0:
                    frame = index;
                    break;
                case TryEnter when installation < 0:
                    installation = index;
                    break;
                case TryCase when landing < 0:
                    landing = index;
                    break;
                case Allocate or AllocateHeap or TryEnter or TryCase or CatchEnter or CatchEnd or TrimFrame
                    or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout or ReceiveTimeout or RemoveMessage
                    or ReceiveMarkerBind or ReceiveMarkerUse or ReceiveMarkerClear or ReceiveMarkerReserve
                    or DebugLine or ExecutableLine:
                    return null;
            }
        }

        if (frame < 0 || installation <= frame || landing <= installation || landing + 4 >= end
            || body[installation].Operands is not
            [
            { Kind: BeamOperandParameters.Y, TypeIndex: null } token,
            { Kind: BeamOperandParameters.Label } target
            ]
            || token.Value >= body[frame].Operands[0].Value
            || body[landing].Operands is not [var slot] || slot != token)
        {
            return null;
        }

        var exceptionClass = new BeamOperand(BeamOperandParameters.X, BeamTryParameters.CaughtClassRegister);
        var reason = new BeamOperand(BeamOperandParameters.X, BeamTryParameters.ReasonRegister);
        var trace = new BeamOperand(BeamOperandParameters.X, BeamTryParameters.SavedTraceRegister);
        int first = landing + 1;
        BeamOperand? classSave = body[first] is
        {
            Definition.Opcode: Move,
            Operands: [var value, { Kind: BeamOperandParameters.Y, TypeIndex: null } destination]
        } && value == exceptionClass && destination.Value < body[frame].Operands[0].Value ? destination : null;
        int copy = classSave is null ? first : first + 1;
        int continuation = copy + 3;
        if (continuation >= end
            || !ReadsLandingCopy(body[copy], reason, token)
            || !ReadsLandingCopy(body[copy + 1], trace, exceptionClass)
            || !ReadsLandingCopy(body[copy + 2], exceptionClass, token)
            || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(installation, (int)target.Value, out int label)
            || label + 1 != landing
            || !flow.IsEntryTemporaryDead(continuation, exceptionClass)
            || !flow.IsDead(
                continuation,
                token,
                allowBadRecordExit: true,
                afterCall: true,
                allowEntryFramePredicates: true
            ))
        {
            return null;
        }

        if (classSave is { } alias && alias != token
            && (!HasTerminalLandingConstruction(body, continuation, end)
                || !flow.IsInitialized(installation, alias, allowFramePredicates: true)
                || !flow.IsDead(
                    continuation,
                    alias,
                    allowBadRecordExit: true,
                    afterCall: true,
                    allowEntryFramePredicates: true
                )))
        {
            return null;
        }

        // try_case removes the sole handler and initializes its tag slot.
        // No remaining path reads the overwritten class register or a saved exception value.
        return (first, continuation);
    }

    private static bool HasTerminalLandingConstruction(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        for (int index = start; index < end; index++)
        {
            switch (body[index].Definition.Opcode)
            {
                case Line or Move or TestHeap or PutList or PutTuple:
                    continue;
                case TailLocalCallWithFrame or TailExternalCallWithFrame:
                    return true;
                case Deallocate:
                    return index + 1 < end && body[index + 1].Definition.Opcode == Return;
                default:
                    return false;
            }
        }

        return false;
    }

    private static bool ReadsLandingCopy(BeamInstruction instruction, BeamOperand from, BeamOperand to) =>
        instruction is { Definition.Opcode: Move, Operands: [var source, var destination] }
        && source == from && destination == to;
}
