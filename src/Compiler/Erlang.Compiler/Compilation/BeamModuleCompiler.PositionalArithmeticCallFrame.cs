using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PlacePositionalArithmeticBeforeCallFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var importsByIndex = imports.OrderBy(entry => emittedImports?[entry.Value] ?? entry.Value)
            .Select(entry => entry.Key).ToArray();

        return PlacePositionalArithmeticBeforeCallFrame(
            code,
            decoded.Body,
            decoded.StackOperands,
            instruction => instruction.Operands[2].Value < importsByIndex.Length && IsLeafArithmeticGcBif(instruction, names, importsByIndex)
        );
    }

    private static byte[]? PlacePositionalArithmeticBeforeCallFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        Func<BeamInstruction, bool> isArithmetic
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            for (int frame = entry + 1; frame + 13 < end; frame++)
            {
                if (body[frame - 1].Definition.Opcode != Label
                    || body[frame] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                    || width.Value != live.Value || width.Value < 3 || width.Value >= FirstReservedXRegister)
                {
                    continue;
                }

                int count = (int)width.Value;
                if (frame + 2 * count + 9 >= end || !HasUnprotectedCallKillSchedule(body, entry))
                {
                    continue;
                }

                var sourceRegisters = new Dictionary<int, int>();
                var incoming = new HashSet<int>();
                int firstLine = frame + count + 1;
                for (int position = frame + 1; position < firstLine; position++)
                {
                    if (body[position] is not
                        {
                            Definition.Opcode: Move,
                            Operands: [
                            { Kind: BeamOperandParameters.X, TypeIndex: null } x,
                            { Kind: BeamOperandParameters.Y, TypeIndex: null } slot
                            ]
                        } || x.Value >= count || slot.Value >= count
                        || !sourceRegisters.TryAdd((int)slot.Value, (int)x.Value) || !incoming.Add((int)x.Value))
                    {
                        break;
                    }
                }

                if (sourceRegisters.Count != count || incoming.Count != count)
                {
                    continue;
                }

                int first = firstLine;
                while (body[first].Definition.Opcode == Line)
                {
                    first++;
                }

                if (first + count + 9 >= end || ReadArithmetic(first) is not { } left
                    || ReadArithmetic(first + 2) is not { } right || left.Register >= right.Register
                    || body[first + 1] is not { Definition.Opcode: Move, Operands: [var leftResult, var leftSlot] }
                    || !IsBareX0(leftResult) || leftSlot != left.Source
                    || body[first + 3] is not { Definition.Opcode: Move, Operands: [var rightResult, var rightOutput] }
                    || !IsBareX0(rightResult) || rightOutput != new BeamOperand(BeamOperandParameters.X, right.Register))
                {
                    continue;
                }

                int restore = first + 4;
                int selector = restore + count - 1;
                var restored = new HashSet<int>();
                for (int position = restore; position < selector; position++)
                {
                    if (body[position] is not
                        {
                            Definition.Opcode: Move,
                            Operands: [
                            { Kind: BeamOperandParameters.Y, TypeIndex: null } source,
                            { Kind: BeamOperandParameters.X, TypeIndex: null } target
                            ]
                        } || !sourceRegisters.TryGetValue((int)source.Value, out int register)
                        || register != target.Value || register == right.Register || !restored.Add(register))
                    {
                        break;
                    }
                }

                if (restored.Count != count - 1
                    || body[selector] is not { Definition.Opcode: LocalCall, Operands: [var arguments, _] }
                    || arguments.Value != count
                    || body[selector + 1] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [var failure, var tagged, { Value: var arity }, _]
                    } || !IsBareX0(tagged) || arity != 2
                    || body[selector + 2] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var containing, { Value.IsOne: true }, { Kind: BeamOperandParameters.X, TypeIndex: null } nested]
                    } || !IsBareX0(containing)
                    || ReadReturnedField(
                        selector + 3,
                        nested,
                        0,
                        body
                    ) is not { } field0
                    || ReadReturnedField(
                        selector + 4,
                        nested,
                        1,
                        body
                    ) is not { } field1 || field0 == field1
                    || ReadDisjointFrameOwnership(body, entry, end) is not { } ownership
                    || ownership.Uses.Count != 1 || !ownership.Uses.ContainsKey(frame))
                {
                    continue;
                }

                int fallback = Enumerable.Range(selector + 5, end - selector - 5).FirstOrDefault(index =>
                    body[index] is { Definition.Opcode: Label, Operands: [var label] } && label.Value == failure.Value);
                if (fallback == 0 || fallback + 2 >= end || body[fallback + 1].Definition.Opcode != Deallocate
                    || body[fallback + 2].Definition.Opcode != Return
                    || Enumerable.Range(selector + 1, end - selector - 1).Any(index =>
                        ownership.Owners[index - entry] == frame
                        && (body[index].Definition.Opcode == InitYRegisters
                            || LinearFrameSizeOperand(body[index].Definition.Opcode) >= 0
                                && body[index].Definition.Opcode != Deallocate
                            || body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                                && operand.Value != field0 && operand.Value != field1))))
                {
                    continue;
                }

                // Neither original slot survives the selector: the checked
                // arm defines both, and the other arm immediately releases them.
                output.Write(code.Span[copied..body[frame].Offset]);
                output.Write(code.Span[body[firstLine].Offset..body[first].Offset]);
                WriteArithmetic(left);
                WriteArithmetic(right);
                output.WriteByte(Allocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, count);
                WriteFrameInitialization(output, [0, 1]);
                copied = body[selector].Offset;
                int operand = FirstStackOperandAtOrAfter(stackOperands, copied);
                for (int index = selector + 1; index < end; index++)
                {
                    if (ownership.Owners[index - entry] != frame)
                    {
                        continue;
                    }

                    BeamInstruction instruction = body[index];
                    if (instruction.Definition.Opcode == Deallocate)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        output.WriteByte(Deallocate);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                        copied = body[index + 1].Offset;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                    {
                        operand++;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < body[index + 1].Offset)
                    {
                        var (from, to, slot) = stackOperands[operand++];
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, YRegisterTag, slot == field0 ? 1 : 0);
                        copied = to;
                    }
                }

                break;

                (int Register, BeamOperand Source, BeamOperand Constant, BeamOperand Import)? ReadArithmetic(int index)
                {
                    BeamInstruction instruction = body[index];

                    return instruction is
                    {
                        Definition.Opcode: GcBifTwo,
                        Operands: [
                        { Kind: BeamOperandParameters.Failure }, { Value.IsZero: true }, var import,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } value,
                        { Kind: BeamOperandParameters.Integer } constant, var result
                            ]
                    } && IsBareX0(result) && isArithmetic(instruction)
                        && sourceRegisters.TryGetValue((int)value.Value, out int register)
                        ? (register, value, constant, import) : null;
                }

                void WriteArithmetic((int Register, BeamOperand Source, BeamOperand Constant, BeamOperand Import) item)
                {
                    output.WriteByte(GcBifTwo);
                    BeamCompactWriter.Write(output, LabelTag, 0);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, count);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, item.Import.Value);
                    BeamCompactWriter.Write(output, XRegisterTag, item.Register);
                    BeamCompactWriter.Write(output, BeamWire.IntegerTag, item.Constant.Value);
                    BeamCompactWriter.Write(output, XRegisterTag, item.Register);
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int? ReadReturnedField(
        int index,
        BeamOperand nested,
        int field,
        IReadOnlyList<BeamInstruction> body
    ) =>
        body[index] is
        {
            Definition.Opcode: GetTupleElement,
            Operands: [var source, var projected, { Kind: BeamOperandParameters.Y, TypeIndex: null } target]
        } && source == nested && projected.Value == field ? (int)target.Value : null;
}
