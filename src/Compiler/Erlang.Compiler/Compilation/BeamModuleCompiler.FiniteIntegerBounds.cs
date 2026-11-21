using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateFiniteIntegerBounds(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries
    )
    {
        if (!SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo))
        {
            return null;
        }

        var atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var operations = imports.OrderBy(entry => entry.Value)
            .Select(entry => entry.Key)
            .Select(entry => entry.Arity == 2 && atomNames[entry.Module - 1] == ErlangModuleNames.Module
                ? atomNames[entry.Function - 1] : null)
            .ToArray();
        if (!operations.Any(operation => operation is ErlangOperators.BitwiseAnd or ErlangOperators.ShiftLeft
            or ErlangOperators.Plus))
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

        var localMasks = ReadLocalMaskResults(functions, localEntries);
        var facts = new Dictionary<BeamOperand, (long Lower, long Upper)>();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        foreach (BeamInstruction instruction in body)
        {
            var operands = instruction.Operands;
            switch (instruction.Definition.Opcode)
            {
                case Line:
                    break;
                case InitYRegisters:
                    foreach (var initialized in operands.Skip(1))
                    {
                        facts.Remove(Untyped(initialized));
                    }

                    break;
                case TestHeap:
                    ClearX((int)operands[1].Value);
                    break;
                case PutTuple:
                    facts.Remove(Untyped(operands[0]));
                    break;
                case Move:
                    var destination = Untyped(operands[1]);
                    if (Lookup(operands[0]) is { } moved)
                    {
                        facts[destination] = moved;
                    }
                    else
                    {
                        facts.Remove(destination);
                    }

                    break;
                case LocalCall or ExternalCall:
                    ClearX(0);
                    if (instruction.Definition.Opcode == LocalCall
                        && localMasks.TryGetValue((int)operands[1].Value, out long mask))
                    {
                        facts[x0] = (0, mask);
                    }

                    break;
                case GcBifTwo when operands[0].Kind == BeamOperandParameters.Failure
                    && operations[(int)operands[2].Value] is { } operation:
                    if (!IsRangeConsumer(operation))
                    {
                        facts.Clear();
                        break;
                    }

                    var left = Lookup(operands[3]);
                    var right = Lookup(operands[4]);
                    Annotate(
                        instruction,
                        3,
                        left
                    );
                    Annotate(
                        instruction,
                        4,
                        right
                    );
                    ClearX((int)operands[1].Value);
                    var result = Untyped(operands[5]);
                    facts.Remove(result);
                    if (InferBitwiseResult(
                        operation,
                        left,
                        right,
                        operands[3],
                        operands[4]
                    ) is { } range)
                    {
                        facts[result] = range;
                    }

                    break;
                case IsLess or IsGreaterOrEqual or NumericEqualTest or NumericNotEqualTest or ExactEqualTest or ExactNotEqualTest:
                    Annotate(
                        instruction,
                        1,
                        Lookup(operands[1])
                    );
                    Annotate(
                        instruction,
                        2,
                        Lookup(operands[2])
                    );
                    // The comparison does not overwrite its inputs. Labels
                    // still end the proof before any other incoming route.
                    break;
                default:
                    facts.Clear();
                    break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        (long Lower, long Upper)? Lookup(BeamOperand operand)
        {
            if (operand.TypeIndex is int type && typeBits[type] is
                (IntegerTypeBits, long minimum, long maximum, null))
            {
                return (minimum, maximum);
            }

            return facts.TryGetValue(Untyped(operand), out var known) ? known : null;
        }

        void ClearX(int live)
        {
            foreach (var register in facts.Keys.Where(register => register.Kind == BeamOperandParameters.X
                && register.Value >= live).ToArray())
            {
                facts.Remove(register);
            }
        }

        void Annotate(BeamInstruction instruction, int index, (long Lower, long Upper)? range)
        {
            BeamOperand operand = instruction.Operands[index];
            if (range is not { } known || operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y)
                || operand.TypeIndex is int oldType && typeBits[oldType] is not (IntegerTypeBits, null, null, null))
            {
                return;
            }

            // Preserve the original operand encoding and replace only its type operand.
            int start = instruction.Offset + 1;
            var cursor = new BeamCursor(code[start..]);
            for (int position = 0; position < index; position++)
            {
                if (BeamCompactReader.Read(cursor).WireTag == BeamOperandParameters.ExtendedTag)
                {
                    _ = BeamCompactReader.Read(cursor);
                    if (instruction.Operands[position].TypeIndex.HasValue)
                    {
                        _ = BeamCompactReader.Read(cursor);
                    }
                }
            }

            int offset = start + cursor.Position;
            output.Write(code.Span[copied..offset]);
            if (operand.TypeIndex.HasValue)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            int register = start + cursor.Position;
            _ = BeamCompactReader.Read(cursor);
            output.WriteByte(ExtendedTypedRegisterOperand);
            output.Write(code.Span[register..(start + cursor.Position)]);
            if (operand.TypeIndex.HasValue)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            int type = GetOperandTypeIndex((IntegerTypeBits, known.Lower, known.Upper, null));
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            copied = start + cursor.Position;
        }

        static BeamOperand Untyped(BeamOperand operand) =>
            operand.TypeIndex is null ? operand : operand with { TypeIndex = null };
    }

    private Dictionary<int, long> ReadLocalMaskResults(
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries
    )
    {
        var masks = new Dictionary<int, long>();
        if (ModuleOptimizationDisabled || OnLoadFunction is not null)
        {
            return masks;
        }

        foreach (FunctionDefinition function in functions)
        {
            var signature = (function.Name, function.Arity);
            if (function.IsInlineFailure || IsDeclaredNative(signature)
                || function.Clauses is not [{ Guard: null, Body: var value }]
                || !localEntries.TryGetValue(signature, out int label)
                || (label <= 0 && !deferredEntries.TryGetValue(label, out label)))
            {
                continue;
            }

            while (true)
            {
                if (value is Expr.Sequence { Items.Count: > 0 } sequence)
                {
                    value = sequence.Items[^1];
                }
                else if (value is Expr.Block block)
                {
                    value = block.Body;
                }
                else
                {
                    break;
                }
            }

            if (value is not Expr.Binary { Operator: ErlangOperators.BitwiseAnd, Left: var left, Right: var right })
            {
                continue;
            }

            Integer? mask = (left, right) switch
            {
                (Expr.Literal { Value: Integer }, Expr.Literal { Value: Integer }) => null,
                (Expr.Literal { Value: Integer constant }, _) => constant,
                (_, Expr.Literal { Value: Integer constant }) => constant,
                _ => null
            };
            if (mask is not null && mask.Value > 0 && mask.Value <= BeamBinaryExtractRangeLimits.MaximumEncodedInteger)
            {
                masks[MapEmittedLabel(label)] = (long)mask.Value;
            }
        }

        return masks;
    }

    private static bool IsRangeConsumer(string operation) =>
        operation is ErlangOperators.BitwiseAnd or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor
            or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight or ErlangOperators.IntegerDivide
            or ErlangOperators.Remainder or ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply;

    private static (long Lower, long Upper)? InferBitwiseResult(
        string operation,
        (long Lower, long Upper)? left,
        (long Lower, long Upper)? right,
        BeamOperand leftOperand,
        BeamOperand rightOperand
    )
    {
        // beam_bounds treats a nonnegative mask as [0, mask] on success.
        // When the other bound is smaller, a low-bit mask is the identity.
        if (operation == ErlangOperators.BitwiseAnd)
        {
            bool literalOnLeft = leftOperand.Kind == BeamOperandParameters.Integer;
            var constant = literalOnLeft ? leftOperand : rightOperand;
            var input = literalOnLeft ? right : left;
            if (constant.Kind != BeamOperandParameters.Integer || constant.Value <= 0
                || constant.Value > BeamBinaryExtractRangeLimits.MaximumEncodedInteger
                || input is { Lower: not 0 })
            {
                return null;
            }

            long mask = (long)constant.Value;
            if (input is null || input.Value.Upper >= mask)
            {
                return (0, mask);
            }

            return (mask & (mask + 1)) == 0 ? (0, input.Value.Upper) : null;
        }

        if (left is not { } value || rightOperand.Kind != BeamOperandParameters.Integer || rightOperand.Value < 0)
        {
            return null;
        }

        long maximum = BeamBinaryExtractRangeLimits.MaximumEncodedInteger;
        if (operation == ErlangOperators.ShiftLeft && value.Lower == 0
            && rightOperand.Value <= BeamTypeWire.NativeSmallMagnitudeBits64)
        {
            int count = (int)rightOperand.Value;

            return value.Upper <= maximum >> count ? (0, value.Upper << count) : null;
        }

        if (operation == ErlangOperators.Plus && value.Lower >= 0 && rightOperand.Value <= maximum - value.Upper)
        {
            long constant = (long)rightOperand.Value;

            return (value.Lower + constant, value.Upper + constant);
        }

        return null;
    }
}
