using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? PlaceEntryBifBeforeAllocation(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands,
        Func<BeamInstruction, bool> isArithmetic
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int operand = 0;
        for (int entry = 1; entry + 3 < body.Count; entry++)
        {
            bool functionEntry = body[entry - 1].Definition.Opcode == FunctionInfo;
            if (body[entry].Definition.Opcode != Label
                || !functionEntry && body[entry + 1].Definition.Opcode != Allocate)
            {
                continue;
            }

            int frame = entry + 1;
            while (body[frame].Definition.Opcode == Line || IsEntryFramePredicate(body[frame].Definition.Opcode))
            {
                frame++;
            }

            if (body[frame] is not { Definition.Opcode: Allocate, Operands: [var size, var live] })
            {
                continue;
            }

            int setup = frame + 1;
            int line = setup;
            var saved = new Dictionary<int, int>();
            for (; line < body.Count; line++)
            {
                BeamInstruction instruction = body[line];
                if (instruction is { Definition.Opcode: Move, Operands: [var source, var slot] }
                    && source is { Kind: BeamOperandParameters.X, TypeIndex: null } && source.Value < live.Value
                    && slot is { Kind: BeamOperandParameters.Y, TypeIndex: null })
                {
                    saved[(int)slot.Value] = (int)source.Value;
                }
                else if (instruction.Definition.Opcode == InitYRegisters)
                {
                    foreach (BeamOperand cleared in instruction.Operands.Skip(1))
                    {
                        saved.Remove((int)cleared.Value);
                    }
                }
                else
                {
                    break;
                }
            }

            int bifIndex = line;
            while (body[bifIndex].Definition.Opcode == Line)
            {
                bifIndex++;
            }

            BeamInstruction bif = body[bifIndex];
            byte opcode = bif.Definition.Opcode;
            bool collects = opcode is GcBifOne or GcBifTwo or GcBifThree;
            if (!(collects || opcode is BifZero or BifOne or BifTwo or BifThree)
                || opcode != BifZero && bif.Operands[0].Kind != BeamOperandParameters.Failure
                || bif.Operands[^1] is not { Kind: BeamOperandParameters.X, TypeIndex: null } result
                || result.Value > live.Value
                || collects && bif.Operands[1].Value > live.Value
                || bif.Operands.Take(bif.Operands.Count - 1).Any(input =>
                    input.Kind == BeamOperandParameters.Y && !saved.ContainsKey((int)input.Value)
                    || input.Kind == BeamOperandParameters.X && input.Value >= live.Value))
            {
                continue;
            }

            var spill = body[bifIndex + 1];
            var storedResult = spill is
            { Definition.Opcode: Move, Operands: [var value, { Kind: BeamOperandParameters.Y, TypeIndex: null } stored] }
                && value == result ? stored : null;
            int following = bifIndex + (storedResult is null ? 1 : 2);
            while (body[following].Definition.Opcode == Line)
            {
                following++;
            }

            BeamInstruction consumer = body[following];
            if (!functionEntry && (!isArithmetic(bif) || storedResult is not null || !result.Value.IsZero
                    || consumer is not { Definition.Opcode: LocalCall, Operands: [var arity, _] }
                    || arity.Value != 1
                    || !(flow ??= new BeamProjectionFlow(body)).IsSingleForwardSelectionTarget(entry)))
            {
                continue;
            }

            bool testedResult = (IsEntryFramePredicate(consumer.Definition.Opcode)
                || consumer.Definition.Opcode is SelectValue or SelectTupleArity)
                && consumer.Operands.Any(input => SameProjectionRegister(input, result)
                    || storedResult is not null && SameProjectionRegister(input, storedResult));
            if (!testedResult && !bif.Operands.Any(input => input.Kind == BeamOperandParameters.Y))
            {
                continue;
            }

            BeamOperand? spilledSlot = storedResult is not null
                && (flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(bifIndex + 1, result)
                ? storedResult : null;
            int register = (int)result.Value;
            bool preserveInput = body.Skip(setup).Take(line - setup).Any(instruction =>
                instruction.Definition.Opcode == Move && instruction.Operands[1] != spilledSlot
                && SameProjectionRegister(instruction.Operands[0], result));
            int destination = preserveInput ? (int)live.Value : register;
            if (destination >= BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int frameStart = body[frame].Offset;
            int bifEnd = body[bifIndex + 1].Offset;
            int resultOffset = bifEnd - (register < 1 << BeamWire.CompactSmallPayloadShift ? 1 : 2);
            output.Write(code.Span[copied..frameStart]);
            output.Write(code.Span[body[line].Offset..bif.Offset]);
            int from = bif.Offset;
            if (collects)
            {
                var cursor = new BeamCursor(code[(bif.Offset + 1)..]);
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[from..(bif.Offset + 1 + cursor.Position)]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, (int)live.Value);
                _ = BeamCompactReader.Read(cursor);
                from = bif.Offset + 1 + cursor.Position;
            }

            while (operand < stackOperands.Count && stackOperands[operand].Start < from)
            {
                operand++;
            }

            while (operand < stackOperands.Count && stackOperands[operand].Start < resultOffset)
            {
                var (start, end, slot) = stackOperands[operand++];
                output.Write(code.Span[from..start]);
                BeamCompactWriter.Write(output, XRegisterTag, saved[slot]);
                from = end;
            }

            output.Write(code.Span[from..resultOffset]);
            BeamCompactWriter.Write(output, XRegisterTag, destination);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, (int)size.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, Math.Max((int)live.Value, destination + 1));
            for (int index = setup; index < line; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode != Move || instruction.Operands[1] != spilledSlot)
                {
                    output.Write(code.Span[instruction.Offset..body[index + 1].Offset]);
                }
            }

            copied = bifEnd;
            if (spilledSlot is not null)
            {
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, XRegisterTag, destination);
                WriteRegister(output, spilledSlot);
                copied = body[bifIndex + 2].Offset;
            }
            else if (destination != register)
            {
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, XRegisterTag, destination);
                BeamCompactWriter.Write(output, XRegisterTag, register);
            }

            entry = bifIndex;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
