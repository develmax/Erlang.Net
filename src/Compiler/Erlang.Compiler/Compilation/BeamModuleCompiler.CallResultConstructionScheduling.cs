using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? DelayCallResultStoreThroughConstruction(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        if (DelayCallResultStoreThroughNullaryArgument(
            code,
            body,
            atomNames,
            importsByIndex
        ) is { } delayed)
        {
            return delayed;
        }

        using var output = new MemoryStream();
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var construction = new BeamOperand(BeamOperandParameters.X, 1);
        int copied = 0;
        for (int index = 0; index + 5 < body.Count; index++)
        {
            if (body[index].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || source != result
                || body[index + 2] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                || body[index + 3] is not
                {
                    Definition.Opcode: BifZero,
                    Operands: [var import, var target]
                } || target != result
                || !IsHeapNeutralNullaryBif(importsByIndex[(int)import.Value], atomNames))
            {
                continue;
            }

            int bif = index + 3;
            int next = bif + 1;
            for (; next + 1 < body.Count; next++)
            {
                BeamInstruction instruction = body[next];
                var operands = instruction.Operands;
                int destination = instruction.Definition.Opcode switch
                {
                    PutTuple => 0,
                    PutList => operands.Count - 1,
                    _ => -1,
                };
                if (destination < 0
                    || operands.Any(operand => operand.TypeIndex is not null || SameProjectionRegister(operand, saved))
                    || operands.Where((_, operandIndex) => operandIndex != destination)
                        .Any(operand => operand.Kind == BeamOperandParameters.X && operand != result))
                {
                    break;
                }

                if (operands[destination] == construction)
                {
                    if (OverwritesCallResultBeforeRead(
                        body,
                        next + 1,
                        result,
                        saved
                    ))
                    {
                        RewriteCallResultConstruction(
                            output,
                            code,
                            body,
                            index,
                            next + 1,
                            copied,
                            construction
                        );
                        copied = body[++next].Offset;
                    }

                    break;
                }

                if (operands[destination] != result)
                {
                    break;
                }
            }

            index = next - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static byte[]? DelayCallResultStoreThroughNullaryArgument(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        using var output = new MemoryStream();
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var argument = new BeamOperand(BeamOperandParameters.X, 1);
        int copied = 0;
        for (int call = 0; call + 3 < body.Count; call++)
        {
            if (body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[call + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null }]
                } || source != result
                || body[call + 2] is not
                {
                    Definition.Opcode: BifZero,
                    Operands: [var import, var target]
                } || target != result
                || !IsHeapNeutralNullaryBif(importsByIndex[(int)import.Value], atomNames))
            {
                continue;
            }

            int consumer = call + 3;
            while (consumer < body.Count && body[consumer].Definition.Opcode == Line)
            {
                consumer++;
            }

            if (consumer == body.Count || body[consumer].Definition.Opcode is not (LocalCall or ExternalCall)
                || !body[consumer].Operands[0].Value.IsOne)
            {
                continue;
            }

            // The preceding call defines only X0. OTP delays saving its result
            // until the next call and computes the independent argument in X1.
            int save = body[call + 1].Offset;
            int bif = body[call + 2].Offset;
            output.Write(code.Span[copied..save]);
            output.WriteByte(BifZero);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, import.Value);
            WriteRegister(output, argument);
            output.Write(code.Span[save..bif]);
            output.WriteByte(Move);
            WriteRegister(output, argument);
            WriteRegister(output, result);
            copied = body[call + 3].Offset;
            call += 2;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool OverwritesCallResultBeforeRead(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand result,
        BeamOperand saved
    )
    {
        for (int i = start; i < body.Count; i++)
        {
            if (body[i] is not { Definition.Opcode: Move, Operands: [var source, var target] }
                || SameProjectionRegister(source, result)
                || SameProjectionRegister(source, saved)
                || target.Kind != BeamOperandParameters.X)
            {
                return false;
            }

            if (target == result)
            {
                return true;
            }
        }

        return false;
    }

    private static void RewriteCallResultConstruction(
        Stream output,
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int call,
        int end,
        int copied,
        BeamOperand construction
    )
    {
        int save = body[call + 1].Offset;
        int heap = body[call + 2].Offset;
        int bif = body[call + 3].Offset;
        output.Write(code.Span[copied..save]);
        // The preceding call verifies every Y slot. Keep X0 rooted through this heap check.
        output.Write(code.Span[heap..(bif - 1)]);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
        for (int index = call + 3; index < end; index++)
        {
            int opcode = body[index].Offset;
            output.WriteByte(code.Span[opcode]);
            var cursor = new BeamCursor(code[(opcode + 1)..body[index + 1].Offset]);
            while (cursor.Position < body[index + 1].Offset - opcode - 1)
            {
                int offset = cursor.Position;
                BeamCompactValue operand = BeamCompactReader.Read(cursor);
                if (operand.WireTag == XRegisterTag && operand.Value.IsZero)
                {
                    WriteRegister(output, construction);
                }
                else
                {
                    output.Write(code.Span[(opcode + 1 + offset)..(opcode + 1 + cursor.Position)]);
                }
            }
        }

        output.Write(code.Span[save..heap]);
    }
}
