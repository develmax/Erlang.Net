using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepTerminalRecordOperandsInX(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(UpdateRecord) || !code.Span.Contains(GcBifTwo)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int stack = 0;
        bool protectedFunction = false;
        for (int save = 0; save + 6 < body.Count; save++)
        {
            var instruction = body[save];
            if (instruction.Definition.Opcode == FunctionInfo)
            {
                protectedFunction = false;
            }

            protectedFunction |= instruction.Definition.Opcode is CatchEnter or TryEnter;
            if (protectedFunction || !TryReadRecordOperandSave(instruction, out BeamOperand previous))
            {
                continue;
            }

            int arithmetic = save + 1;
            while (body[arithmetic].Definition.Opcode == Line)
            {
                arithmetic++;
            }

            if (arithmetic + 4 >= body.Count || body[arithmetic] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [{ Value.IsZero: true }, { Value.IsZero: true }, _, var left, var right, var result]
                } || !IsBareX0(result) || !Independent(left) || !Independent(right)
                || !TryReadRecordOperandSave(body[arithmetic + 1], out BeamOperand computed) || computed == previous
                || body[arithmetic + 2] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                || body[arithmetic + 3].Definition.Opcode != UpdateRecord)
            {
                continue;
            }

            int update = arithmetic + 3;
            var operands = body[update].Operands;
            if (!IsBareX0(operands[3]) || !IsSingleReplacement(previous) || !IsSingleReplacement(computed)
                || operands.Where((_, index) => index != 3).Any(operand => operand.Kind == BeamOperandParameters.X)
                || !HasClosedRecordReturn(
                    body,
                    update + 1,
                    previous,
                    computed
                )
                || !(flow ??= new BeamProjectionFlow(body)).IsInitialized(save, previous, allowFramePredicates: true))
            {
                continue;
            }

            // Both values are consumed before the terminal frame closes. Retain
            // X0 across the non-tail GC BIF and put its result in X1.
            output.Write(code.Span[copied..instruction.Offset]);
            int bifOperands = body[arithmetic].Offset + 1;
            var cursor = new BeamCursor(code[bifOperands..]);
            _ = BeamCompactReader.Read(cursor);
            int bifLive = bifOperands + cursor.Position;
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[body[save + 1].Offset..bifLive]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.Write(code.Span[(bifOperands + cursor.Position)..(body[arithmetic + 1].Offset - 1)]);
            BeamCompactWriter.Write(output, XRegisterTag, 1);

            int heapLive = body[update].Offset - 1;
            output.Write(code.Span[body[arithmetic + 2].Offset..heapLive]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            copied = heapLive + 1;
            int updateEnd = body[update + 1].Offset;
            while (stack < stackOperands.Count && stackOperands[stack].Start < body[update].Offset)
            {
                stack++;
            }

            while (stack < stackOperands.Count && stackOperands[stack].Start < updateEnd)
            {
                var (from, to, slot) = stackOperands[stack++];
                int target = slot == previous.Value ? 0 : slot == computed.Value ? 1 : -1;
                if (target < 0)
                {
                    continue;
                }

                output.Write(code.Span[copied..from]);
                BeamCompactWriter.Write(output, XRegisterTag, target);
                copied = to;
            }

            save = update;

            bool Independent(BeamOperand operand) =>
                operand.Kind is BeamOperandParameters.Y or BeamOperandParameters.Integer
                && !SameProjectionRegister(operand, previous);

            bool IsSingleReplacement(BeamOperand slot) =>
                Enumerable.Range(0, operands.Count).Where(index => SameProjectionRegister(operands[index], slot)).ToArray()
                    is [int use] && use >= FirstRecordUpdateValueOperand
                    && (use - FirstRecordUpdateValueOperand) % RecordUpdateFieldPairWidth == 0;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool TryReadRecordOperandSave(BeamInstruction instruction, out BeamOperand saved)
    {
        if (instruction is { Definition.Opcode: Move, Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot] }
            && IsBareX0(source))
        {
            saved = slot;

            return true;
        }

        saved = default!;

        return false;
    }

    private static bool HasClosedRecordReturn(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand previous,
        BeamOperand computed
    )
    {
        for (int index = start; index < body.Count - 1; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode == Deallocate)
            {
                return body[index + 1].Definition.Opcode == Return;
            }

            if (instruction.Definition.Opcode is not (UpdateRecord or PutTuple or Line)
                || instruction.Operands.Any(operand => SameProjectionRegister(operand, previous)
                    || SameProjectionRegister(operand, computed)
                    || operand is { Kind: BeamOperandParameters.X, Value.IsOne: true }))
            {
                return false;
            }
        }

        return false;
    }
}
