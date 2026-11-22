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

    private static byte[]? StoreArithmeticResultAcrossZeroFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Func<BeamInstruction, bool> isArithmetic
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 4 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [
                    { Kind: BeamOperandParameters.Failure, Value.IsZero: true },
                    { Value.Sign: > 0 } roots,
                        _, _, _,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } scratch
                    ]
                } || scratch.Value != roots.Value
                || body[index + 1] is not { Definition.Opcode: Allocate, Operands: [{ Value.IsZero: true }, var live] }
                || live.Value != scratch.Value + 1
                || body[index + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var value, { Kind: BeamOperandParameters.X, TypeIndex: null } target]
                } || value != scratch || target.Value >= scratch.Value
                || body[index + 3] is not { Definition.Opcode: Move, Operands: [var replacement, _] }
                || !IsNonRegisterValue(replacement)
                || !ReplacesBifResultBeforeCall(
                    body,
                    index + 3,
                    scratch,
                    target,
                    ordinaryOnly: true
                )
                || !isArithmetic(body[index]))
            {
                continue;
            }

            // The scratch result is the only new allocation root. The copy
            // killed the old argument, and a literal replaces the scratch
            // before the call can read it.
            int destination = body[index + 1].Offset - CompactRegisterCountWidth((int)scratch.Value);
            output.Write(code.Span[copied..destination]);
            WriteRegister(output, target);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, roots.Value);
            copied = body[index + 3].Offset;
            index += 2;
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
        BeamOperand argument,
        bool ordinaryOnly = false
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

        byte opcode = consumer.Definition.Opcode;

        return (opcode is LocalCall or ExternalCall || !ordinaryOnly && opcode is (TailLocalCall or TailExternalCall
            or TailLocalCallWithFrame or TailExternalCallWithFrame))
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
