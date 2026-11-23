using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleRepeatedArithmeticEffectFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stack) = decoded;
        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var imported = imports.OrderBy(entry => emittedImports?[entry.Value] ?? entry.Value).Select(entry => entry.Key).ToArray();
        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        BeamOperand x2 = new(BeamOperandParameters.X, 2);
        BeamOperand y0 = new(BeamOperandParameters.Y, 0);
        BeamOperand y1 = new(BeamOperandParameters.Y, 1);
        using var output = new MemoryStream(code.Length);
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
            if (!HasUnprotectedCallKillSchedule(body, entry))
            {
                continue;
            }

            for (int frame = entry + 1; frame + 6 < end; frame++)
            {
                if (body[frame] is not { Definition.Opcode: Allocate, Operands: [{ Value: var width }, var live] }
                    || width != 2 || !IsMove(body[frame + 1], x0, y0) || !IsMove(body[frame + 2], x1, y1))
                {
                    continue;
                }

                bool late = live.Value == 2 && body[frame - 1].Definition.Opcode == Label
                    && body[frame + 3].Definition.Opcode == Line;
                int first = late ? frame + 4 : frame - 1;
                if ((!late && (live.Value != 3 || frame < entry + 3 || body[frame - 2].Definition.Opcode != Line
                        || body[frame - 3].Definition.Opcode != Label))
                    || body[first] is not
                    {
                        Definition.Opcode: GcBifTwo,
                        Operands: [
                        { Value.IsZero: true }, var rooted, var arithmetic, var number,
                        { Kind: BeamOperandParameters.Integer } increment, var result
                        ]
                    }
                    || arithmetic.Value >= imported.Length || !IsLeafArithmeticGcBif(body[first], names, imported)
                    || rooted.Value != (late ? 0 : 2) || number != (late ? y1 : x1) || result != (late ? x1 : x2))
                {
                    continue;
                }

                int constant = late ? first + 1 : frame + 4;
                int firstCall = late ? constant + 2 : constant + 1;
                int second = firstCall + 2;
                int secondCall = second + 3;
                int release = secondCall + 2;
                if (release + 1 >= end || !IsLiteralArgument(body[constant])
                    || (late ? !IsMove(body[constant + 1], y0, x0) : !IsMove(body[frame + 3], x2, x1))
                    || !IsThreeArgumentEffect(body[firstCall])
                    || body[firstCall + 1].Definition.Opcode != Line
                    || body[second] is not
                    {
                        Definition.Opcode: GcBifTwo,
                        Operands: [{ Value.IsZero: true }, { Value.IsZero: true }, var again, var retained, var amount, var target]
                    }
                    || again.Value != arithmetic.Value || amount != increment || target != x1
                    || !SameProjectionRegister(retained, y1)
                    || !IsLiteralArgument(body[second + 1]) || !IsMove(body[second + 2], y0, x0)
                    || !IsThreeArgumentEffect(body[secondCall])
                    || body[secondCall + 1] is not
                    { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Atom }, var returned] }
                    || returned != x0 || body[release] is not { Definition.Opcode: Deallocate, Operands: [{ Value: var released }] }
                    || released != 2 || body[release + 1].Definition.Opcode != Return)
                {
                    continue;
                }

                // The first BIF only consumes the incoming numeric argument.
                // Keep both original arguments rooted until the frame can own
                // them, and leave its fresh X2 result available for the first call.
                output.Write(code.Span[copied..body[frame].Offset]);
                if (late)
                {
                    output.Write(code.Span[body[frame + 3].Offset..body[first].Offset]);
                    output.WriteByte(GcBifTwo);
                    BeamCompactWriter.Write(output, LabelTag, 0);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arithmetic.Value);
                    WriteRegister(output, x1);
                    BeamCompactWriter.Write(output, BeamWire.IntegerTag, increment.Value);
                    WriteRegister(output, x2);
                }

                output.WriteByte(Allocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 3);
                WriteMove(x1, y0);
                WriteMove(x0, y1);
                WriteMove(x2, x1);
                WriteInstruction(constant);
                output.Write(code.Span[body[firstCall].Offset..body[second].Offset]);

                // The subsequent BIF and call are the final readers of the
                // two saved inputs. Preserve the encoded numeric operand type
                // while assigning the physical source and outgoing slots.
                int stackIndex = FirstStackOperandAtOrAfter(stack, body[second].Offset);
                var (operand, suffix, _) = stack[stackIndex];
                output.Write(code.Span[body[second].Offset..operand]);
                WriteRegister(output, y0);
                output.Write(code.Span[suffix..body[second + 2].Offset]);
                WriteMove(y1, x0);
                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                output.Write(code.Span[body[secondCall].Offset..body[release].Offset]);
                output.WriteByte(Deallocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                copied = body[release + 1].Offset;
                frame = release;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsLiteralArgument(BeamInstruction instruction) => instruction is
        { Definition.Opcode: Move, Operands: [var literal, var destination] }
            && destination == x2 && literal.TypeIndex is null && IsNonRegisterValue(literal);

        bool IsThreeArgumentEffect(BeamInstruction call)
        {
            if (call is not { Definition.Opcode: ExternalCall, Operands: [{ Value: var arity }, var import] }
                || arity != 3 || import.Value >= imported.Length)
            {
                return false;
            }

            var (module, function, count) = imported[(int)import.Value];

            return !BeamTrimBifs.IsPure(names[module - 1], names[function - 1], count);
        }

        void WriteInstruction(int index) =>
            output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void WriteMove(BeamOperand source, BeamOperand destination)
        {
            output.WriteByte(Move);
            WriteRegister(output, source);
            WriteRegister(output, destination);
        }
    }
}
