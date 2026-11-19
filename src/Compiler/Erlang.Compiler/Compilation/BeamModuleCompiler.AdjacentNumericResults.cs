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
        var importsByIndex = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        for (int index = 0; index < importsByIndex.Length; index++)
        {
            var (module, function, arity) = importsByIndex[index];
            if (arity != 2 || atomNames[module - 1] != ErlangModuleNames.Module)
            {
                continue;
            }

            switch (atomNames[function - 1])
            {
                case ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply:
                    producers.Add(index);
                    break;
                case ErlangOperators.BitwiseAnd or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor
                    or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight
                    or ErlangOperators.IntegerDivide or ErlangOperators.Remainder:
                    consumers.Add(index);
                    break;
            }
        }

        bool numericProducer = producers.Count != 0 && consumers.Count != 0;
        bool localInteger = (producers.Count != 0 || consumers.Count != 0)
            && localResultEntries.ContainsValue(IntegerTypeBits);
        bool localAtomOrTuple = localResultEntries.ContainsValue(LocalAtomOrTupleType)
            && code.Span.Contains(IsTuple);
        bool numericRecordFields = producers.Count != 0 && code.Span.Contains(UpdateRecord);
        if (!numericProducer && !localInteger && !localAtomOrTuple && !numericRecordFields)
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
        HashSet<BeamOperand>? recordValues = numericRecordFields ? [] : null;
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
            if (producer >= 0 && IsOrdinaryBinaryBif(instruction, consumers))
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

            if (recordValues is not null)
            {
                if (instruction.Definition.Opcode == UpdateRecord)
                {
                    copied = AnnotateNumericRecordFields(
                        code,
                        output,
                        copied,
                        instruction,
                        recordValues
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
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
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
        int operandType
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
        int type = GetOperandTypeIndex((operandType, null, null, null));
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);

        return end;
    }
}
