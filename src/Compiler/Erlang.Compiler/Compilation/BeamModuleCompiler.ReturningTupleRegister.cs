using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ReuseReturningTupleRegister(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 4 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                || body[index + 1] is not { Definition.Opcode: PutTuple, Operands: [var temp, _, ..] } inner
                || temp is not { Kind: BeamOperandParameters.X, TypeIndex: null } || temp == result
                || body[index + 2] is not { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] } outer
                || destination != result || !inner.Operands.Skip(2).All(IsStable)
                || !outer.Operands.Skip(2).All(field => field == temp || IsStable(field))
                || !outer.Operands.Skip(2).Contains(temp))
            {
                continue;
            }

            int returned = index + 3;
            if (body[returned].Definition.Opcode == Deallocate)
            {
                returned++;
            }

            if (body[returned].Definition.Opcode != Return)
            {
                continue;
            }

            // These pure constructors share a heap check and have no active X
            // root at its entry. Their common output can occupy X0 throughout.
            output.Write(code.Span[copied..inner.Offset]);
            WriteTuple(inner, result, oldRegister: null);
            WriteTuple(outer, result, oldRegister: temp);
            copied = body[index + 3].Offset;
            index = returned;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsStable(BeamOperand operand) => operand.TypeIndex is null && operand.Kind is
            BeamOperandParameters.Y or BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil;

        void WriteTuple(BeamInstruction tuple, BeamOperand register, BeamOperand? oldRegister)
        {
            output.WriteByte(PutTuple);
            WriteRegister(output, register);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tuple.Operands[1].Value);
            foreach (var field in tuple.Operands.Skip(2))
            {
                int tag = field.Kind switch
                {
                    BeamOperandParameters.Atom or BeamOperandParameters.Nil => AtomTag,
                    BeamOperandParameters.Integer => BeamWire.IntegerTag,
                    BeamOperandParameters.Y => YRegisterTag,
                    _ => XRegisterTag,
                };
                BeamCompactWriter.Write(output, tag, field == oldRegister ? register.Value : field.Value);
            }
        }
    }

    private byte[]? ReuseTerminalProjectionCarrier(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var sibling = new BeamOperand(BeamOperandParameters.X, 1);
        var nil = new BeamOperand(BeamOperandParameters.Nil, NilAtomIndex);
        var edits = new List<CodeEdit>();
        for (int start = 0; start + 6 < body.Count; start++)
        {
            if (body[start] is not { Definition.Opcode: IsTuple, Operands: [var failure, var tuple] }
                || tuple != result || failure.Kind != BeamOperandParameters.Label
                || body[start + 1] is not
                { Definition.Opcode: TestArity, Operands: [var arityFailure, var checkedTuple, var arity] }
                || arityFailure != failure || checkedTuple != result
                || body[start + 2] is not
                {
                    Definition.Opcode: TestHeap,
                    Operands: [{ Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 }, var live]
                }
                || live.Value < 1 || live.Value > 2)
            {
                continue;
            }

            int projectionIndex = start + 3;
            BeamOperand? siblingField = null;
            if (live.Value.IsOne && body[projectionIndex] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var original, { Kind: BeamOperandParameters.Unsigned } earlierField, var retained]
                }
                && original == result && retained == sibling && earlierField.Value.Sign >= 0
                && earlierField.Value < arity.Value && body[projectionIndex + 1].Definition.Opcode == GetTupleElement)
            {
                siblingField = earlierField;
                projectionIndex++;
            }

            bool hasSibling = live.Value == 2 || siblingField is not null;
            if (hasSibling && (start < 2 || body[start - 1].Definition.Opcode != Label
                || body[start - 2].Definition.Opcode != FunctionInfo))
            {
                continue;
            }

            var scratch = new BeamOperand(BeamOperandParameters.X, hasSibling ? 2 : 1);
            int constructorIndex = projectionIndex + 1;
            int terminal = constructorIndex + 1;
            BeamInstruction construction = body[constructorIndex];
            bool isTuple = construction.Definition.Opcode == PutTuple;
            if (terminal >= body.Count || body[projectionIndex] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var carrier, { Kind: BeamOperandParameters.Unsigned } field, var projected]
                }
                || carrier != result || projected != scratch || field.Value.Sign < 0 || field.Value >= arity.Value
                || (isTuple ? construction.Operands[0] != result
                    : construction is not { Definition.Opcode: PutList, Operands: [_, _, var target] } || target != result))
            {
                continue;
            }

            if (isTuple && body[terminal] is { Definition.Opcode: PutList, Operands: [var head, var tail, var list] }
                && head == result && tail == nil && list == result)
            {
                terminal++;
            }

            if (terminal >= body.Count || body[terminal].Definition.Opcode != Return)
            {
                continue;
            }

            var fields = (isTuple ? construction.Operands.Skip(2) : construction.Operands.Take(2)).ToArray();
            if (fields.Count(operand => operand == scratch) != 1 || fields.Any(operand =>
                operand.TypeIndex is not null || operand != scratch && !(hasSibling && operand == sibling)
                    && operand.Kind is not (BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil)))
            {
                continue;
            }

            // A checked pair reconstructed in source order is handled by the
            // tuple identity rule. It needs neither projection nor construction.
            if (isTuple && arity.Value == 2 && siblingField is { Value.IsZero: true } && field.Value.IsOne
                && fields.SequenceEqual(new[] { sibling, scratch }))
            {
                continue;
            }

            // Keep the carrier rooted through the heap check and its earlier
            // projection. Only the final read can reuse the carrier register.
            int projectionStart = body[projectionIndex].Offset + 1;
            var projection = new BeamCursor(code[projectionStart..construction.Offset]);
            _ = BeamCompactReader.Read(projection);
            _ = BeamCompactReader.Read(projection);
            AddReplacement(projection, projectionStart);

            int constructionStart = construction.Offset + 1;
            var operandCursor = new BeamCursor(code[constructionStart..body[constructorIndex + 1].Offset]);
            if (isTuple)
            {
                _ = BeamCompactReader.Read(operandCursor);
                _ = operandCursor.ReadByte(); // Extended list marker.
                _ = BeamCompactReader.Read(operandCursor);
            }

            foreach (var operand in fields)
            {
                if (operand == scratch)
                {
                    AddReplacement(operandCursor, constructionStart);
                    break;
                }

                _ = BeamCompactReader.Read(operandCursor);
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        void AddReplacement(BeamCursor cursor, int start)
        {
            int from = start + cursor.Position;
            _ = BeamCompactReader.Read(cursor);
            edits.Add(new CodeEdit(
                from,
                start + cursor.Position,
                XRegisterTag,
                (int)result.Value
            ));
        }
    }
}
