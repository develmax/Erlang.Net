using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ReallocateSingletonRecordTail(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 1; start + 8 < body.Count; start++)
        {
            if (body[start - 1].Definition.Opcode != ExternalCall
                || body[start] is not
                { Definition.Opcode: IsTaggedTuple, Operands: [var failure, var result, _, _] }
                || failure.Kind != BeamOperandParameters.Label
                || !IsPlainX(result, 0) || !IsTagTest(
                    body[start],
                    failure,
                    result,
                    2
                )
                || !IsSecondField(body[start + 1], result, result)
                || body[start + 2].Definition.Opcode != IsNonemptyList
                || !body[start + 2].Operands.SequenceEqual([failure, result])
                || body[start + 3] is not { Definition.Opcode: GetList, Operands: [var list, var head, var tail] }
                || list != result || !IsPlainX(head, 1) || !IsPlainX(tail, 2)
                || !IsTagTest(
                    body[start + 4],
                    failure,
                    head,
                    2
                )
                || !IsSecondField(body[start + 5], head, result)
                || body[start + 6] is not
                { Definition.Opcode: IsTaggedTuple, Operands: [_, _, var recordArity, _] }
                || recordArity.Kind != BeamOperandParameters.Unsigned
                || !IsTagTest(
                    body[start + 6],
                    failure,
                    result,
                    (int)recordArity.Value
                )
                || body[start + 7].Definition.Opcode != IsNil
                || !body[start + 7].Operands.SequenceEqual([failure, tail]))
            {
                continue;
            }

            int liveHeap = start + 8;
            int tupleAt = liveHeap + 1;
            int priorField = 0;
            var projections = new List<BeamInstruction>();
            var seenRegisters = new HashSet<BeamOperand>();
            while (tupleAt < body.Count && body[tupleAt] is
                { Definition.Opcode: GetTupleElement, Operands: [var record, var field, var destination] }
                && record == result && field.Kind == BeamOperandParameters.Unsigned
                && field.Value > priorField && field.Value < recordArity.Value
                && destination is { Kind: BeamOperandParameters.X, TypeIndex: null }
                && !seenRegisters.Contains(result) && seenRegisters.Add(destination))
            {
                projections.Add(body[tupleAt++]);
                priorField = (int)field.Value;
            }

            if (projections.Count < 2
                || !ReadArm(liveHeap, tupleAt, 1)
                || body[tupleAt + 6] is not { Definition.Opcode: Label, Operands: [var fallbackLabel] }
                || fallbackLabel.Value != failure.Value
                || !ReadArm(tupleAt + 7, tupleAt + 8, 0))
            {
                continue;
            }

            int fallbackTupleAt = tupleAt + 8;
            var success = body[tupleAt].Operands;
            var fallback = body[fallbackTupleAt].Operands;
            if (!body[tupleAt + 5].Operands.SequenceEqual(body[fallbackTupleAt + 5].Operands)
                || !body[tupleAt + 2].Operands.SequenceEqual(body[fallbackTupleAt + 2].Operands)
                || !body[tupleAt + 3].Operands.SequenceEqual(body[fallbackTupleAt + 3].Operands)
                || !success.Skip(2).Where(operand => operand.Kind == BeamOperandParameters.X).ToHashSet()
                    .SetEquals(seenRegisters)
                || fallback.Skip(2).Any(operand => operand.Kind == BeamOperandParameters.X))
            {
                continue;
            }

            // The old list and its tested tail have no use in either terminal arm.
            // Keep the head and record in X1, then allocate the field intervals
            // in field order. The last projection can reuse the record register.
            output.Write(code.Span[copied..body[start + 3].Offset]);
            output.WriteByte(GetList);
            WriteRegister(output, result);
            WriteRegister(output, head);
            WriteRegister(output, result);
            CopyInstruction(start + 4);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, head);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            WriteRegister(output, head);
            WriteTagTest(body[start + 6], head);
            output.WriteByte(IsNil);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            WriteRegister(output, result);
            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[liveHeap].Operands[0].Value);
            // X0 contains the tested nil, so the roots form a valid X0..X1 prefix.
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);

            var registers = new Dictionary<BeamOperand, BeamOperand>();
            for (int index = 0; index < projections.Count; index++)
            {
                var operands = projections[index].Operands;
                var register = index == projections.Count - 1
                    ? head
                    : result with { Value = index == 0 ? 0 : index + 1 };
                registers.Add(operands[2], register);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, head);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operands[1].Value);
                WriteRegister(output, register);
            }

            WriteArm(tupleAt, registers);
            CopyInstruction(tupleAt + 6);
            CopyInstruction(tupleAt + 7);
            WriteArm(fallbackTupleAt, registers);
            copied = body[fallbackTupleAt + 6].Offset;
            start = fallbackTupleAt + 5;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool ReadArm(int heapAt, int constructor, int live)
        {
            if (constructor + 6 >= body.Count
                || body[heapAt] is not { Definition.Opcode: TestHeap, Operands: [var words, var liveCount] }
                || (code.Span[body[heapAt].Offset + 1] & BeamWire.TagMask) != BeamWire.UnsignedTag
                || liveCount.Kind != BeamOperandParameters.Unsigned || liveCount.Value != live
                || body[constructor] is not { Definition.Opcode: PutTuple, Operands: [var target, var arity, ..] }
                || !IsPlainX(target, 0) || arity.Kind != BeamOperandParameters.Count || words.Value != arity.Value + 1
                || body[constructor].Operands.Skip(2).Any(operand => !CanWriteOperand(operand))
                || body[constructor + 1] is not { Definition.Opcode: Move, Operands: [var source, var argument] }
                || source != target || !IsPlainX(argument, 1)
                || !IsStackLoad(body[constructor + 2], 0) || !IsStackLoad(body[constructor + 3], 2)
                || body[constructor + 4].Definition.Opcode != Line
                || body[constructor + 5] is not
                { Definition.Opcode: TailExternalCallWithFrame, Operands: [var args, _, var frame] }
                || args.Value != 3)
            {
                return false;
            }

            return body[constructor].Operands.Skip(2)
                .Concat([body[constructor + 2].Operands[0], body[constructor + 3].Operands[0]])
                .All(operand => operand.Kind != BeamOperandParameters.Y || operand.Value < frame.Value);
        }

        void CopyInstruction(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void WriteTagTest(BeamInstruction instruction, BeamOperand tested)
        {
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, instruction.Operands[0].Value);
            WriteRegister(output, tested);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, instruction.Operands[2].Value);
            BeamCompactWriter.Write(output, AtomTag, instruction.Operands[3].Value);
        }

        void WriteArm(int constructor, Dictionary<BeamOperand, BeamOperand> registers)
        {
            var operands = body[constructor].Operands;
            output.WriteByte(PutTuple);
            WriteRegister(output, body[constructor + 1].Operands[1]);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operands[1].Value);
            foreach (BeamOperand operand in operands.Skip(2))
            {
                switch (operand.Kind)
                {
                    case BeamOperandParameters.X:
                        WriteRegister(output, registers[operand]);
                        break;
                    case BeamOperandParameters.Y:
                        WriteRegister(output, operand);
                        break;
                    case BeamOperandParameters.Integer:
                        BeamCompactWriter.Write(output, BeamWire.IntegerTag, operand.Value);
                        break;
                    case BeamOperandParameters.LiteralValue:
                        output.WriteByte(ExtendedLiteralOperand);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operand.Value);
                        break;
                    default:
                        BeamCompactWriter.Write(output, AtomTag, operand.Value);
                        break;
                }
            }

            // X0 is the final move after the independent stack load to X2.
            CopyInstruction(constructor + 3);
            CopyInstruction(constructor + 2);
            CopyInstruction(constructor + 4);
            CopyInstruction(constructor + 5);
        }

        static bool IsPlainX(BeamOperand operand, int index) =>
            operand is { Kind: BeamOperandParameters.X, TypeIndex: null } && operand.Value == index;

        static bool IsSecondField(BeamInstruction instruction, BeamOperand from, BeamOperand to) =>
            instruction is { Definition.Opcode: GetTupleElement, Operands: [var source, var field, var destination] }
            && source == from && field.Kind == BeamOperandParameters.Unsigned && field.Value == 1 && destination == to;

        static bool IsTagTest(
            BeamInstruction instruction,
            BeamOperand fail,
            BeamOperand value,
            int count
        ) =>
            instruction is { Definition.Opcode: IsTaggedTuple, Operands: [var target, var source, var arity, var atom] }
            && target == fail && source == value && arity.Kind == BeamOperandParameters.Unsigned
            && arity.Value == count && atom.Kind == BeamOperandParameters.Atom;

        static bool IsStackLoad(BeamInstruction instruction, int register) =>
            instruction is { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null }, var x] }
            && IsPlainX(x, register);

        static bool CanWriteOperand(BeamOperand operand) => operand.TypeIndex is null
            && operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y or BeamOperandParameters.Atom
                or BeamOperandParameters.Nil or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue;
    }
}
