using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardFreshConsRecordBuilders(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord)
            || !code.Span.Contains(TestHeap) || !atoms.TryGetValue(CopyFreshRecordUpdateHint, out int copyAtom)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        int copy = MapEmittedAtom(copyAtom);
        int reuse = atoms.TryGetValue(ReuseRecordUpdateHint, out int reuseAtom) ? MapEmittedAtom(reuseAtom) : -1;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        bool unprotected = false;
        for (int heap = 0; heap + 5 < body.Count; heap++)
        {
            if (body[heap].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, heap + 1);
                continue;
            }

            if (!unprotected || body[heap].Definition.Opcode != TestHeap)
            {
                continue;
            }

            int first = heap + 1;
            while (first < body.Count && body[first].Definition.Opcode == GetTupleElement)
            {
                first++;
            }

            if (first >= body.Count || body[first] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [var head, var tail, var accumulator]
                } || !IsBareX(head) || !IsBareX(tail) || tail.Value.IsZero || tail.Value >= head.Value
                || accumulator != head)
            {
                continue;
            }

            int end = first + 1;
            while (end < body.Count && body[end].Definition.Opcode is (PutTuple or PutList or Move or UpdateRecord))
            {
                end++;
            }

            if (end >= body.Count || body[end].Definition.Opcode != Return || body[end - 1] is not
                {
                    Definition.Opcode: UpdateRecord,
                    Operands: [var hint, _, _, var result, _, ..]
                } final || hint.Value != copy || result != x0 || FirstFreshConsReplacement(final, head) is not int countField)
            {
                continue;
            }

            var rewritten = new Dictionary<int, int>();
            bool closed = true;
            for (int index = first + 1; index < end; index++)
            {
                if (body[index].Definition.Opcode != Move)
                {
                    continue;
                }

                if (body[index] is not { Operands: [var source, var scratch] } || source != x0
                    || !IsBareX(scratch) || scratch == x0 || scratch == head
                    || body[index - 1] is not { Definition.Opcode: PutList, Operands: [_, _, var cons] } || cons != x0
                    || body[index + 1] is not
                    {
                        Definition.Opcode: UpdateRecord,
                        Operands: [var mode, _, var owner, var updated, _, ..]
                    } update || updated != x0 || SameProjectionRegister(owner, x0)
                    || (mode.Value != copy && mode.Value != reuse)
                    || FirstFreshConsReplacement(update, scratch) is not int field
                    || !IsTerminalConstructionScratchDead(
                        body,
                        index + 2,
                        end,
                        scratch
                    ))
                {
                    closed = false;
                    break;
                }

                rewritten.Add(index + 1, field);
            }

            if (!closed || !rewritten.ContainsKey(end - 1) || Enumerable.Range(first + 1, end - first - 1)
                .Any(index => ObservesConsCarrier(index)))
            {
                continue;
            }

            // The lower tail dies at the first cons. Every forwarded X0
            // save feeds only the immediately following record replacement.
            // The closed interval keeps the two newly built conses distinct.
            output.Write(code.Span[copied..body[first].Offset]);
            output.WriteByte(PutList);
            WriteRegister(output, head);
            WriteRegister(output, tail);
            WriteRegister(output, tail);
            int type = GetOperandTypeIndex((ConsTypeBits, null, null, null));
            for (int index = first + 1; index < end; index++)
            {
                if (body[index].Definition.Opcode == Move)
                {
                    continue;
                }

                if (!rewritten.TryGetValue(index, out int field))
                {
                    output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                    continue;
                }

                WriteFreshConsRecordUpdate(
                    output,
                    code,
                    body[index],
                    copy,
                    type,
                    field,
                    index == end - 1 ? countField : -1,
                    tail
                );
            }

            copied = body[end].Offset;
            heap = end;

            bool ObservesConsCarrier(int index)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == Move)
                {
                    return false;
                }

                return instruction.Operands.Where((_, operand) =>
                    (!rewritten.TryGetValue(index, out int replacement) || replacement != operand)
                    && (index != end - 1 || operand != countField))
                    .Any(operand => SameProjectionRegister(operand, head) || SameProjectionRegister(operand, tail));
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int? FirstFreshConsReplacement(BeamInstruction update, BeamOperand value)
    {
        var uses = Enumerable.Range(0, update.Operands.Count)
            .Where(index => SameProjectionRegister(update.Operands[index], value)).ToArray();

        return uses is [int field] && field >= FirstRecordUpdateValueOperand
            && (field - FirstRecordUpdateValueOperand) % RecordUpdateFieldPairWidth == 0 ? field : null;
    }

    private static void WriteFreshConsRecordUpdate(
        Stream output,
        ReadOnlyMemory<byte> code,
        BeamInstruction instruction,
        int copy,
        int type,
        int freshField,
        int countField,
        BeamOperand counter
    )
    {
        output.WriteByte(UpdateRecord);
        int offset = instruction.Offset + 1;
        var cursor = new BeamCursor(code[offset..]);
        for (int index = 0; index < instruction.Operands.Count; index++)
        {
            int start = offset + cursor.Position;
            BeamOperand operand = instruction.Operands[index];
            var raw = BeamCompactReader.Read(cursor);
            if (raw.WireTag == BeamOperandParameters.ExtendedTag)
            {
                _ = BeamCompactReader.Read(cursor);
                if (operand.TypeIndex.HasValue)
                {
                    _ = BeamCompactReader.Read(cursor);
                }
            }

            if (index == 0)
            {
                BeamCompactWriter.Write(output, AtomTag, copy);
            }
            else if (index == freshField || index == countField)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
                BeamCompactWriter.Write(output, XRegisterTag, index == countField ? counter.Value : 0);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }
            else
            {
                output.Write(code.Span[start..(offset + cursor.Position)]);
            }
        }
    }
}
