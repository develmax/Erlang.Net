using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? StoreBifResultsDirectly(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction bif = body[index];
            if (bif.Definition.Opcode is not (BifZero or BifOne or BifTwo or BifThree
                or GcBifOne or GcBifTwo or GcBifThree)
                || bif.Operands[^1] is not { Kind: BeamOperandParameters.X, TypeIndex: null } result)
            {
                continue;
            }

            int next = index + 1;
            int copy = next;
            while (body[copy].Definition.Opcode == Line)
            {
                copy++;
            }

            if (body[copy] is not { Definition.Opcode: Move, Operands: [var source, var slot] }
                || source != result || slot.TypeIndex is not null || slot == result)
            {
                continue;
            }

            int call = copy + 1;
            while (body[call].Definition.Opcode == Line)
            {
                call++;
            }

            bool directArgument = slot.Kind == BeamOperandParameters.X
                && ReplacesBifResultBeforeCall(
                    body,
                    call,
                    result,
                    slot
                );
            bool stackResult = slot.Kind == BeamOperandParameters.Y;
            bool deadOnCall = stackResult && body[call].Definition.Opcode is LocalCall or ExternalCall
                && result.Value >= body[call].Operands[0].Value;
            bool deadOnBranch = stackResult && !deadOnCall && IsEntryFramePredicate(body[call].Definition.Opcode)
                && CrossesTestedResultCall(
                    body,
                    call,
                    result,
                    slot
                )
                && (flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(call, result);
            if (!directArgument && !deadOnCall && !deadOnBranch)
            {
                continue;
            }

            // A failing BIF skips the destination write as it skipped the move.
            // On success, an argument overwrite, call, or both guard exits prove the former X value dead.
            int registerWidth = result.Value < 1 << BeamWire.CompactSmallPayloadShift ? 1 : 2;
            int bifEnd = body[next].Offset;
            output.Write(code.Span[copied..(bifEnd - registerWidth)]);
            WriteRegister(output, slot);
            output.Write(code.Span[bifEnd..body[copy].Offset]);
            copied = body[copy + 1].Offset;
            index = copy;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ReplacesBifResultBeforeCall(
        IReadOnlyList<BeamInstruction> body,
        int overwrite,
        BeamOperand result,
        BeamOperand argument
    )
    {
        if (body[overwrite] is not { Definition.Opcode: Move, Operands: [var replacement, var destination] }
            || destination != result || SameProjectionRegister(replacement, result))
        {
            return false;
        }

        int call = overwrite + 1;
        while (body[call].Definition.Opcode == Line || body[call] is
            {
                Definition.Opcode: Move,
                Operands: [var constant, { Kind: BeamOperandParameters.X, TypeIndex: null } otherArgument]
            } && IsNonRegisterValue(constant) && otherArgument != result && otherArgument != argument)
        {
            call++;
        }

        BeamInstruction consumer = body[call];

        return consumer.Definition.Opcode is LocalCall or ExternalCall or TailLocalCall or TailExternalCall
            or TailLocalCallWithFrame or TailExternalCallWithFrame
            && result.Value < consumer.Operands[0].Value && argument.Value < consumer.Operands[0].Value;
    }

    private static bool CrossesTestedResultCall(
        IReadOnlyList<BeamInstruction> body,
        int test,
        BeamOperand result,
        BeamOperand slot
    )
    {
        if (!body[test].Operands.Any(value => SameProjectionRegister(value, slot))
            || body[test + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y } input, var argument]
            }
            || SameProjectionRegister(input, slot) || !SameProjectionRegister(argument, result))
        {
            return false;
        }

        int call = test + 2;
        while (body[call].Definition.Opcode == Line)
        {
            call++;
        }

        if (body[call].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[call].Operands[0].Value != 1 || !result.Value.IsZero)
        {
            return false;
        }

        int use = call + 1;
        while (body[use].Definition.Opcode == Line)
        {
            use++;
        }

        BeamInstruction consumer = body[use];

        return consumer.Definition.Opcode is BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree
            && consumer.Operands.Take(consumer.Operands.Count - 1).Any(value => SameProjectionRegister(value, slot));
    }
}
