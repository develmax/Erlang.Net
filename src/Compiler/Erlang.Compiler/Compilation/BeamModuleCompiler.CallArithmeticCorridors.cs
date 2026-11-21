using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? DelayCallResultStoreThroughArithmetic(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        using var output = new MemoryStream();
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var arithmetic = new BeamOperand(BeamOperandParameters.X, 1);
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
            HashSet<int>? unprotected = null;
            bool readScopes = false;
            for (int call = start; call + 3 < end; call++)
            {
                if (body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[call + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || source != result)
                {
                    continue;
                }

                int first = SkipLines(call + 2);
                int next = first;
                int last = -1;
                while (next < end && IsArithmetic(body[next], saved, first: last < 0))
                {
                    last = next;
                    next = SkipLines(next + 1);
                }

                if (last < 0 || next >= end)
                {
                    continue;
                }

                bool store = body[next] is
                {
                    Definition.Opcode: Move,
                    Operands: [var operand, { Kind: BeamOperandParameters.Y, TypeIndex: null } target]
                } && operand == result && target != saved;
                int setup = store ? SkipLines(next + 1) : next;
                bool replacesResult = store && setup < end && body[setup] is
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.Y or BeamOperandParameters.Integer
                            or BeamOperandParameters.Atom or BeamOperandParameters.LiteralValue
                            or BeamOperandParameters.Nil
                    },
                        var argument
                    ]
                } && argument == result;
                int consumer = replacesResult ? SkipLines(setup + 1) : setup;
                if (consumer >= end || body[consumer].Definition.Opcode is not (LocalCall or ExternalCall)
                    || !body[consumer].Operands[0].Value.IsOne
                    || store && !replacesResult)
                {
                    continue;
                }

                if (!readScopes)
                {
                    unprotected = ReadUnprotectedInstructionIndices(
                        body,
                        start,
                        end,
                        labelsOnly: false
                    );
                    readScopes = true;
                }

                if (unprotected?.Contains(call + 1) == false)
                {
                    continue;
                }
                // The ordinary call has verified the Y frame and defines only X0.
                // Keep that value rooted while X1 carries the arithmetic result.
                int save = body[call + 1].Offset;
                output.Write(code.Span[copied..save]);
                for (int index = call + 2; index <= last; index++)
                {
                    if (body[index].Definition.Opcode == Line)
                    {
                        output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                        continue;
                    }

                    WriteCallArithmeticStep(
                        output,
                        code[body[index].Offset..body[index + 1].Offset],
                        body[index],
                        saved,
                        first: index == first,
                        destination: store && index == last ? body[next].Operands[1] : null
                    );
                }

                copied = body[last + 1].Offset;
                if (store)
                {
                    output.Write(code.Span[copied..body[next].Offset]);
                    copied = body[next + 1].Offset;
                }

                output.Write(code.Span[save..body[call + 2].Offset]);
                if (!store)
                {
                    output.WriteByte(Move);
                    WriteRegister(output, arithmetic);
                    WriteRegister(output, result);
                }

                call = last;
            }

            int SkipLines(int index)
            {
                while (index < end && body[index].Definition.Opcode == Line)
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

        bool IsArithmetic(BeamInstruction instruction, BeamOperand saved, bool first)
        {
            if (!IsLeafArithmeticGcBif(instruction, atomNames, importsByIndex)
                || !instruction.Operands[0].Value.IsZero
                || instruction.Operands[1].Value > 1
                || instruction.Operands[^1] != result)
            {
                return false;
            }

            var left = instruction.Operands[3];
            var right = instruction.Operands[4];

            return first
                ? instruction.Operands[1].Value.IsZero
                    && (SameProjectionRegister(left, saved) || SameProjectionRegister(right, saved))
                    && left.Kind != BeamOperandParameters.X && right.Kind != BeamOperandParameters.X
                : (left.Kind != BeamOperandParameters.X || left.Value.IsZero)
                    && (right.Kind != BeamOperandParameters.X || right.Value.IsZero)
                    && (!instruction.Operands[1].Value.IsZero
                        || left.Kind != BeamOperandParameters.X && right.Kind != BeamOperandParameters.X);
        }
    }

    private static void WriteCallArithmeticStep(
        Stream output,
        ReadOnlyMemory<byte> encoded,
        BeamInstruction instruction,
        BeamOperand saved,
        bool first,
        BeamOperand? destination
    )
    {
        output.WriteByte(GcBifTwo);
        var cursor = new BeamCursor(encoded[1..]);
        var operands = instruction.Operands;
        for (int index = 0; index < operands.Count; index++)
        {
            BeamOperand operand = operands[index];
            int from = cursor.Position + 1;
            _ = BeamCompactReader.Read(cursor);
            if (operand.TypeIndex is not null)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            if (operand.TypeIndex is not null || operand.Kind == BeamOperandParameters.LiteralValue)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            if (index == 1)
            {
                // Preserve X0 at every collection. A later step can also need
                // the previous arithmetic result, now in X1.
                int roots = first || operand.Value.IsZero ? 1 : 2;
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, roots);
            }
            else if (destination is not null && index == operands.Count - 1)
            {
                WriteRegister(output, destination);
            }
            else if (SameProjectionRegister(operand, saved))
            {
                WriteX(0, operand.TypeIndex);
            }
            else if (operand.Kind == BeamOperandParameters.X)
            {
                WriteX(1, operand.TypeIndex);
            }
            else
            {
                output.Write(encoded.Span[from..(cursor.Position + 1)]);
            }
        }

        void WriteX(int register, int? type)
        {
            if (type is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            BeamCompactWriter.Write(output, XRegisterTag, register);
            if (type is not null)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type.Value);
            }
        }
    }
}
