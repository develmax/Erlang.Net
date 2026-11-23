using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CompactCheckedNestedRecordLeaf(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        int tupleType = typeBits.IndexOf((TupleTypeBits, null, null, null));
        if (tupleType < 0)
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream(code.Length);
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        for (int boundary = 0; boundary < body.Count; boundary++)
        {
            byte opcode = body[boundary].Definition.Opcode;
            if (opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = boundary + 1;
            int end = boundary;
            if (opcode == FunctionInfo)
            {
                while (end > start && body[end - 1].Definition.Opcode == Line)
                {
                    end--;
                }

                if (end <= start || body[--end].Definition.Opcode != Label)
                {
                    continue;
                }
            }

            if (end <= start + 12 || body[start].Definition.Opcode != Label
                || body[start + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple, Operands: [_, var parent, var outerWidth, _]
                }
                || parent is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                || body[start + 2] is not
                {
                    Definition.Opcode: Allocate, Operands: [var width, var live]
                }
                || live.Value <= parent.Value || live.Value >= FirstReservedXRegister || width.Value != live.Value + 1)
            {
                continue;
            }

            var inputs = new Dictionary<int, BeamOperand>();
            int read = start + 3;
            for (; read < end && body[read] is
            {
                Definition.Opcode: Move,
                Operands:
                    [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } from,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } to
                    ]
            } && from.Value < live.Value && to.Value < width.Value
                  && inputs.TryAdd((int)to.Value, from); read++)
            {
            }

            if (inputs.Count != live.Value || inputs.Values.Distinct().Count() != inputs.Count
                || read + 9 >= end
                || body[read] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var outer, var field, { Kind: BeamOperandParameters.Y, TypeIndex: null } child]
                }
                || outer is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || !inputs.TryGetValue((int)outer.Value, out var original) || original != parent
                || child.Value >= width.Value || inputs.ContainsKey((int)child.Value) || field.Value >= outerWidth.Value
                || body[read + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var checkedChild, var innerWidth, var tag]
                } || checkedChild != child
                || body[read + 2] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                || ReadHeapWords(code, body[read + 2]) is not { End: var liveOffset, Words: var words }
                || words < outerWidth.Value + innerWidth.Value + 2
                || body[read + 3] is not
                {
                    Definition.Opcode: UpdateRecord,
                    Operands: [_, var firstWidth, var firstSource, var firstResult, _, ..]
                } first
                || firstWidth != innerWidth || !SameProjectionRegister(firstSource, child)
                || firstSource.TypeIndex != tupleType || firstResult != x0
                || body[read + 4] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var relayResult, { Kind: BeamOperandParameters.Y, TypeIndex: null } relay]
                }
                || relayResult != x0 || relay.Value >= width.Value || relay == outer
                || body[read + 5] is not
                {
                    Definition.Opcode: UpdateRecord,
                    Operands: [_, var secondWidth, var secondSource, var secondResult, _, var replaceField, var replacement]
                } second
                || secondWidth != outerWidth || !SameProjectionRegister(secondSource, outer)
                || secondSource.TypeIndex != tupleType || secondResult != x0 || replacement != relay
                || replaceField.Value != field.Value + 1
                || body[read + 6] is not { Definition.Opcode: Deallocate, Operands: [var released] } || released != width
                || body[read + 7].Definition.Opcode != Return)
            {
                continue;
            }

            const int FirstField = FirstRecordUpdateValueOperand - 1;
            if (first.Operands.Count <= FirstField
                || first.Operands.Skip(FirstField).Where((_, index) => index % 2 == 1).Any(value =>
                    value.TypeIndex is not null
                    || (value.Kind == BeamOperandParameters.Y && !inputs.ContainsKey((int)value.Value))
                    || value.Kind is not (BeamOperandParameters.Y or BeamOperandParameters.Atom
                        or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil))
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(read + 1, (int)rejected.Value, out int error)
                || error != read + 8)
            {
                continue;
            }

            int exception = error + 1;
            while (exception < end && body[exception].Definition.Opcode == Line)
            {
                exception++;
            }

            if (exception + 1 != end
                || body[exception] is not { Definition.Opcode: BadRecord, Operands: [var invalid] }
                || invalid != child)
            {
                continue;
            }

            // The entire nested update is a leaf. The child check sees its
            // original payload and the heap check retains the two records and
            // all incoming replacement values. The inner result dies at the
            // update of the outer record.
            var loaded = new BeamOperand(BeamOperandParameters.X, live.Value);
            var updated = new BeamOperand(BeamOperandParameters.X, original == x0 ? 1 : 0);
            output.Write(code.Span[copied..body[start + 2].Offset]);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, original);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, loaded);
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            WriteRegister(output, loaded);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, innerWidth.Value);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            output.Write(code.Span[body[read + 2].Offset..liveOffset]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value + 1);
            WriteUpdate(
                first,
                loaded,
                updated,
                operand => operand.Kind == BeamOperandParameters.Y ? inputs[(int)operand.Value] : operand
            );
            WriteUpdate(
                second,
                original,
                x0,
                _ => updated with { TypeIndex = tupleType }
            );
            output.WriteByte(Return);
            output.Write(code.Span[body[error].Offset..body[exception].Offset]);
            output.WriteByte(BadRecord);
            WriteRegister(output, loaded);
            copied = body[end].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteUpdate(
            BeamInstruction instruction,
            BeamOperand source,
            BeamOperand result,
            Func<BeamOperand, BeamOperand> map
        )
        {
            IReadOnlyList<BeamOperand> operands = instruction.Operands;
            output.WriteByte(UpdateRecord);
            BeamCompactWriter.Write(output, AtomTag, operands[0].Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operands[1].Value);
            WriteTyped(source with { TypeIndex = tupleType });
            WriteRegister(output, result);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operands[4].Value);
            for (int index = FirstRecordUpdateValueOperand; index < operands.Count; index += 2)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operands[index - 1].Value);
                BeamOperand value = map(operands[index]);
                if (value.Kind == BeamOperandParameters.X)
                {
                    WriteTyped(value);
                }
                else
                {
                    WriteTupleJoinOperand(output, value);
                }
            }
        }

        void WriteTyped(BeamOperand register)
        {
            if (register.TypeIndex is int type)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
                WriteRegister(output, register);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }
            else
            {
                WriteRegister(output, register);
            }
        }
    }

    private byte[]? CompactLiteralRecordUpdateLeaf(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        BeamProjectionFlow? flow = null;
        var edits = new List<CodeEdit>();
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            for (int index = start; index + 7 < end; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: Allocate,
                        Operands: [{ Value.IsOne: true }, var incoming]
                    } || body[index + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } source,
                        { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null } saved]
                    } || incoming.Value <= source.Value || body[index + 2] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [{ Kind: BeamOperandParameters.Label } failure, var record, var width, _]
                    } || !SameProjectionRegister(record, saved)
                    || body[index + 3] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                    || body[index + 4] is not { Definition.Opcode: UpdateRecord, Operands: var update }
                    || update[1].Value != width.Value || !SameProjectionRegister(update[2], saved) || !IsBareX0(update[3])
                    || update.Count <= FirstRecordUpdateValueOperand
                    || update.Skip(FirstRecordUpdateValueOperand).Any(operand =>
                        operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y or BeamOperandParameters.Float)
                    || body[index + 5] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                    || body[index + 6].Definition.Opcode != Return
                    || code.Span[body[index + 4].Offset - 1] != BeamWire.UnsignedTag)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.ReachesLinearFrameUnframed(start, index)
                    || !flow.TryGetSingleForwardTarget(index + 2, (int)failure.Value, out int rejected)
                    || rejected <= index + 6 || rejected >= end)
                {
                    continue;
                }

                int error = rejected + 1;
                while (error < end && body[error].Definition.Opcode == Line)
                {
                    error++;
                }

                if (error >= end || body[error] is not { Definition.Opcode: BadRecord, Operands: [var original] }
                    || !SameProjectionRegister(original, saved))
                {
                    continue;
                }

                // Both continuations use the original record. The discarded
                // entry frame proved the complete X prefix at this point.
                edits.Add(new CodeEdit(
                    body[index].Offset,
                    body[index + 2].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));
                int heap = body[index + 4].Offset - 1;
                edits.Add(new CodeEdit(
                    heap,
                    heap + 1,
                    BeamWire.UnsignedTag,
                    (int)source.Value + 1
                ));
                edits.Add(new CodeEdit(
                    body[index + 5].Offset,
                    body[index + 6].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));
                RemapSlot(index + 2, index + 5, (int)source.Value);
                RemapSlot(error, error + 1, (int)source.Value);
                index += 6;
            }
        }

        edits.Sort((left, right) => left.Start.CompareTo(right.Start));

        return ApplyDuplicatedFrameReadEdits(code, edits);

        void RemapSlot(int first, int limit, int register)
        {
            int offset = body[limit].Offset;
            int stack = FirstStackOperandAtOrAfter(stackOperands, body[first].Offset);
            while (stack < stackOperands.Count && stackOperands[stack].Start < offset)
            {
                var (from, to, _) = stackOperands[stack++];
                edits.Add(new CodeEdit(
                    from,
                    to,
                    XRegisterTag,
                    register
                ));
            }
        }
    }
}
