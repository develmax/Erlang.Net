using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const string CopyFreshRecordUpdateHint = "copy";

    private byte[]? ForwardTerminalConsRecordUpdates(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(PutList) || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        int reuse = atoms.GetValueOrDefault(ReuseRecordUpdateHint, -1);

        return RewriteTerminalConsRecordUpdates(
            code,
            decoded.Body,
            decoded.StackOperands,
            reuse,
            () => Intern(CopyFreshRecordUpdateHint),
            () => GetOperandTypeIndex((ConsTypeBits, null, null, null))
        );
    }

    private static byte[]? RewriteTerminalConsRecordUpdates(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack,
        int reuse,
        Func<int> copyHint,
        Func<int> consType
    )
    {
        using var output = new MemoryStream();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        bool unprotected = false;
        int copied = 0;
        for (int list = 0; list + 5 < body.Count; list++)
        {
            if (body[list].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, list + 1);
                continue;
            }

            if (!unprotected || body[list] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [var head, var tail, var construction]
                } || construction != x0
                || body[list + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || result != x0
                || body[list + 2] is not
                {
                    Definition.Opcode: UpdateRecord,
                    Operands: [{ Kind: BeamOperandParameters.Atom } hint, _, var record, var destination, _, ..]
                } update || destination != x0 || SameProjectionRegister(record, saved)
                || body[list + 3].Definition.Opcode != Deallocate || body[list + 4].Definition.Opcode != Return
                || update.Operands.Count(item => SameProjectionRegister(item, saved)) != 1
                || update.Operands.Skip(5).FirstOrDefault(item => SameProjectionRegister(item, saved)) is not { } updateValue)
            {
                continue;
            }

            int first = body[list + 2].Offset;
            int end = body[list + 3].Offset;
            int operand = FirstStackOperandAtOrAfter(stack, first);
            while (operand < stack.Count && stack[operand].Start < end && stack[operand].Slot != saved.Value)
            {
                operand++;
            }

            if (operand >= stack.Count || stack[operand].Start >= end)
            {
                continue;
            }

            if (list >= 3 && head == saved && tail == x0 && body[list - 2].Offset >= copied
                && body[list - 3] is { Definition.Opcode: PutTuple, Operands: [var tuple, ..] } && tuple == x0
                && body[list - 2] is { Definition.Opcode: Move, Operands: [var accepted, var pending] }
                && accepted == x0 && pending == saved
                && body[list - 1] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y } carrier, _, var projected]
                } && !SameProjectionRegister(carrier, saved) && projected == x0
                && ChooseTerminalConsTailRegister(update) is int scratch)
            {
                // The tuple and its list tail need distinct registers. The
                // tail projection reads an independent saved carrier.
                output.Write(code.Span[copied..body[list - 2].Offset]);
                output.Write(code.Span[body[list - 1].Offset..(body[list].Offset - 1)]);
                BeamCompactWriter.Write(output, XRegisterTag, scratch);
                output.WriteByte(PutList);
                WriteRegister(output, x0);
                BeamCompactWriter.Write(output, XRegisterTag, scratch);
                WriteRegister(output, x0);
            }
            else
            {
                output.Write(code.Span[copied..body[list + 1].Offset]);
            }

            var (at, after, _) = stack[operand];
            if (hint.Value == reuse)
            {
                // OTP's no_reuse SSA pass marks a newly constructed cons as
                // unlikely to reuse the old field value.
                var cursor = new BeamCursor(code[(first + 1)..]);
                _ = BeamCompactReader.Read(cursor);
                output.WriteByte(UpdateRecord);
                BeamCompactWriter.Write(output, AtomTag, copyHint());
                first += 1 + cursor.Position;
            }

            output.Write(code.Span[first..at]);
            if (updateValue.TypeIndex is null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            WriteRegister(output, x0);
            if (updateValue.TypeIndex is null)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, consType());
            }

            copied = after;
            list += 2;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int? ChooseTerminalConsTailRegister(BeamInstruction update)
    {
        var used = update.Operands.Where(item => item.Kind == BeamOperandParameters.X)
            .Select(item => (int)item.Value).ToHashSet();
        int scratch = Enumerable.Range(1, Math.Min(used.Count + 1, FirstReservedXRegister - 1))
            .FirstOrDefault(candidate => !used.Contains(candidate));

        return scratch > 0 ? scratch : null;
    }
}
