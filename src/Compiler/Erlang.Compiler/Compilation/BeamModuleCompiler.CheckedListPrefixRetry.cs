using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseCheckedListPrefixOnRetry(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        for (int first = 0; first + 3 < body.Count; first++)
        {
            if (ReadCheckedListPrefix(body, first) is not { } before)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetForwardLabel(first + 2, before.Failure, out int retry))
            {
                continue;
            }

            int check = retry + 1;
            while (check < body.Count && body[check].Definition.Opcode == GetTupleElement)
            {
                check++;
            }

            if (ReadCheckedListPrefix(body, check) is not { } after
                || before.List != after.List || before.Head != after.Head || before.Tail != after.Tail
                || before.Arity != after.Arity || before.Tag != after.Tag
                || !flow.TryGetForwardLabel(check + 2, after.Failure, out int fallback)
                || !HasUnprotectedCheckedFailureRegion(body, first, fallback)
                || !flow.HasCommonListPrefixOnRetry(first, retry, before.List)
                || !ProjectionsAgree(first, retry, check))
            {
                continue;
            }

            // Failure of either original prefix test implies failure of the
            // retry's identical prefix. Every other edge comes from after
            // successful matching and still owns the original list value.
            Redirect(first, after.Failure);
            Redirect(first + 2, after.Failure);
            Remove(check);
            Remove(check + 2);
            first = check + 2;
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        bool ProjectionsAgree(int before, int label, int after)
        {
            int count = after - label - 1;
            if (count == 0)
            {
                return true;
            }

            var setup = body[before + 1].Operands;
            var carrier = body[label + 1].Operands[0];
            for (int offset = 0; offset < count; offset++)
            {
                int original = before - count + offset;
                var retry = body[label + 1 + offset];
                if (original < 0 || body[original].Definition.Opcode != GetTupleElement
                    || !body[original].Operands.SequenceEqual(retry.Operands)
                    || retry.Operands is not [var tuple, _, var result]
                    || tuple != carrier || !IsBareX(tuple) || !IsBareX(result) || tuple == result
                    || setup.Contains(tuple) || setup.Contains(result))
                {
                    return false;
                }
            }

            return true;
        }

        void Redirect(int index, int label)
        {
            int start = body[index].Offset + 1;
            var operand = new BeamCursor(code[start..]);
            _ = BeamCompactReader.Read(operand);
            edits.Add(new(
                start,
                start + operand.Position,
                LabelTag,
                label
            ));
        }

        void Remove(int index) => edits.Add(new(
            body[index].Offset,
            body[index + 1].Offset,
            0,
            0,
            IsRemoval: true
        ));
    }

    private static (int Failure, BeamOperand List, BeamOperand Head, BeamOperand Tail, BeamOperand Arity, BeamOperand Tag)?
        ReadCheckedListPrefix(IReadOnlyList<BeamInstruction> body, int index)
    {
        if (index + 3 >= body.Count || body[index] is not
            { Definition.Opcode: IsNonemptyList, Operands: [{ Kind: BeamOperandParameters.Label } failure, var list] }
            || body[index + 1] is not
            { Definition.Opcode: GetList, Operands: [var from, var head, var tail] }
            || body[index + 2] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [var rejected, var tested, var arity, { Kind: BeamOperandParameters.Atom } tag]
            } || failure != rejected || from != list || tested != head
            || !IsBareX(list) || !IsBareX(head) || !IsBareX(tail)
            || list == head || list == tail || head == tail)
        {
            return null;
        }

        return ((int)failure.Value, list, head, tail, arity, tag);
    }


    private (ReadOnlyMemory<byte> Code, int LabelCount)? SelectCheckedListDelimiterPrefixes(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        Dictionary<int, string>? names = null;
        using var output = new MemoryStream();
        int copied = 0;
        int added = 0;
        for (int first = 0; first + 4 < body.Count; first++)
        {
            if (ReadCheckedListPrefix(body, first) is not { } original
                || body[first].Offset < copied)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetForwardLabel(first + 2, original.Failure, out int retry)
                || ReadCheckedListPrefix(body, retry + 1) is not { } alternative
                || original.List != alternative.List || original.Head != alternative.Head
                || original.Tail != alternative.Tail || original.Arity != alternative.Arity
                || original.Tag == alternative.Tag || original.Arity.Value <= 0
                || !flow.TryGetForwardLabel(retry + 3, alternative.Failure, out int fallback)
                || !HasUnprotectedCheckedFailureRegion(body, first, fallback)
                || !flow.TryReadListPrefixGuardFailures(
                    first,
                    retry,
                    original.List,
                    out int[] failures
                )
                || !flow.IsEntryTemporaryDead(first + 3, original.Head)
                || !flow.IsEntryTemporaryDead(retry + 4, original.Head)
                || !flow.IsEntryTemporaryDead(fallback, original.Head)
                || !flow.IsEntryTemporaryDead(fallback, original.Tail))
            {
                continue;
            }

            names ??= atoms.Where(atom => MapEmittedAtom(atom.Value) > 0)
                .ToDictionary(atom => MapEmittedAtom(atom.Value), atom => atom.Key);
            if (!names.TryGetValue((int)original.Tag.Value, out string? firstTag)
                || !names.TryGetValue((int)alternative.Tag.Value, out string? secondTag))
            {
                continue;
            }

            int firstTarget = labelCount + added++;
            output.Write(code.Span[copied..body[first].Offset]);
            output.WriteByte(IsNonemptyList);
            WriteLabelOperand(alternative.Failure);
            WriteRegister(output, original.List);
            CopyInstruction(first + 1);
            output.WriteByte(IsTuple);
            WriteLabelOperand(alternative.Failure);
            WriteRegister(output, original.Head);
            output.WriteByte(TestArity);
            WriteLabelOperand(alternative.Failure);
            WriteRegister(output, original.Head);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, original.Arity.Value);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, original.Head);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            WriteRegister(output, original.Head);
            output.WriteByte(SelectValue);
            WriteRegister(output, original.Head);
            WriteLabelOperand(alternative.Failure);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 4);
            if (TermOrder.Compare(new Atom(firstTag), new Atom(secondTag), exact: true) < 0)
            {
                WriteCase(original.Tag, firstTarget);
                WriteCase(alternative.Tag, original.Failure);
            }
            else
            {
                WriteCase(alternative.Tag, original.Failure);
                WriteCase(original.Tag, firstTarget);
            }

            output.WriteByte(Label);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, firstTarget);
            int start = body[first + 3].Offset;
            foreach (int failed in failures)
            {
                int operand = body[failed].Offset + 1;
                var cursor = new BeamCursor(code[operand..]);
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[start..operand]);
                WriteLabelOperand(alternative.Failure);
                start = operand + cursor.Position;
            }

            // The first arm's later rejection cannot match the second tag.
            // The outer fallback observes neither local split scratch value.
            output.Write(code.Span[start..body[retry + 1].Offset]);
            copied = body[retry + 4].Offset;
            first = retry + 3;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, SelectValue);
        output.Write(code.Span[copied..]);

        return OrderFinalPhysicalLabelsCore(
            output.ToArray(),
            labelCount + added,
            formMultiKeySelections: false,
            relocateOnly: true
        );

        void WriteLabelOperand(int label) => BeamCompactWriter.Write(output, LabelTag, label);

        void WriteCase(BeamOperand tag, int label)
        {
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            WriteLabelOperand(label);
        }

        void CopyInstruction(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
    }

    private static bool IsBareX(BeamOperand operand) => operand is { Kind: BeamOperandParameters.X, TypeIndex: null };

    private sealed partial class BeamProjectionFlow
    {
        internal bool TryReadListPrefixGuardFailures(
            int prefix,
            int retry,
            BeamOperand list,
            out int[] failures
        )
        {
            failures = [];
            if (!HasCommonListPrefixOnRetry(prefix, retry, list))
            {
                return false;
            }

            var label = body[retry].Operands[0].Value;
            failures = predecessors[retry].Where(edge => edge > prefix + 2).Distinct().Order().ToArray();

            // Fallthrough entries would need their current frame and cursor.
            // Only explicit, pure guard failures can bypass the disjoint tag.
            return failures.All(edge => IsEntryFramePredicate(body[edge].Definition.Opcode)
                && body[edge].Operands[0].Kind == BeamOperandParameters.Label
                && body[edge].Operands[0].Value == label);
        }

        internal bool HasCommonListPrefixOnRetry(int prefix, int retry, BeamOperand list) =>
            predecessors[retry].Any(edge => edge > prefix + 2)
            && ProvesConsOnRemainingRetryEdges(prefix, retry, list);
    }
}
