using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? SinkCheckedTupleJoinFields(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        BeamProjectionFlow? flow = null;
        var carrier = new BeamOperand(BeamOperandParameters.X, 0);
        var choice = carrier with { Value = 1 };
        var value = carrier with { Value = 2 };
        var descriptor = carrier with { Value = 3 };
        var selector = carrier with { Value = 4 };
        using var output = new MemoryStream();
        int copied = 0;
        for (int entry = 2; entry + 24 < body.Count; entry++)
        {
            if (body[entry - 2] is not
                { Definition.Opcode: FunctionInfo, Operands: [_, _, { Kind: BeamOperandParameters.Unsigned, Value.IsOne: true }] }
                || body[entry - 1].Definition.Opcode != Label
                || !IsTagged(body[entry], carrier, out BeamOperand outerArity)
                || body[entry + 1] is not { Definition.Opcode: Move }
                || !body[entry + 1].Operands.SequenceEqual([carrier, choice])
                || !ReadField(
                    body[entry + 2],
                    choice,
                    carrier,
                    outerArity,
                    out BeamOperand left
                )
                || !ReadField(
                    body[entry + 3],
                    choice,
                    value,
                    outerArity,
                    out BeamOperand right
                )
                || !ReadField(
                    body[entry + 4],
                    choice,
                    descriptor,
                    outerArity,
                    out BeamOperand late
                )
                || !ReadField(
                    body[entry + 5],
                    choice,
                    selector,
                    outerArity,
                    out BeamOperand test
                )
                || left.Value <= 0 || right.Value <= left.Value || late.Value <= right.Value || test.Value <= late.Value)
            {
                continue;
            }

            int first = entry + 6;
            if (!IsExactAtomTest(body[first], value)
                || !ReadArm(first + 1, out int firstJump, out int firstLive)
                || body[firstJump] is not { Definition.Opcode: Jump, Operands: [var joinLabel] }
                || firstJump + 2 >= body.Count || !AtLabel(firstJump + 1, body[first].Operands[0])
                || !IsExactAtomTest(body[firstJump + 2], carrier))
            {
                continue;
            }

            int second = firstJump + 2;
            if (!ReadArm(second + 1, out int secondJump, out int secondLive)
                || body[secondJump] is not { Definition.Opcode: Jump, Operands: [var otherJoin] }
                || otherJoin != joinLabel || !AtLabel(secondJump + 1, body[second].Operands[0])
                || !ReadArm(secondJump + 2, out int join, out int thirdLive)
                || !AtLabel(join, joinLabel) || join + 13 >= body.Count)
            {
                continue;
            }

            int guard = join + 1;
            int finalHeap = guard + 7;
            int finalTuple = finalHeap + 1;
            int fallback = finalHeap + 3;
            int done = fallback + 3;
            if (!IsTagged(body[guard], selector, out BeamOperand innerArity)
                || !ReadField(
                    body[guard + 1],
                    selector,
                    value,
                    innerArity,
                    out BeamOperand payload
                )
                || body[guard + 2] is not { Definition.Opcode: IsList, Operands: [var tryBinary, var listValue] }
                || listValue != value
                || body[guard + 3] is not { Definition.Opcode: Jump, Operands: [var accept] }
                || !AtLabel(guard + 4, tryBinary)
                || body[guard + 5] is not { Definition.Opcode: IsBinary, Operands: [var badBinary, var binaryValue] }
                || binaryValue != value || badBinary != body[guard].Operands[0]
                || !AtLabel(guard + 6, accept)
                || !IsHeap(finalHeap, out BeamOperand words)
                || body[finalTuple] is not { Definition.Opcode: PutTuple, Operands: [var destination, { Kind: BeamOperandParameters.Count } count, ..] }
                || destination != carrier || words.Value != count.Value + 1
                || !ReadFinalOperands(body[finalTuple])
                || body[finalTuple + 1].Definition.Opcode != Return
                || !AtLabel(fallback, badBinary)
                || body[fallback + 1].Definition.Opcode != Move
                || !body[fallback + 1].Operands.SequenceEqual([choice, carrier])
                || body[fallback + 2].Definition.Opcode != Return
                || body[done].Definition.Opcode is not (Label or EndCode))
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(first, (int)body[first].Operands[0].Value, out int secondLabel)
                || secondLabel != firstJump + 1
                || !flow.TryGetSingleForwardTarget(second, (int)body[second].Operands[0].Value, out int thirdLabel)
                || thirdLabel != secondJump + 1
                || !flow.TryGetExclusiveForwardTarget(
                    firstJump,
                    secondJump,
                    join - 1,
                    (int)joinLabel.Value,
                    out int verifiedJoin
                ) || verifiedJoin != join
                || !flow.TryGetSingleForwardTarget(guard + 2, (int)tryBinary.Value, out int binaryLabel)
                || binaryLabel != guard + 4
                || !flow.TryGetExclusiveForwardTarget(
                    guard + 3,
                    guard + 5,
                    (int)accept.Value,
                    out int acceptLabel
                )
                || acceptLabel != guard + 6
                || !flow.TryGetExclusiveForwardTarget(
                    guard,
                    guard + 5,
                    (int)badBinary.Value,
                    out int fallbackLabel
                )
                || fallbackLabel != fallback)
            {
                continue;
            }

            // The checked tuple stays in X0 on both outcomes. The two scalar
            // comparisons and their pure constructions share X1 as the join value.
            output.Write(code.Span[copied..body[entry + 1].Offset]);
            WriteField(carrier, left, choice);
            WriteField(carrier, right, value);
            Copy(first);
            WriteArm(first + 1, firstJump, firstLive);
            Copy(firstJump);
            Copy(secondLabel);
            WriteTest(second, choice);
            WriteArm(second + 1, secondJump, secondLive);
            Copy(secondJump);
            Copy(thirdLabel);
            WriteArm(thirdLabel + 1, join, thirdLive);

            Copy(join);
            WriteField(carrier, test, value);
            WriteTest(guard, value);
            WriteField(value, payload, value);
            output.Write(code.Span[body[guard + 2].Offset..body[finalHeap].Offset]);
            WriteHeap(finalHeap, 3);
            WriteField(carrier, late, carrier);
            WriteTuple(finalTuple, carrier, terminal: true);
            Copy(finalTuple + 1);
            Copy(fallback);
            Copy(fallback + 2);
            copied = body[done].Offset;
            entry = done - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool AtLabel(int index, BeamOperand target) =>
            index < body.Count && target.Kind == BeamOperandParameters.Label
            && body[index] is { Definition.Opcode: Label, Operands: [var label] } && label.Value == target.Value;

        bool IsHeap(int index, out BeamOperand words)
        {
            if (index < body.Count && body[index] is { Definition.Opcode: TestHeap, Operands: [var count, var live] }
                && count.Kind == BeamOperandParameters.Unsigned && live.Kind == BeamOperandParameters.Unsigned
                && (code.Span[body[index].Offset + 1] & BeamWire.TagMask) == BeamWire.UnsignedTag)
            {
                words = count;

                return true;
            }

            words = null!;

            return false;
        }

        bool ReadArm(int start, out int next, out int liveCount)
        {
            next = start + 1;
            liveCount = 0;
            if (!IsHeap(start, out BeamOperand words))
            {
                return false;
            }

            long reserved = 0;
            while (next < body.Count && body[next] is
                { Definition.Opcode: PutTuple, Operands: [var target, { Kind: BeamOperandParameters.Count } arity, ..] })
            {
                if (target != carrier || arity.Value < 0 || arity.Value >= FirstReservedXRegister
                    || body[next].Operands.Skip(2).Any(operand => operand != carrier && operand != value
                        && !IsTupleJoinLiteral(operand)))
                {
                    return false;
                }

                reserved += (int)arity.Value + 1;
                next++;
            }

            if (next >= body.Count || next == start + 1 || reserved != words.Value)
            {
                return false;
            }

            // X0 and the joined X1 are live at the arm exit. Each tuple redefines
            // X1, so propagate just its old X1 and X2 inputs backwards.
            int live = 0b011;
            for (int index = next - 1; index > start; index--)
            {
                var operands = body[index].Operands.Skip(2);
                live &= ~0b010;
                foreach (BeamOperand operand in operands)
                {
                    live |= operand == carrier ? 0b010 : operand == value ? 0b100 : 0;
                }
            }

            liveCount = (live & 0b100) != 0 ? 3 : (live & 0b010) != 0 ? 2 : 1;

            return true;
        }

        bool ReadFinalOperands(BeamInstruction instruction)
        {
            BeamOperand[] operands = instruction.Operands.Skip(2).ToArray();

            return operands.Count(operand => operand == descriptor) == 1
                && operands.Any(operand => operand == carrier) && operands.Any(operand => operand == value)
                && operands.All(operand => operand == carrier || operand == value || operand == descriptor
                    || IsTupleJoinLiteral(operand));
        }

        void Copy(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void WriteArm(int start, int end, int live)
        {
            WriteHeap(start, live);
            for (int index = start + 1; index < end; index++)
            {
                WriteTuple(index, choice, terminal: false);
            }
        }

        void WriteHeap(int index, int live)
        {
            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[index].Operands[0].Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
        }

        void WriteTuple(int index, BeamOperand target, bool terminal)
        {
            var operands = body[index].Operands;
            output.WriteByte(PutTuple);
            WriteRegister(output, target);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operands[1].Value);
            foreach (BeamOperand operand in operands.Skip(2))
            {
                BeamOperand source = operand == carrier ? choice : operand;
                if (terminal && operand == descriptor)
                {
                    source = carrier;
                }

                WriteTupleJoinOperand(output, source);
            }
        }

        void WriteTest(int index, BeamOperand input)
        {
            var instruction = body[index];
            output.WriteByte(instruction.Definition.Opcode);
            BeamCompactWriter.Write(output, LabelTag, instruction.Operands[0].Value);
            WriteRegister(output, input);
            if (instruction.Definition.Opcode == IsTaggedTuple)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, instruction.Operands[2].Value);
            }

            BeamCompactWriter.Write(output, AtomTag, instruction.Operands[^1].Value);
        }

        void WriteField(BeamOperand from, BeamOperand field, BeamOperand to)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, from);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, to);
        }

        static bool IsTagged(BeamInstruction instruction, BeamOperand source, out BeamOperand width)
        {
            if (instruction is
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label }, var input,
                    { Kind: BeamOperandParameters.Unsigned } arity, { Kind: BeamOperandParameters.Atom }]
                } && input == source)
            {
                width = arity;

                return true;
            }

            width = null!;

            return false;
        }

        static bool ReadField(
            BeamInstruction instruction,
            BeamOperand source,
            BeamOperand destination,
            BeamOperand arity,
            out BeamOperand field
        )
        {
            if (instruction is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var from, { Kind: BeamOperandParameters.Unsigned } index, var to]
                } && from == source && to == destination && index.Value >= 0 && index.Value < arity.Value)
            {
                field = index;

                return true;
            }

            field = null!;

            return false;
        }

        static bool IsExactAtomTest(BeamInstruction instruction, BeamOperand source) =>
            instruction is
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [{ Kind: BeamOperandParameters.Label }, var input, { Kind: BeamOperandParameters.Atom }]
            } && input == source;
    }

    private static bool IsTupleJoinLiteral(BeamOperand operand) =>
        operand.Kind is BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue;

    private static void WriteTupleJoinOperand(Stream output, BeamOperand operand)
    {
        switch (operand.Kind)
        {
            case BeamOperandParameters.X:
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
}
