using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleStackFieldBeforeListConstruction(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteStackFieldBeforeListConstruction(code, body);
    }

    private static byte[]? RewriteStackFieldBeforeListConstruction(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int heap = 0; heap + 4 < body.Count; heap++)
        {
            if (body[heap].Definition.Opcode != TestHeap
                || body[heap + 1] is not
                {
                    Definition.Opcode: PutTuple, Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } tuple, _, ..]
                } construction
                || body[heap + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null }, _, { Kind: BeamOperandParameters.X, TypeIndex: null } tail]
                } || tuple == tail || construction.Operands.Skip(2).Any(operand => SameProjectionRegister(operand, tail))
                || body[heap + 3] is not { Definition.Opcode: PutList, Operands: [var head, var rest, var result] }
                || head != tuple || rest != tail || result != tuple)
            {
                continue;
            }

            int construct = body[heap + 1].Offset;
            int project = body[heap + 2].Offset;
            int list = body[heap + 3].Offset;

            // Both instructions remain inside the same reserved heap region.
            // Constructing the head neither reads the projected tail nor writes its carrier.
            output.Write(code.Span[copied..construct]);
            output.Write(code.Span[project..list]);
            output.Write(code.Span[construct..project]);
            copied = list;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private byte[]? ScheduleTerminalPairBeforeListConstruction(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var tail = new BeamOperand(BeamOperandParameters.X, 1);
        var scratch = new BeamOperand(BeamOperandParameters.X, 2);
        int copied = 0;
        for (int heap = 0; heap + 9 < body.Count; heap++)
        {
            if (body[heap] is not
                {
                    Definition.Opcode: TestHeap,
                    Operands: [{ Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 }, { Value.IsOne: true }]
                }
                || !IsProjection(body[heap + 1], result, tail)
                || !IsProjection(body[heap + 2], result, scratch)
                || body[heap + 1].Operands[1] == body[heap + 2].Operands[1]
                || body[heap + 3] is not { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] } tuple
                || destination != result
                || body[heap + 4] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null }, _, var projectedTail]
                }
                || projectedTail != tail
                || body[heap + 5] is not { Definition.Opcode: PutList, Operands: [var head, var rest, var list] }
                || head != result || rest != tail || list != result
                || body[heap + 6] is not { Definition.Opcode: UpdateRecord, Operands: [_, _, _, var returned, ..] } update
                || returned != result || update.Operands.Any(operand => SameProjectionRegister(operand, scratch))
                || body[heap + 7].Definition.Opcode != Deallocate || body[heap + 8].Definition.Opcode != Return)
            {
                continue;
            }

            var fields = tuple.Operands.Skip(2).ToArray();
            if (fields.Count(field => field == tail) != 1 || fields.Count(field => field == scratch) != 1
                || fields.Any(field => field.TypeIndex is not null || field != tail && field != scratch
                    && field.Kind is not
                        (BeamOperandParameters.Y or BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil)))
            {
                continue;
            }

            // The Y carrier is independent of the returned tuple. Its tail
            // keeps X1 through the pair construction. The last read of X0 can
            // use X0 before the newly constructed tuple replaces that field.
            output.Write(code.Span[copied..body[heap + 1].Offset]);
            output.Write(code.Span[body[heap + 4].Offset..body[heap + 5].Offset]);
            WriteField(body[heap + 1].Operands[1], scratch);
            WriteField(body[heap + 2].Operands[1], result);

            int start = tuple.Offset + 1;
            var operands = new BeamCursor(code[start..body[heap + 4].Offset]);
            _ = BeamCompactReader.Read(operands);
            _ = operands.ReadByte(); // Extended list marker.
            _ = BeamCompactReader.Read(operands);
            int previous = tuple.Offset;
            foreach (var field in fields)
            {
                int at = start + operands.Position;
                _ = BeamCompactReader.Read(operands);
                if (field == tail || field == scratch)
                {
                    output.Write(code.Span[previous..at]);
                    WriteRegister(output, field == tail ? scratch : result);
                    previous = start + operands.Position;
                }
            }

            output.Write(code.Span[previous..body[heap + 4].Offset]);
            copied = body[heap + 5].Offset;
            heap += 5;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsProjection(BeamInstruction instruction, BeamOperand source, BeamOperand destination) =>
            instruction is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var tuple, { Kind: BeamOperandParameters.Unsigned }, var projected]
            }
            && source == tuple && destination == projected;

        void WriteField(BeamOperand field, BeamOperand destination)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, result);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, destination);
        }
    }
}
