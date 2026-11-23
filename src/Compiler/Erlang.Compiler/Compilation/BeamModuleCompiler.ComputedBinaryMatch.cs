using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static readonly byte[] ComputedBinaryMatchShape =
    [
        IsLess, Allocate, InitYRegisters, Move, Line, GcBifOne, ExternalCall,
        GcBifOne, Move, Line, GcBifTwo, Move, Line, CreateBinary, Move,
        StartBinaryMatchKnown, GetBinaryInteger, BinaryMatch, Jump, Label, Move,
        BadMatch, Label, Line, GcBifTwo, ExternalCall
    ];

    private byte[]? LocalizeComputedBinaryMatch(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (!SsaTypeInferenceEnabled || !atoms.ContainsKey(BinaryModuleNames.EncodeUnsigned))
        {
            return null;
        }

        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var callees = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        using var result = new MemoryStream();
        int copied = 0;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        var zero = new BeamOperand(BeamOperandParameters.Integer, 0);
        ILookup<int, int>? labelReferences = null;
        for (int start = 1; start + ComputedBinaryMatchShape.Length < body.Count; start++)
        {
            if (body[start].Definition.Opcode != IsLess || body[start - 1].Definition.Opcode != Label)
            {
                continue;
            }
            BeamInstruction[] shape = body.Skip(start).Take(ComputedBinaryMatchShape.Length).ToArray();
            if (!shape.Select(instruction => instruction.Definition.Opcode).SequenceEqual(ComputedBinaryMatchShape))
            {
                continue;
            }

            var lesser = shape[0].Operands;
            var allocate = shape[1].Operands;
            var save = shape[3].Operands;
            var negate = shape[5].Operands;
            var byteCount = shape[7].Operands;
            var sizeTransfer = shape[8].Operands;
            var width = shape[10].Operands;
            var widthTransfer = shape[11].Operands;
            var construct = shape[13].Operands;
            var saveConstructed = shape[14].Operands;
            var match = shape[15].Operands;
            var extract = shape[16].Operands;
            var exhaust = shape[17].Operands;
            var jump = shape[18].Operands[0];
            var restoreFailure = shape[20].Operands;
            var bad = shape[21].Operands[0];
            var add = shape[24].Operands;
            BeamOperand saved = save[1];
            BeamOperand size = sizeTransfer[1];
            BeamOperand widthSlot = widthTransfer[1];
            BeamOperand extracted = extract[^1];
            BeamOperand badLabel = extract[0];
            int frame = (int)allocate[0].Value;
            int negativeBlockLabel = (int)body[start - 1].Operands[0].Value;
            if (lesser[1] != x0 || lesser[2] != zero || frame != 4 || allocate[1].Value != 1
                || save[0] != x0 || !IsY(saved) || !IsY(size) || !IsY(widthSlot) || !IsY(extracted)
                || new[] { saved.Value, size.Value, widthSlot.Value, extracted.Value }.Distinct().Count() != frame
                || !shape[2].Operands.Skip(1).ToHashSet().SetEquals([size, widthSlot, extracted])
                || !IsBif(negate, ErlangOperators.Minus, 1) || negate[3] != saved || negate[4] != x0
                || !IsEncodingCall(shape[6]) || !IsEncodingCall(shape[25])
                || !IsBif(byteCount, AutoImportNames.BitSize, 1) || byteCount[3] != x0 || byteCount[4] != x0
                || sizeTransfer[0] != x0 || widthTransfer[0] != x0
                || !IsBif(width, ErlangOperators.Plus, 2) || !SameRegister(width[3], size)
                || width[3].TypeIndex is not int sizeType || width[4].Kind != BeamOperandParameters.Integer
                || width[4].Value <= 0 || width[4].Value > long.MaxValue - MaximumBitSize || width[5] != x0
                || typeBits[sizeType] is not (IntegerTypeBits, 0, MaximumBitSize, null)
                || construct.Count < 8 || construct[2].Value != 0 || construct[4] != x0 || construct[^1] != size
                || construct.Skip(6).SkipLast(1).Any(operand => operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y)
                || saveConstructed[0] != x0 || saveConstructed[1] != size
                || !IsAtom(match[0], BeamBinaryNames.NoFail) || match[1].Value != 1 || match[2] != x0 || match[3] != x1
                || badLabel.Kind != BeamOperandParameters.Label || !SameRegister(extract[1], x1)
                || extract[1].TypeIndex is not int contextType
                || extract[2].Value != 2 || extract[3] != widthSlot || !extract[4].Value.IsOne || !extract[5].Value.IsZero
                || exhaust.Count != 5 || exhaust[0] != badLabel || exhaust[1] != x1
                || !IsAtom(exhaust[3], BeamBinaryNames.EnsureExactly) || !exhaust[4].Value.IsZero
                || shape[19].Operands[0].Value != badLabel.Value || restoreFailure[0] != size || restoreFailure[1] != x0
                || bad != x0 || shape[22].Operands[0].Value != jump.Value
                || !IsBif(add, ErlangOperators.Plus, 2) || add[3] != extracted || add[4] != saved || add[5] != x0)
            {
                continue;
            }

            int functionStart = start - 1;
            while (functionStart > 0 && body[functionStart].Definition.Opcode != FunctionInfo)
            {
                functionStart--;
            }
            int functionEnd = start + shape.Length;
            while (functionEnd < body.Count && body[functionEnd].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                functionEnd++;
            }
            while (functionEnd > start + shape.Length
                && body[functionEnd - 1].Definition.Opcode is Label or Line)
            {
                functionEnd--;
            }

            labelReferences ??= body.SelectMany((instruction, index) => instruction.Operands
                    .Where(operand => operand.Kind == BeamOperandParameters.Label)
                    .Select(operand => (Label: (int)operand.Value, Index: index)))
                .ToLookup(item => item.Label, item => item.Index);
            if (labelReferences[negativeBlockLabel].Distinct().ToArray() is not [var positive]
                || positive <= functionStart || positive >= start - 1 || body[positive].Definition.Opcode != IsLess
                || body[positive].Operands[1] != zero || body[positive].Operands[2] != x0
                || !labelReferences[(int)badLabel.Value].Order().SequenceEqual([start + 16, start + 17])
                || !labelReferences[(int)jump.Value].SequenceEqual([start + 18])
                || body.Skip(functionStart).Take(functionEnd - functionStart).Any(instruction => instruction.Definition.Opcode
                    is TryEnter or CatchEnter or Wait or WaitTimeout)
                || !HasClosedTail(start + shape.Length, functionEnd, frame))
            {
                continue;
            }

            int number = GetOperandTypeIndex((NumberTypeBits, null, 0, null));
            long addition = (long)width[4].Value;
            int widthType = GetOperandTypeIndex((IntegerTypeBits, addition, MaximumBitSize + addition, null));
            int extractedType = GetOperandTypeIndex((IntegerTypeBits, 0, null, null));
            result.Write(code.Span[copied..shape[0].Offset]);

            // Only the original numeric term crosses the ordinary call.
            Op(
                IsLess,
                lesser[0],
                x0 with { TypeIndex = number },
                zero
            );
            Copy(start + 4);
            Op(
                GcBifOne,
                negate[0],
                U(1),
                negate[2],
                x0 with { TypeIndex = number },
                x1
            );
            Op(Allocate, U(1), U(2));
            Op(Move, x0, y0);
            Op(Move, x1, x0);
            Copy(start + 6);
            Copy(start + 7);
            Copy(start + 9);
            Op(
                GcBifTwo,
                width[0],
                U(1),
                width[2],
                x0 with { TypeIndex = sizeType },
                width[4],
                x1
            );
            Copy(start + 12);
            result.WriteByte(CreateBinary);
            for (int index = 0; index < construct.Count; index++)
            {
                Write(index switch
                {
                    2 => U(2),
                    _ when index == construct.Count - 1 => x0 with { TypeIndex = sizeType },
                    _ => construct[index]
                });
            }

            var context = new BeamOperand(BeamOperandParameters.X, 2);
            Op(
                StartBinaryMatchKnown,
                match[0],
                U(2),
                x0,
                context
            );
            Op(
                GetBinaryInteger,
                badLabel,
                context with { TypeIndex = contextType },
                U(3),
                x1 with { TypeIndex = widthType },
                extract[4],
                extract[5],
                x1
            );
            Op(
                BinaryMatch,
                badLabel,
                context,
                exhaust[2],
                exhaust[3],
                exhaust[4]
            );
            Copy(start + 23);
            Op(
                GcBifTwo,
                add[0],
                U(2),
                add[2],
                x1 with { TypeIndex = extractedType },
                y0 with { TypeIndex = number },
                x0
            );
            Op(InitYRegisters, exhaust[2] with { Value = 1 }, y0);
            Copy(start + 25);
            for (int index = start + shape.Length; index < functionEnd; index++)
            {
                if (body[index].Definition.Opcode == Deallocate)
                {
                    Op(Deallocate, U(1));
                }
                else
                {
                    Copy(index);
                }
            }

            // The failed extraction and exhaustion test both retain X0 unchanged.
            Copy(start + 19);
            Copy(start + 12);
            Copy(start + 21);
            lineInstructions++;
            copied = body[functionEnd].Offset;
            start = functionEnd - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        result.Write(code.Span[copied..]);

        return result.ToArray();

        bool HasClosedTail(int first, int end, int priorFrame)
        {
            if (first >= end || body[end - 1].Definition.Opcode is not (Return or BadMatch))
            {
                return false;
            }

            var tailLabels = body.Skip(first).Take(end - first)
                .Where(instruction => instruction.Definition.Opcode == Label)
                .Select(instruction => instruction.Operands[0].Value)
                .ToHashSet();
            for (int index = first; index < end; index++)
            {
                var instruction = body[index];
                if (instruction.Definition.Opcode == Deallocate)
                {
                    if (instruction.Operands[0].Value != priorFrame)
                    {
                        return false;
                    }

                    continue;
                }

                if (instruction.Definition.Opcode == Label && labelReferences![(int)instruction.Operands[0].Value]
                    .Any(reference => reference < first || reference >= end))
                {
                    int next = index + 1;
                    while (next < end && body[next].Definition.Opcode == Line)
                    {
                        next++;
                    }
                    if (next >= end || body[next].Definition.Opcode != BadMatch || body[next].Operands[0] != x0)
                    {
                        return false;
                    }
                }
                if (instruction.Definition.Opcode is not
                    (Line or Label or StartBinaryMatch or BinaryMatch or GcBifOne or GcBifTwo
                    or ExactEqualTest or GetBinaryTail or CreateBinary or Return or BadMatch)
                    || instruction.Definition.Opcode == Return
                    && (index == first || body[index - 1].Definition.Opcode != Deallocate)
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                        || operand.Kind == BeamOperandParameters.Label && !tailLabels.Contains(operand.Value)))
                {
                    return false;
                }
            }

            return true;
        }

        bool IsEncodingCall(BeamInstruction instruction) =>
            instruction.Operands[0].Value.IsOne
            && IsImport(
                instruction.Operands[1],
                BinaryModuleNames.Module,
                BinaryModuleNames.EncodeUnsigned,
                1
            );

        bool IsBif(IReadOnlyList<BeamOperand> operands, string function, int arity) =>
            operands[0].Kind == BeamOperandParameters.Failure
            && IsImport(
                operands[2],
                ErlangModuleNames.Module,
                function,
                arity
            );

        bool IsImport(
            BeamOperand operand,
            string module,
            string function,
            int arity
        )
        {
            int index = (int)operand.Value;
            var (owner, name, count) = callees[index];

            return names[owner - 1] == module && names[name - 1] == function && count == arity;
        }

        bool IsAtom(BeamOperand operand, string name) =>
            operand.Kind == BeamOperandParameters.Atom && names[(int)operand.Value - 1] == name;

        void Copy(int index) => result.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void Op(byte opcode, params BeamOperand[] operands)
        {
            result.WriteByte(opcode);
            foreach (BeamOperand operand in operands)
            {
                Write(operand);
            }
        }

        void Write(BeamOperand operand)
        {
            if (operand.Kind == BeamOperandParameters.Count)
            {
                result.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(result, BeamWire.UnsignedTag, operand.Value);

                return;
            }

            if (operand.Kind == BeamOperandParameters.LiteralValue)
            {
                result.WriteByte(ExtendedLiteralOperand);
            }
            if (operand.TypeIndex is int type)
            {
                result.WriteByte(ExtendedTypedRegisterOperand);
            }
            int tag = operand.Kind switch
            {
                BeamOperandParameters.X => XRegisterTag,
                BeamOperandParameters.Y => YRegisterTag,
                BeamOperandParameters.Atom or BeamOperandParameters.Nil => AtomTag,
                BeamOperandParameters.Label or BeamOperandParameters.Failure => LabelTag,
                BeamOperandParameters.Integer => BeamWire.IntegerTag,
                _ => BeamWire.UnsignedTag
            };
            BeamCompactWriter.Write(result, tag, operand.Value);
            if (operand.TypeIndex is int index)
            {
                BeamCompactWriter.Write(result, BeamWire.UnsignedTag, index);
            }
        }

        static bool SameRegister(BeamOperand left, BeamOperand right) =>
            left.Kind == right.Kind && left.Value == right.Value;

        static bool IsY(BeamOperand operand) =>
            operand.Kind == BeamOperandParameters.Y && operand.TypeIndex is null;

        static BeamOperand U(int value) => new(BeamOperandParameters.Unsigned, value);
    }
}
