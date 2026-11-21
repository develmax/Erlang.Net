using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RetainPositionedBinaryTail(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<string> atomNames,
        IReadOnlyList<(int Module, int Function, int Arity)> importsByIndex
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        ILookup<BeamOperand, BeamOperand>? transfers = null;
        for (int index = 1; index + 15 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: BinaryMatch,
                    Operands: [
                        var bad, var context, _,
                        var ensure, var width, var alignment,
                        var integer, var integerLive, var flags, var bits, var bitUnit, var head,
                        var takeTail, _, var tailUnit, var tail
                    ]
                }
                || !IsX(context, 1) || bad.Kind != BeamOperandParameters.Label
                || !IsAtom(ensure, BeamBinaryNames.EnsureAtLeast) || !IsAtom(integer, BeamBinaryNames.IntegerSegment)
                || !IsAtom(takeTail, BeamBinaryNames.GetTail)
                || width.Value != bits.Value || bits.Value <= 0 || bits.Value > BitSyntaxDefaults.BinaryUnit
                || alignment.Value != tailUnit.Value
                || tailUnit.Value != BitSyntaxDefaults.BitStringUnit && tailUnit.Value != BitSyntaxDefaults.BinaryUnit
                || bitUnit.Value != 1 || integerLive.Value != 2
                || !(flags.Kind == BeamOperandParameters.Nil || flags.Kind == BeamOperandParameters.LiteralValue
                    && ExternalTermFormat.Decode(literalBytes[(int)flags.Value]) is Nil)
                || body[index - 1] is not
                { Definition.Opcode: StartBinaryMatch, Operands: [var fail, var input, _, var matchContext] }
                || fail != bad || !IsX(input, 0) || matchContext != context)
            {
                continue;
            }

            int jump = index + 1;
            if (IsX(head, 2) && IsX(tail, 3))
            {
                if (!IsMove(body[jump], head, input)
                    || !IsMove(body[jump + 2], tail, input)
                    || body[jump + 1] is not { Definition.Opcode: Move, Operands: [var headFrom, var headSlot] }
                    || body[jump + 3] is not { Definition.Opcode: Move, Operands: [var tailFrom, var tailSlot] }
                    || headFrom != input || tailFrom != input)
                {
                    continue;
                }

                head = headSlot;
                tail = tailSlot;
                jump += 4;
            }

            if (!IsY(head) || !IsY(tail) || head == tail
                || body[jump] is not { Definition.Opcode: Jump, Operands: [var success] }
                || !IsLabel(body[jump + 1], bad)
                || body[jump + 2] is not { Definition.Opcode: Move, Operands: [var original, var reload] }
                || !IsY(original) || reload != input)
            {
                continue;
            }

            int badMatch = SkipTailLines(jump + 3);
            if (badMatch + 4 >= body.Count
                || body[badMatch] is not { Definition.Opcode: BadMatch, Operands: [var failedValue] }
                || failedValue != input || !IsLabel(body[badMatch + 1], success))
            {
                continue;
            }

            int band = SkipTailLines(badMatch + 2);
            int test = band + 1;
            if (test >= body.Count
                || body[band] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [{ Kind: BeamOperandParameters.Failure }, var live, var bif, var operand, var mask, var bandResult]
                }
                || live.Value != 0 || operand != head || bandResult != input
                || !IsBif(bif, ErlangOperators.BitwiseAnd, 2) || mask.Kind != BeamOperandParameters.Integer
                || mask.Value < 0 || mask.Value > byte.MaxValue
                || body[test] is not { Definition.Opcode: ExactEqualTest, Operands: [var fallback, var tested, var expected] }
                || fallback.Kind != BeamOperandParameters.Label || tested != input || expected != mask
                || ReadArm(test + 1, head, tail) is not { } left
                || !IsLabel(body[left.Construction + 3], fallback)
                || ReadArm(left.Construction + 4, head, tail) is not { } right)
            {
                continue;
            }

            // The context and head stay in X registers only along these closed
            // success paths. Preserve the original input reload on match failure.
            transfers ??= body.SelectMany(instruction => instruction.Operands)
                .Where(operand => operand.Kind == BeamOperandParameters.Label)
                .ToLookup(operand => operand);
            if (transfers[success].Count() != 1 || transfers[fallback].Count() != 1)
            {
                continue;
            }

            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(BinaryMatch);
            WriteValue(bad);
            WriteValue(context);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 9);
            foreach (BeamOperand field in body[index].Operands.Skip(3).Take(8))
            {
                WriteValue(field);
            }

            WriteValue(input);
            Copy(jump, band);
            output.WriteByte(GcBifTwo);
            WriteValue(body[band].Operands[0]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            WriteValue(bif);
            WriteValue(input);
            WriteValue(mask);
            WriteX(2);
            output.WriteByte(ExactEqualTest);
            WriteValue(fallback);
            WriteX(2);
            WriteValue(expected);
            int? headType = SsaTypeInferenceEnabled && ReadFixedWidth(bits, bitUnit, signed: false) is { } bounds
                ? GetOperandTypeIndex((IntegerTypeBits, bounds.Lower, bounds.Upper, null))
                : null;
            EmitArm(
                left,
                test + 1,
                head,
                tail,
                headType,
                (int)tailUnit.Value
            );
            EmitArm(
                right,
                left.Construction + 3,
                head,
                tail,
                headType,
                (int)tailUnit.Value
            );
            copied = body[right.Construction + 3].Offset;
            index = right.Construction + 2;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        PositionedTailArm? ReadArm(int start, BeamOperand head, BeamOperand tail)
        {
            int size = SkipTailLines(start);
            int addition = SkipTailLines(size + 1);
            int store = SkipTailLines(addition + 1);
            int construction = SkipTailLines(store + 1);
            if (construction + 3 >= body.Count
                || body[size] is not
                {
                    Definition.Opcode: GcBifOne,
                    Operands: [{ Kind: BeamOperandParameters.Failure }, var live, var measure, var source, var measured]
                }
                || live.Value != 0 || source != tail || !IsX(measured, 0)
                || !(IsBif(measure, ErlangModuleNames.ByteSize, 1) || IsBif(measure, ErlangModuleNames.BitSize, 1))
                || body[addition] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [
                    { Kind: BeamOperandParameters.Failure }, var sumLive,
                        var add, var oldSize, { Kind: BeamOperandParameters.Integer }, var sum
                    ]
                }
                || sumLive.Value != 1 || !IsBif(add, ErlangOperators.Plus, 2)
                || !SameProjectionRegister(oldSize, measured) || sum != measured
                || body[store] is not { Definition.Opcode: Move, Operands: [var saved, var length] }
                || saved != measured || !IsY(length) || length == head || length == tail
                || body[construction] is not
                {
                    Definition.Opcode: CreateBinary,
                    Operands: [{ Kind: BeamOperandParameters.Failure }, var heap, var binaryLive, _, var target, var count, ..]
                }
                || heap.Value != 0 || binaryLive.Value != 0 || target != measured
                || body[construction + 1].Definition.Opcode != Deallocate || body[construction + 2].Definition.Opcode != Return)
            {
                return null;
            }

            // Each stack operand becomes a proved input register. All other
            // segments must have literal operands and therefore cannot alias it.
            var fields = body[construction].Operands.Skip(6).ToArray();
            if (count.Value != fields.Length || fields.Any(field => field != head && field != tail && field != length
                    && field.Kind is not (BeamOperandParameters.Unsigned or BeamOperandParameters.Integer
                        or BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue))
                || fields.Count(field => field == head) != 1 || fields.Count(field => field == tail) != 1
                || fields.Count(field => field == length) != 1)
            {
                return null;
            }

            return new(
                size,
                addition,
                store,
                construction,
                length
            );
        }

        void EmitArm(
            PositionedTailArm arm,
            int start,
            BeamOperand head,
            BeamOperand tail,
            int? headType,
            int tailUnit
        )
        {
            Copy(start, arm.Size);
            var size = body[arm.Size].Operands;
            output.WriteByte(GcBifOne);
            WriteValue(size[0]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            WriteValue(size[2]);
            int contextType = GetOperandTypeIndex((MatchableTypeBits, null, null, null));
            WriteTypedX(1, contextType);
            WriteX(2);

            // Keep source markers at their original operations. The final line
            // pass removes repeats after the rewritten instructions are in place.
            Copy(arm.Size + 1, arm.Addition);
            var sum = body[arm.Addition].Operands;
            output.WriteByte(GcBifTwo);
            WriteValue(sum[0]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 3);
            WriteValue(sum[2]);
            if (sum[3].TypeIndex is int type)
            {
                WriteTypedX(2, type);
            }
            else
            {
                WriteX(2);
            }

            WriteValue(sum[4]);
            WriteX(2);
            Copy(arm.Addition + 1, arm.Store);
            Copy(arm.Store + 1, arm.Construction);
            output.WriteByte(GetBinaryTail);
            WriteX(1);
            WriteX(1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 3);

            var construction = body[arm.Construction].Operands;
            output.WriteByte(CreateBinary);
            WriteValue(construction[0]);
            WriteValue(construction[1]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 3);
            WriteValue(construction[3]);
            WriteValue(construction[4]);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, construction[5].Value);
            int? lengthType = ReadPositionedLengthType(sum[3], sum[4]);
            foreach (var field in construction.Skip(6))
            {
                if (field == head)
                {
                    WriteConstructorRegister(0, headType);
                }
                else if (field == tail)
                {
                    // A successful positioned match proves the remaining tail unit.
                    // Materialization produces a bitstring with the same size unit.
                    int? tailType = SsaTypeInferenceEnabled
                        ? GetOperandTypeIndex((MatchableTypeBits, null, null, tailUnit))
                        : null;
                    WriteConstructorRegister(1, tailType);
                }
                else if (field == arm.Length)
                {
                    WriteConstructorRegister(2, lengthType);
                }
                else
                {
                    WriteValue(field);
                }
            }

            Copy(arm.Construction + 1, arm.Construction + 3);
        }

        int SkipTailLines(int start)
        {
            while (start < body.Count && body[start].Definition.Opcode == Line)
            {
                start++;
            }

            return start;
        }

        bool IsAtom(BeamOperand value, string name) =>
            value.Kind == BeamOperandParameters.Atom && atomNames[(int)value.Value - 1] == name;

        bool IsBif(BeamOperand operand, string name, int arity)
        {
            if (operand.Kind != BeamOperandParameters.Unsigned || operand.Value >= importsByIndex.Count)
            {
                return false;
            }

            var import = importsByIndex[(int)operand.Value];

            return import.Arity == arity && atomNames[import.Module - 1] == ErlangModuleNames.Module
                && atomNames[import.Function - 1] == name;
        }

        void Copy(int start, int end) => output.Write(code.Span[body[start].Offset..body[end].Offset]);

        void WriteX(int register) => BeamCompactWriter.Write(output, XRegisterTag, register);

        void WriteConstructorRegister(int register, int? type)
        {
            if (type is int index)
            {
                WriteTypedX(register, index);
            }
            else
            {
                WriteX(register);
            }
        }

        int? ReadPositionedLengthType(BeamOperand measured, BeamOperand addition)
        {
            if (!SsaTypeInferenceEnabled || measured.TypeIndex is not int type || (uint)type >= (uint)typeBits.Count
                || typeBits[type] is not (IntegerTypeBits, var lower, var upper, _))
            {
                return null;
            }

            return GetOperandTypeIndex((IntegerTypeBits, Add(lower), Add(upper), null));

            long? Add(long? bound)
            {
                if (bound is not long value)
                {
                    return null;
                }

                var result = value + addition.Value;

                return result >= BeamBinaryExtractRangeLimits.MinimumEncodedInteger
                    && result <= BeamBinaryExtractRangeLimits.MaximumEncodedInteger ? (long)result : null;
            }
        }

        void WriteTypedX(int register, int type)
        {
            output.WriteByte(ExtendedTypedRegisterOperand);
            WriteX(register);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
        }

        void WriteValue(BeamOperand operand)
        {
            int tag = operand.Kind switch
            {
                BeamOperandParameters.X => XRegisterTag,
                BeamOperandParameters.Failure or BeamOperandParameters.Label => LabelTag,
                BeamOperandParameters.Atom or BeamOperandParameters.Nil => AtomTag,
                BeamOperandParameters.Integer => BeamWire.IntegerTag,
                _ => BeamWire.UnsignedTag
            };
            if (operand.Kind == BeamOperandParameters.LiteralValue)
            {
                output.WriteByte(ExtendedLiteralOperand);
            }

            BeamCompactWriter.Write(output, tag, operand.Value);
        }

        static bool IsX(BeamOperand operand, int register) =>
            operand is { Kind: BeamOperandParameters.X, TypeIndex: null } && operand.Value == register;

        static bool IsY(BeamOperand operand) => operand is { Kind: BeamOperandParameters.Y, TypeIndex: null };

        static bool IsMove(BeamInstruction instruction, BeamOperand source, BeamOperand target) =>
            instruction.Definition.Opcode == Move && instruction.Operands.SequenceEqual([source, target]);

        static bool IsLabel(BeamInstruction instruction, BeamOperand label) =>
            instruction.Definition.Opcode == Label && instruction.Operands[0].Value == label.Value;
    }

    private readonly record struct PositionedTailArm(
        int Size,
        int Addition,
        int Store,
        int Construction,
        BeamOperand Length
    );
}
