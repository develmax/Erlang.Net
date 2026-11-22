using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SinkCheckedRecordConstructionFields(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo)
            || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();

        return SinkCheckedRecordConstructionFields(
            code,
            body,
            instruction => IsLeafArithmeticGcBif(instruction, names, imported)
        );
    }

    private static byte[]? SinkCheckedRecordConstructionFields(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Func<BeamInstruction, bool> isArithmetic
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        bool unprotected = false;
        for (int first = 0; first + 6 < body.Count; first++)
        {
            if (body[first].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, first + 1);
                continue;
            }

            if (!unprotected
                || !ReadDeferredRecordField(
                    body[first],
                    out var outer,
                    out var outerField,
                    out var outerResult
                )
                || !ReadDeferredRecordField(
                    body[first + 1],
                    out var inner,
                    out var innerField,
                    out var innerResult
                ))
            {
                continue;
            }

            int arithmetic = first + 2;
            while (arithmetic < body.Count && body[arithmetic].Definition.Opcode == Line)
            {
                arithmetic++;
            }

            if (arithmetic == first + 2 || arithmetic + 3 >= body.Count
                || body[arithmetic] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [{ Kind: BeamOperandParameters.Failure, Value.IsZero: true }, var live, _, var left, var right, var result]
                } || !isArithmetic(body[arithmetic])
                || !IsBareX(result) || result.Value >= FirstReservedXRegister
                || result.Value != live.Value || outerResult.Value + 2 != live.Value || innerResult.Value + 1 != live.Value
                || outer.Value >= outerResult.Value || inner.Value >= outerResult.Value || outer == inner
                || !ReadBeforeFields(left, outerResult) || !ReadBeforeFields(right, outerResult)
                || body[arithmetic + 1] is not { Definition.Opcode: TestHeap, Operands: [_, var heapLive] }
                || heapLive.Value != live.Value + 1)
            {
                continue;
            }

            int end = arithmetic + 2;
            while (end < body.Count && body[end].Definition.Opcode is (Move or PutTuple or PutList or UpdateRecord))
            {
                end++;
            }

            if (end == arithmetic + 2 || end >= body.Count || body[end].Definition.Opcode != Return
                || body[end - 1].Definition.Opcode != UpdateRecord
                || !(flow ??= new BeamProjectionFlow(body)).ProvesRecordConstructionField(first, outer, outerField)
                || !flow!.ProvesRecordConstructionField(first, inner, innerField))
            {
                continue;
            }

            // Only the two projected fields leave the arithmetic root set.
            // The arithmetic result takes the first free root. The later
            // child projection precedes the outer projection in the use block.
            using var replacement = new MemoryStream();
            int root = (int)outerResult.Value;
            var registers = new Dictionary<int, int> { [root] = root + 2, [root + 2] = root };
            if (!WriteDeferredRecordInstruction(
                replacement,
                code,
                body,
                arithmetic,
                1,
                root,
                registers
            )
                || !WriteDeferredRecordInstruction(
                    replacement,
                    code,
                    body,
                    arithmetic + 1,
                    1,
                    root + 1,
                    registers
                )
                || !WriteDeferredRecordInstruction(
                    replacement,
                    code,
                    body,
                    first + 1,
                    -1,
                    0,
                    registers
                )
                || !WriteDeferredRecordInstruction(
                    replacement,
                    code,
                    body,
                    first,
                    -1,
                    0,
                    registers
                ))
            {
                continue;
            }

            bool complete = true;
            for (int index = arithmetic + 2; index < end; index++)
            {
                if (!WriteDeferredRecordInstruction(
                    replacement,
                    code,
                    body,
                    index,
                    -1,
                    0,
                    registers
                ))
                {
                    complete = false;
                    break;
                }
            }

            if (!complete)
            {
                continue;
            }

            output.Write(code.Span[copied..body[first].Offset]);
            output.Write(code.Span[body[first + 2].Offset..body[arithmetic].Offset]);
            replacement.WriteTo(output);
            copied = body[end].Offset;
            first = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ReadDeferredRecordField(
        BeamInstruction instruction,
        out BeamOperand source,
        out BeamOperand field,
        out BeamOperand result
    )
    {
        if (instruction is { Definition.Opcode: GetTupleElement, Operands: [var tuple, var index, var destination] }
            && IsBareX(tuple) && IsBareX(destination))
        {
            source = tuple;
            field = index;
            result = destination;

            return true;
        }

        source = field = result = default!;

        return false;
    }

    private static bool ReadBeforeFields(BeamOperand operand, BeamOperand firstField) =>
        operand.Kind == BeamOperandParameters.X && operand.Value < firstField.Value
        || IsNonRegisterValue(operand);

    private static bool WriteDeferredRecordInstruction(
        Stream output,
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int index,
        int liveOperand,
        int live,
        IReadOnlyDictionary<int, int> registers
    )
    {
        BeamInstruction instruction = body[index];
        int start = instruction.Offset + 1;
        int end = body[index + 1].Offset;
        var cursor = new BeamCursor(code[start..end]);
        output.WriteByte(instruction.Definition.Opcode);
        int word = 0;
        while (start + cursor.Position < end)
        {
            int offset = cursor.Position;
            var compact = BeamCompactReader.Read(cursor);
            if (word == liveOperand)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
            }
            else if (compact.WireTag == XRegisterTag && registers.TryGetValue((int)compact.Value, out int register))
            {
                BeamCompactWriter.Write(output, XRegisterTag, register);
            }
            else
            {
                byte prefix = code.Span[start + offset];
                if (compact.WireTag == (ExtendedListOperand & BeamWire.TagMask) && prefix is not
                    (ExtendedListOperand or ExtendedLiteralOperand or ExtendedTypedRegisterOperand))
                {
                    return false;
                }

                output.Write(code.Span[(start + offset)..(start + cursor.Position)]);
            }

            word++;
        }

        return true;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool ProvesRecordConstructionField(int use, BeamOperand tuple, BeamOperand field)
        {
            int current = use;
            while (predecessors[current] is [var previous] && previous + 1 == current)
            {
                var instruction = body[previous];
                var operands = instruction.Operands;
                if (instruction.Definition.Opcode is IsTaggedTuple or TestArity
                    && SameProjectionRegister(operands[1], tuple))
                {
                    return field.Value < operands[2].Value;
                }

                bool keepsTuple = instruction.Definition.Opcode switch
                {
                    Line or IsTuple or TestArity or IsTaggedTuple or IsNonemptyList => true,
                    GetTupleElement => !SameProjectionRegister(operands[2], tuple),
                    GetList => !SameProjectionRegister(operands[1], tuple)
                        && !SameProjectionRegister(operands[2], tuple),
                    _ => false
                };
                if (!keepsTuple)
                {
                    return false;
                }

                current = previous;
            }

            return false;
        }
    }
}
