using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateAdjacentNumericResults(
        ReadOnlyMemory<byte> code,
        int labelCount,
        Dictionary<int, int> localResultEntries
    )
    {
        if (!SsaTypeInferenceEnabled)
        {
            return null;
        }

        var atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var producers = new HashSet<int>();
        var consumers = new HashSet<int>();
        var sizeProducers = new HashSet<int>();
        var mathFloatCalls = new HashSet<int>();
        var integerConversions = new HashSet<int>();
        int tupleSize = -1;
        var comparisons = new HashSet<int>();
        var importsByIndex = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        for (int index = 0; index < importsByIndex.Length; index++)
        {
            var (module, function, arity) = importsByIndex[index];
            string name = atomNames[function - 1];
            if (atomNames[module - 1] == MathNativeNames.Module && ReturnsMathFloat(name, arity))
            {
                mathFloatCalls.Add(index);
            }

            if (atomNames[module - 1] != ErlangModuleNames.Module)
            {
                continue;
            }

            if (arity == 1 && name is AutoImportNames.Trunc or AutoImportNames.Round or AutoImportNames.Ceil or AutoImportNames.Floor)
            {
                integerConversions.Add(index);
            }

            if (arity == 1 && name is AutoImportNames.ByteSize or AutoImportNames.BitSize or AutoImportNames.MapSize)
            {
                sizeProducers.Add(index);
            }

            if (arity == 1 && name == AutoImportNames.TupleSize)
            {
                tupleSize = index;
            }
            if (arity != 2)
            {
                continue;
            }

            switch (name)
            {
                case ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply:
                    producers.Add(index);
                    break;
                case ErlangOperators.BitwiseAnd or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor
                    or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight
                    or ErlangOperators.IntegerDivide or ErlangOperators.Remainder:
                    consumers.Add(index);
                    break;
                case ErlangOperators.Less or ErlangOperators.LessOrEqual
                    or ErlangOperators.Greater or ErlangOperators.GreaterOrEqual
                    or ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual
                    or ErlangOperators.ExactEqual or ErlangOperators.ExactNotEqual:
                    comparisons.Add(index);
                    break;
            }
        }

        bool numericProducer = producers.Count != 0 && consumers.Count != 0;
        bool localInteger = (producers.Count != 0 || consumers.Count != 0)
            && localResultEntries.ContainsValue(IntegerTypeBits);
        bool localAtomOrTuple = localResultEntries.ContainsValue(LocalAtomOrTupleType)
            && code.Span.Contains(IsTuple);
        bool numericRecordFields = producers.Count != 0 && code.Span.Contains(UpdateRecord);
        bool sizeRange = sizeProducers.Count != 0 && code.Span.Contains(GcBifOne);
        bool tupleSizeRange = tupleSize >= 0 && code.Span.Contains(BifOne);
        bool mathResult = mathFloatCalls.Count != 0 && integerConversions.Count != 0 && code.Span.Contains(ExternalCall);
        if (!numericProducer && !localInteger && !localAtomOrTuple && !numericRecordFields && !sizeRange && !tupleSizeRange
            && !mathResult)
        {
            return null;
        }

        var header = new BeamCodeHeader(
            BeamWire.FormatVersion,
            highestOpcode,
            labelCount,
            exports.Count + locals.Count,
            code
        );
        IReadOnlyList<BeamInstruction> body;
        try
        {
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int producer = -1;
        bool opaqueX0 = false;
        int localResultType = 0;
        var localResult = new BeamOperand(BeamOperandParameters.X, 0);
        BeamOperand? sizeResult = null;
        long? sizeLowerBound = null;
        BeamOperand? tupleSizeResult = null;
        BeamOperand? mathValue = null;
        int mathType = 0;
        HashSet<BeamOperand>? recordValues = numericRecordFields ? [] : null;
        Dictionary<int, (BeamOperand Slot, long Minimum)>? recordRanges = numericRecordFields ? [] : null;
        BeamProjectionFlow? recordFlow = null;
        BeamOperand? integerComparisonResult = null;
        BeamOperand? recordIntegerResult = null;
        HashSet<BeamOperand>? savedComparisonNumbers = null;
        for (int index = 0; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            int before = copied;
            // A source line is neutral. Every other instruction ends the immediately
            // preceding definition, including labels with additional predecessors.
            bool convertsMathFloat = mathType == (NumberTypeBits & ~IntegerTypeBits) && mathValue is not null
                && instruction is { Definition.Opcode: GcBifOne, Operands: [var conversionFailure, _, var conversion, var arg, _] }
                && conversionFailure.Value.IsZero && integerConversions.Contains((int)conversion.Value)
                && SameProjectionRegister(arg, mathValue);
            if (convertsMathFloat && instruction.Operands[3].TypeIndex is null)
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    instruction,
                    3,
                    mathType
                );
            }
            else if (mathType == IntegerTypeBits && mathValue is not null
                && (IsOrdinaryBinaryBif(instruction, producers) || IsOrdinaryBinaryBif(instruction, consumers)))
            {
                copied = AnnotateNumericUse(
                    code,
                    output,
                    copied,
                    instruction,
                    mathValue,
                    mathType
                );
            }
            else if (integerComparisonResult is not null && instruction.Definition.Opcode == NumericEqualTest)
            {
                copied = AnnotateIntegerResultComparison(
                    code,
                    output,
                    copied,
                    instruction,
                    integerComparisonResult,
                    savedComparisonNumbers
                );
            }
            else if (tupleSizeResult is not null && ReadTupleSizeComparison(instruction, tupleSizeResult) is int tupleSizeUse)
            {
                copied = AnnotateTupleSizeComparison(
                    code,
                    output,
                    copied,
                    instruction,
                    tupleSizeUse,
                    body[index + 1].Offset
                );
            }
            else if (sizeResult is not null && ReadSizeConsumer(instruction, sizeResult) is int sizeUse)
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    instruction,
                    sizeUse,
                    IntegerTypeBits,
                    minimum: sizeLowerBound ?? 0,
                    maximum: OtpSizeUpperBound
                );
            }
            else if (producer >= 0 && IsOrdinaryBinaryBif(instruction, consumers))
            {
                copied = AnnotateNumericUse(
                    code,
                    output,
                    copied,
                    instruction,
                    body[producer].Operands[^1],
                    NumberTypeBits
                );
            }
            else if (localResultType == IntegerTypeBits && (IsOrdinaryBinaryBif(instruction, consumers)
                || IsOrdinaryBinaryBif(instruction, producers)))
            {
                copied = AnnotateNumericUse(
                    code,
                    output,
                    copied,
                    instruction,
                    localResult,
                    IntegerTypeBits
                );
            }
            else if (localResultType == LocalAtomOrTupleType
                && instruction is { Definition.Opcode: IsTuple, Operands: [_, var input] }
                && IsBareX0(input))
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    instruction,
                    1,
                    LocalAtomOrTupleType
                );
            }

            // Pinned beam_call_types gives a float on successful math calls.
            // It then gives an unbounded integer on the rounding BIF's success.
            // Follow copies in that fallthrough block. A label ends both facts.
            if (mathResult && instruction is { Definition.Opcode: ExternalCall, Operands: [_, var mathImport] }
                && mathFloatCalls.Contains((int)mathImport.Value))
            {
                mathValue = localResult;
                mathType = NumberTypeBits & ~IntegerTypeBits;
            }
            else if (convertsMathFloat)
            {
                mathValue = instruction.Operands[^1];
                mathType = IntegerTypeBits;
            }
            else if (mathValue is not null
                && instruction is { Definition.Opcode: Move, Operands: [var mathSource, var mathDestination] }
                && SameProjectionRegister(mathSource, mathValue))
            {
                mathValue = mathDestination;
            }
            else
            {
                mathValue = null;
                mathType = 0;
            }

            // The local result and a literal integer make this affine operation integer on success.
            // A normal local call invalidates X facts, but preserves numeric saves in its caller's frame.
            integerComparisonResult = localResultType == IntegerTypeBits
                && IsOrdinaryBinaryBif(instruction, producers)
                && (IsBareX0(instruction.Operands[3]) && instruction.Operands[4].Kind == BeamOperandParameters.Integer
                    || IsBareX0(instruction.Operands[4]) && instruction.Operands[3].Kind == BeamOperandParameters.Integer)
                ? instruction.Operands[^1] : null;
            if (instruction is { Definition.Opcode: LocalCall, Operands: [_, var callee] }
                && localResultEntries.GetValueOrDefault((int)callee.Value) == IntegerTypeBits)
            {
                savedComparisonNumbers = recordValues?.Where(value => value.Kind == BeamOperandParameters.Y).ToHashSet();
            }
            else if (integerComparisonResult is null)
            {
                savedComparisonNumbers = null;
            }

            if (recordValues is not null)
            {
                if (instruction is
                    {
                        Definition.Opcode: IsLess,
                        Operands: [var failure, { Kind: BeamOperandParameters.Y } slot,
                        { Kind: BeamOperandParameters.Integer } bound]
                    }
                    && recordValues.Contains(slot) && bound.Value >= long.MinValue && bound.Value <= long.MaxValue
                    && (recordFlow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(index, (int)failure.Value, out int target)
                    && ReadBoundedRecordUpdate(body, target, slot) is int update)
                {
                    recordRanges!.Add(update, (slot, (long)bound.Value));
                    copied = AnnotateRegisterInput(
                        code,
                        output,
                        copied,
                        instruction,
                        1,
                        NumberTypeBits
                    );
                }

                if (instruction.Definition.Opcode == UpdateRecord)
                {
                    copied = AnnotateNumericRecordFields(
                        code,
                        output,
                        copied,
                        instruction,
                        recordValues,
                        recordRanges!.TryGetValue(index, out var range) ? range : null,
                        recordIntegerResult
                    );
                    recordValues.Clear();
                }
                else
                {
                    AdvanceNumericRecordValues(
                        instruction,
                        recordValues,
                        producers,
                        inputWasTyped: before != copied
                    );
                }
            }

            // A checked local integer result can still be read in a record
            // replacement after nonclobbering rooted instructions.
            if (numericRecordFields)
            {
                recordIntegerResult = instruction switch
                {
                    { Definition.Opcode: LocalCall, Operands: [_, var target] }
                        when localResultEntries.GetValueOrDefault((int)target.Value) == IntegerTypeBits => localResult,
                    { Definition.Opcode: GcBifTwo, Operands: [var fail, var live, .., var destination] }
                        when fail.Value.IsZero && live.Value > 0 && !SameProjectionRegister(destination, localResult)
                        => recordIntegerResult,
                    { Definition.Opcode: TestHeap, Operands: [_, var live] } when live.Value > 0 => recordIntegerResult,
                    _ => null
                };
            }

            // Local result facts are carried only across neutral source lines.
            localResultType = instruction is { Definition.Opcode: LocalCall, Operands: [_, var entry] }
                ? localResultEntries.GetValueOrDefault((int)entry.Value) : 0;
            producer = opaqueX0 && IsOrdinaryBinaryBif(instruction, producers)
                && HasUnknownNumericInput(instruction)
                && (IsBareX0(instruction.Operands[3]) || IsBareX0(instruction.Operands[4])) ? index : -1;
            opaqueX0 = producer >= 0
                || HasOpaqueNumericInputResult(
                    instruction,
                    atomNames,
                    importsByIndex
                );
            // beam_call_types gives these size BIFs the same integer range on
            // success. A line or copy keeps that result in the fallthrough block.
            sizeLowerBound = sizeResult is not null && index + 1 < body.Count
                ? ReadInclusiveSizeLowerBound(instruction, body[index + 1], sizeResult) : null;
            sizeResult = instruction switch
            {
                {
                    Definition.Opcode: GcBifOne,
                    Operands: [{ Kind: BeamOperandParameters.Failure }, _, var import,
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y }, var measured]
                }
                    when sizeProducers.Contains((int)import.Value) => measured,
                { Definition.Opcode: Move, Operands: [var from, var to] }
                    when sizeResult is not null && SameProjectionRegister(sizeResult, from) => to,
                _ => sizeLowerBound.HasValue ? sizeResult : null
            };
            // A successful tuple_size BIF defines its result only on this fallthrough path.
            // A label or any other instruction that may replace it ends this fact.
            tupleSizeResult = instruction switch
            {
                { Definition.Opcode: BifOne, Operands: [_, var import, _, var arity] }
                    when import.Value == tupleSize => arity,
                { Definition.Opcode: Move, Operands: [var from, var to] }
                    when tupleSizeResult is not null && SameProjectionRegister(tupleSizeResult, from) => to,
                _ => null
            };
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int? ReadSizeConsumer(BeamInstruction instruction, BeamOperand measured)
        {
            var operands = instruction.Operands;
            int first = instruction.Definition.Opcode switch
            {
                IsLess or IsGreaterOrEqual or NumericEqualTest or NumericNotEqualTest
                    or ExactEqualTest or ExactNotEqualTest => 1,
                BifTwo when operands[0].Kind == BeamOperandParameters.Failure
                    && comparisons.Contains((int)operands[1].Value) => 2,
                GcBifTwo when IsOrdinaryBinaryBif(instruction, producers)
                    || IsOrdinaryBinaryBif(instruction, consumers) => 3,
                _ => -1
            };
            if (first < 0)
            {
                return null;
            }

            bool left = SameProjectionRegister(operands[first], measured);
            bool right = SameProjectionRegister(operands[first + 1], measured);
            int index = left ? first : first + 1;

            return left != right && operands[index].TypeIndex is null ? index : null;
        }
    }

    private static bool ReturnsMathFloat(string function, int arity) => arity switch
    {
        0 => function == MathNativeNames.Pi,
        1 => MathNativeNames.UnaryFunctions.Contains(function),
        2 => MathNativeNames.BinaryFunctions.Contains(function),
        _ => false
    };

    private static long? ReadInclusiveSizeLowerBound(
        BeamInstruction lowerTest,
        BeamInstruction upperTest,
        BeamOperand size
    )
    {
        // Both guards consume the same size version. The upper test is only
        // reached after success of the lower test, without a branch join.
        if (lowerTest is
            {
                Definition.Opcode: IsGreaterOrEqual,
                Operands: [var failure, var lowerInput, { Kind: BeamOperandParameters.Integer } lower]
            }
            && upperTest is
            {
                Definition.Opcode: IsGreaterOrEqual,
                Operands: [var upperFailure, { Kind: BeamOperandParameters.Integer }, var upperInput]
            }
            && failure == upperFailure && SameProjectionRegister(lowerInput, size)
            && SameProjectionRegister(upperInput, size)
            && lower.Value > 0 && lower.Value <= OtpSizeUpperBound)
        {
            return (long)lower.Value;
        }

        return null;
    }

    private int AnnotateIntegerResultComparison(
        ReadOnlyMemory<byte> code,
        Stream output,
        int copied,
        BeamInstruction comparison,
        BeamOperand integer,
        HashSet<BeamOperand>? savedNumbers
    )
    {
        if (comparison.Operands is not [_, var left, var right]
            || SameProjectionRegister(left, integer) == SameProjectionRegister(right, integer))
        {
            return copied;
        }

        for (int index = 1; index < comparison.Operands.Count; index++)
        {
            BeamOperand value = comparison.Operands[index];
            int type = value == integer ? IntegerTypeBits : savedNumbers?.Contains(value) == true ? NumberTypeBits : 0;
            if (type != 0)
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    comparison,
                    index,
                    type
                );
            }
        }

        return copied;
    }

    private static int? ReadTupleSizeComparison(BeamInstruction test, BeamOperand size)
    {
        if (test.Definition.Opcode is not (IsLess or IsGreaterOrEqual)
            || test.Operands is not [_, var left, var right])
        {
            return null;
        }

        if (right.Kind == BeamOperandParameters.Integer && left.TypeIndex is null && SameProjectionRegister(left, size))
        {
            return 1;
        }
        if (left.Kind == BeamOperandParameters.Integer && right.TypeIndex is null && SameProjectionRegister(right, size))
        {
            return 2;
        }

        return null;
    }

    private int AnnotateTupleSizeComparison(
        ReadOnlyMemory<byte> code,
        Stream output,
        int copied,
        BeamInstruction test,
        int measured,
        int end
    )
    {
        if (test.Definition.Opcode == IsGreaterOrEqual)
        {
            return AnnotateRegisterInput(
                code,
                output,
                copied,
                test,
                measured,
                IntegerTypeBits,
                minimum: 0,
                maximum: MaxTupleArity
            );
        }

        // beam_jump rewrites a strict comparison with a typed integer and an
        // integer literal to an inclusive comparison with the adjacent bound.
        output.Write(code.Span[copied..test.Offset]);
        output.WriteByte(IsGreaterOrEqual);
        BeamCompactWriter.Write(output, LabelTag, test.Operands[0].Value);
        var value = test.Operands[measured];
        var bound = test.Operands[3 - measured].Value;
        if (measured == 1)
        {
            BeamCompactWriter.Write(output, BeamWire.IntegerTag, bound - 1);
        }
        output.WriteByte(ExtendedTypedRegisterOperand);
        WriteRegister(output, value);
        int type = GetOperandTypeIndex((IntegerTypeBits, 0, MaxTupleArity, null));
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
        if (measured == 2)
        {
            BeamCompactWriter.Write(output, BeamWire.IntegerTag, bound + 1);
        }

        return end;
    }

    private static bool HasOpaqueNumericInputResult(
        BeamInstruction instruction,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        var operands = instruction.Operands;
        if (instruction.Definition.Opcode == ExternalCall)
        {
            var (module, function, arity) = importsByIndex[(int)operands[1].Value];

            // beam_call_types has specialized remote return rules for these modules.
            return atomNames[module - 1] switch
            {
                MathNativeNames.Module or ListModuleNames.Module or MapModuleNames.Module => false,
                ErlangModuleNames.Module => arity == 1
                    && atomNames[function - 1] is ErlangClockNames.MonotonicTime or SystemInfoBif,
                _ => true
            };
        }

        if (instruction.Definition.Opcode != BifOne || !operands[0].Value.IsZero
            || operands[2].TypeIndex is not null || !IsBareX0(operands[^1]))
        {
            return false;
        }

        var lookup = importsByIndex[(int)operands[1].Value];

        return lookup.Arity == 1 && atomNames[lookup.Module - 1] == ErlangModuleNames.Module
            && atomNames[lookup.Function - 1] is AutoImportNames.Get or AutoImportNames.Hd;
    }

    private const string SystemInfoBif = "system_info";

    private static bool IsBareX0(BeamOperand operand) =>
        operand is { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null };

    private static bool IsOrdinaryBinaryBif(BeamInstruction instruction, HashSet<int> operations) =>
        instruction.Definition.Opcode == GcBifTwo && instruction.Operands[0].Value.IsZero
        && operations.Contains((int)instruction.Operands[2].Value);

    private static bool HasUnknownNumericInput(BeamInstruction producer)
    {
        var operands = producer.Operands;

        return IsBareX0(operands[^1])
            && IsSupportedInput(operands[3]) && IsSupportedInput(operands[4])
            && (operands[3].Kind != BeamOperandParameters.Integer || operands[4].Kind != BeamOperandParameters.Integer);

        static bool IsSupportedInput(BeamOperand operand) =>
            operand.TypeIndex is null
            && operand.Kind is BeamOperandParameters.Integer or BeamOperandParameters.X or BeamOperandParameters.Y;
    }

    private int AnnotateNumericUse(
        ReadOnlyMemory<byte> code,
        Stream output,
        int copied,
        BeamInstruction consumer,
        BeamOperand result,
        int operandType
    )
    {
        var operands = consumer.Operands;
        bool first = SameProjectionRegister(operands[3], result);
        bool second = SameProjectionRegister(operands[4], result);
        if (first == second || operands[first ? 3 : 4].TypeIndex is not null)
        {
            return copied;
        }

        return AnnotateRegisterInput(
            code,
            output,
            copied,
            consumer,
            first ? 3 : 4,
            operandType
        );
    }

    private int AnnotateRegisterInput(
        ReadOnlyMemory<byte> code,
        Stream output,
        int copied,
        BeamInstruction consumer,
        int target,
        int operandType,
        long? minimum = null,
        long? maximum = null
    )
    {
        // Preserve the consumer operation and failure edge while typing its input.
        int start = consumer.Offset + 1;
        var cursor = new BeamCursor(code[start..]);
        var operands = consumer.Operands;
        for (int position = 0; position < target; position++)
        {
            if (BeamCompactReader.Read(cursor).WireTag == BeamOperandParameters.ExtendedTag)
            {
                _ = BeamCompactReader.Read(cursor);
                if (operands[position].TypeIndex.HasValue)
                {
                    _ = BeamCompactReader.Read(cursor);
                }
            }
        }

        int offset = start + cursor.Position;
        output.Write(code.Span[copied..offset]);
        output.WriteByte(ExtendedTypedRegisterOperand);
        _ = BeamCompactReader.Read(cursor);
        int end = start + cursor.Position;
        output.Write(code.Span[offset..end]);
        int type = GetOperandTypeIndex((operandType, minimum, maximum, null));
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);

        return end;
    }
}
