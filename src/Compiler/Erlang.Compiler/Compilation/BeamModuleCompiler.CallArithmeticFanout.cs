using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepCallResultForArithmeticFanout(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled)
        {
            return null;
        }

        using var output = new MemoryStream();
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var firstArithmetic = new BeamOperand(BeamOperandParameters.X, 1);
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
            int stop = end;
            // The following function's line and pre-entry label precede its FuncInfo.
            while (stop > start && body[stop - 1].Definition.Opcode is Label or Line)
            {
                stop--;
            }

            if (stop <= start || body[stop - 1].Definition.Opcode != Return)
            {
                continue;
            }

            for (int call = start; call + 10 < stop; call++)
            {
                if (body[call].Definition.Opcode is not (ExternalCall or LocalCall)
                    || body[call + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedInput]
                    }
                    || source != result)
                {
                    continue;
                }

                int first = SkipLines(call + 2);
                if (first + 2 >= stop || !UsesSavedInteger(body[first], savedInput)
                    || body[first + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var firstValue, { Kind: BeamOperandParameters.Y, TypeIndex: null } firstSlot]
                    }
                    || firstValue != result || firstSlot == savedInput)
                {
                    continue;
                }

                int second = SkipLines(first + 2);
                if (second + 4 >= stop || !UsesSavedInteger(body[second], savedInput)
                    || !Moves(second + 1, result, savedInput)
                    || !Moves(second + 2, firstSlot, result))
                {
                    continue;
                }

                int convert = SkipLines(second + 3);
                if (convert + 3 >= stop || !IsUnaryCall(convert) || !Moves(convert + 1, result, firstSlot)
                    || !Moves(convert + 2, savedInput, result)
                    || !IsUnaryCall(SkipLines(convert + 3))
                    || !IsUnprotectedTail(
                        start,
                        call,
                        convert + 3,
                        stop
                    ))
                {
                    continue;
                }

                // The original X0 survives two independent operations. The first
                // integer BIF proves the type on its normal edge. During the second
                // BIF both values are live, so it consumes X0 and defines Y directly.
                output.Write(code.Span[copied..body[call + 1].Offset]);
                Copy(call + 2, first);
                WriteCallArithmeticStep(
                    output,
                    code[body[first].Offset..body[first + 1].Offset],
                    body[first],
                    savedInput,
                    first: true,
                    destination: firstArithmetic
                );
                Copy(first + 2, second);
                int integerType = GetOperandTypeIndex((IntegerTypeBits, null, null, null));
                var secondOperands = body[second].Operands;
                output.WriteByte(GcBifTwo);
                BeamCompactWriter.Write(output, LabelTag, 0);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, secondOperands[2].Value);
                foreach (var operand in secondOperands.Skip(3).Take(2))
                {
                    if (SameProjectionRegister(operand, savedInput))
                    {
                        output.WriteByte(ExtendedTypedRegisterOperand);
                        WriteRegister(output, result);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, integerType);
                    }
                    else
                    {
                        BeamCompactWriter.Write(output, BeamWire.IntegerTag, operand.Value);
                    }
                }

                WriteRegister(output, savedInput);
                output.WriteByte(Move);
                WriteRegister(output, firstArithmetic);
                WriteRegister(output, result);
                Copy(second + 3, convert + 1);
                output.WriteByte(Swap);
                WriteRegister(output, savedInput);
                WriteRegister(output, result);
                highestOpcode = Math.Max(highestOpcode, Swap);

                // The exchange saves the first converted value in the former
                // input slot. The remaining straight-line frame uses the two
                // stack slots under the opposite names.
                copied = body[convert + 3].Offset;
                int tailEnd = body[stop].Offset;
                foreach (var operand in stackOperands)
                {
                    if (operand.Start < copied || operand.Start >= tailEnd)
                    {
                        continue;
                    }

                    int slot = operand.Slot;
                    if (slot != (int)firstSlot.Value && slot != (int)savedInput.Value)
                    {
                        continue;
                    }

                    output.Write(code.Span[copied..operand.Start]);
                    BeamCompactWriter.Write(
                        output,
                        YRegisterTag,
                        slot == (int)firstSlot.Value ? savedInput.Value : firstSlot.Value
                    );
                    copied = operand.End;
                }

                output.Write(code.Span[copied..tailEnd]);
                copied = tailEnd;
                break;
            }

            int SkipLines(int index)
            {
                while (index < stop && body[index].Definition.Opcode == Line)
                {
                    index++;
                }

                return index;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Copy(int first, int last) => output.Write(code.Span[body[first].Offset..body[last].Offset]);

        bool IsUnaryCall(int index) => body[index].Definition.Opcode is (LocalCall or ExternalCall)
            && body[index].Operands[0].Value.IsOne;

        bool Moves(int index, BeamOperand from, BeamOperand to) =>
            body[index] is { Definition.Opcode: Move, Operands: [var source, var target] }
            && source == from && target == to;

        bool UsesSavedInteger(BeamInstruction instruction, BeamOperand saved)
        {
            if (!IsLeafArithmeticGcBif(instruction, atomNames, importsByIndex)
                || !instruction.Operands[0].Value.IsZero || !instruction.Operands[1].Value.IsZero
                || instruction.Operands[^1] != result)
            {
                return false;
            }

            var import = importsByIndex[(int)instruction.Operands[2].Value];

            return atomNames[import.Function - 1] is ErlangOperators.BitwiseAnd or ErlangOperators.BitwiseOr
                    or ErlangOperators.BitwiseXor or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight
                    or ErlangOperators.IntegerDivide or ErlangOperators.Remainder
                && (SameProjectionRegister(instruction.Operands[3], saved) && instruction.Operands[4].Kind == BeamOperandParameters.Integer
                    || SameProjectionRegister(instruction.Operands[4], saved) && instruction.Operands[3].Kind == BeamOperandParameters.Integer);
        }

        bool IsUnprotectedTail(
            int start,
            int call,
            int tail,
            int end
        )
        {
            // All remaining operations observe the same frame, with no incoming
            // branches, handler scopes, or instructions changing the slot base.
            for (int index = tail; index < end; index++)
            {
                if (body[index].Definition.Opcode is not (Line or LocalCall or ExternalCall
                    or Move or BifZero or TestHeap or PutList or PutTuple or Deallocate or Return))
                {
                    return false;
                }
            }

            return ReadUnprotectedInstructionIndices(
                body,
                start,
                end,
                labelsOnly: false
            )?.Contains(call) != false;
        }
    }
}
