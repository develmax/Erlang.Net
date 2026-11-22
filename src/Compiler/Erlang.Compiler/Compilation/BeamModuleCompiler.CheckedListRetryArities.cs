using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? SelectCheckedListRetryArities(
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
        Dictionary<int, string>? atomNames = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int added = 0;
        for (int start = 0; start + 10 < body.Count; start++)
        {
            if (body[start].Offset < copied || ReadCheckedListPrefix(body, start) is not { } first)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetForwardLabel(start + 2, first.Failure, out int otherLabel)
                || ReadCheckedListPrefix(body, otherLabel + 1) is not { } other
                || first.List != other.List || first.Head != other.Head || first.Tail != other.Tail
                || first.Arity.Value <= other.Arity.Value || other.Arity.Value <= 0 || first.Tag == other.Tag
                || !flow.TryGetForwardLabel(otherLabel + 3, other.Failure, out int retryLabel)
                || ReadCheckedListPrefix(body, retryLabel + 1) is not { } retry
                || first.List != retry.List || first.Head != retry.Head || first.Tail != retry.Tail
                || first.Arity != retry.Arity || first.Tag != retry.Tag
                || !flow.TryGetForwardLabel(retryLabel + 3, retry.Failure, out int fallback)
                || !HasUnprotectedCheckedFailureRegion(body, start, fallback))
            {
                continue;
            }

            int retryEnd = retryLabel + 4;
            while (retryEnd < fallback && body[retryEnd].Definition.Opcode != Label)
            {
                retryEnd++;
            }

            if (ReadArm(
                start,
                otherLabel,
                first.Failure,
                first.List
            ) is not { } firstArm
                || ReadArm(
                    otherLabel + 1,
                    retryLabel,
                    other.Failure,
                    first.List
                ) is not { } otherArm
                || ReadArm(
                    retryLabel + 1,
                    retryEnd,
                    retry.Failure,
                    first.List
                ) is not { } retryArm
                || !flow.HasExclusiveCheckedTokenFailures(otherLabel, firstArm.Guards)
                || !flow.HasExclusiveCheckedTokenFailures(retryLabel, otherArm.Guards)
                || !firstArm.Writes.Concat(otherArm.Writes).Concat(retryArm.Writes)
                    .All(value => flow.IsEntryTemporaryDead(fallback, value)))
            {
                continue;
            }

            int scratchIndex = (int)System.Numerics.BigInteger.Max(
                first.List.Value,
                System.Numerics.BigInteger.Max(first.Head.Value, first.Tail.Value)
            ) + 1;
            if (scratchIndex >= FirstReservedXRegister)
            {
                continue;
            }

            var scratch = new BeamOperand(BeamOperandParameters.X, scratchIndex);
            BeamOperand otherTag = flow.IsEntryTemporaryDead(otherLabel + 4, other.Head)
                ? other.Head
                : scratch;
            if (!flow.IsEntryTemporaryDead(start + 3, scratch)
                || !flow.IsEntryTemporaryDead(fallback, scratch)
                || !flow.IsEntryTemporaryDead(otherLabel + 4, otherTag))
            {
                continue;
            }

            // Every retry restarts from the original list. A failed guard
            // cannot re-enter an arm of a different head arity.
            int firstLabel = labelCount + added++;
            output.Write(code.Span[copied..body[start].Offset]);
            output.WriteByte(IsNonemptyList);
            Failure(retry.Failure);
            WriteRegister(output, first.List);
            CopyInstruction(start + 1);
            output.WriteByte(IsTuple);
            Failure(retry.Failure);
            WriteRegister(output, first.Head);
            output.WriteByte(SelectTupleArity);
            output.WriteByte(ExtendedTypedRegisterOperand);
            WriteRegister(output, first.Head);
            Unsigned(GetOperandTypeIndex((TupleTypeBits, null, null, null)));
            Failure(retry.Failure);
            output.WriteByte(ExtendedListOperand);
            Unsigned(4);
            Unsigned((int)other.Arity.Value);
            Failure(first.Failure);
            Unsigned((int)first.Arity.Value);
            Failure(firstLabel);
            output.WriteByte(Label);
            Unsigned(firstLabel);
            TestTag(
                first.Head,
                scratch,
                first.Tag,
                retry.Failure
            );
            int firstStart = start + 3;
            int retryStart = retryLabel + 4;
            if (ReadDelimiter(
                firstStart,
                firstArm.Guards[^1],
                retryStart,
                retryArm.Guards,
                fallback,
                scratchIndex
            ) is { } delimiter)
            {
                int completed = labelCount + added++;
                WriteArm(
                    firstStart,
                    delimiter.First,
                    firstArm.Guards,
                    retry.Failure
                );
                WriteDelimiterSelect(
                    delimiter,
                    retry.Failure,
                    other.Failure,
                    completed
                );
                output.WriteByte(Label);
                Unsigned(completed);
                output.Write(code.Span[body[delimiter.First + 1].Offset..body[otherLabel].Offset]);
                CopyInstruction(retryLabel);
                output.Write(code.Span[body[delimiter.Retry + 1].Offset..body[retryEnd].Offset]);
            }
            else
            {
                WriteArm(
                    firstStart,
                    otherLabel,
                    firstArm.Guards,
                    other.Failure
                );
                CopyInstruction(retryLabel);
                CopyInstruction(retryLabel + 2);
                output.Write(code.Span[body[retryStart].Offset..body[retryEnd].Offset]);
            }

            CopyInstruction(otherLabel);
            TestTag(
                other.Head,
                otherTag,
                other.Tag,
                retry.Failure
            );
            WriteArm(
                otherLabel + 4,
                retryLabel,
                otherArm.Guards,
                retry.Failure
            );
            copied = body[retryEnd].Offset;
            start = retryEnd - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, SelectTupleArity);
        output.Write(code.Span[copied..]);

        return (output.ToArray(), labelCount + added);

        (List<int> Guards, HashSet<BeamOperand> Writes)? ReadArm(
            int index,
            int end,
            int failure,
            BeamOperand source
        )
        {
            if (end <= index + 3 || body[end - 1].Definition.Opcode != Return)
            {
                return null;
            }

            var guards = new List<int>();
            var writes = new HashSet<BeamOperand>();
            for (; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                switch (instruction.Definition.Opcode)
                {
                    case IsNonemptyList or IsTaggedTuple when instruction.Operands[0].Kind == BeamOperandParameters.Label
                        && instruction.Operands[0].Value == failure:
                        guards.Add(index);
                        break;
                    case GetList when instruction.Operands is [var list, var head, var tail] && IsBareX(list)
                        && IsBareX(head) && IsBareX(tail) && head != source && tail != source:
                        writes.Add(head);
                        writes.Add(tail);
                        break;
                    case GetTupleElement when instruction.Operands is [var tuple, _, var destination]
                        && IsBareX(tuple) && IsBareX(destination) && destination != source:
                        writes.Add(destination);
                        break;
                    default:
                        return guards.Count > 2 && HasOwnedSuffix(index, end) ? (guards, writes) : null;
                }
            }

            return null;
        }

        (int First, int Retry, BeamOperand Head, int Arity, BeamOperand Scratch, BeamOperand FirstTag,
            BeamOperand RetryTag, bool FirstTagSortsBeforeRetry)?
            ReadDelimiter(
                int firstStart,
                int firstCheck,
                int retryStart,
                IReadOnlyList<int> retryGuards,
                int fallback,
                int scratchStart
            )
        {
            int count = firstCheck - firstStart;
            int retryCheck = retryStart + count;
            if (count < 3 || !retryGuards.Contains(retryCheck)
                || body[firstCheck + 1].Definition.Opcode is not (Allocate or AllocateHeap)
                || body[firstCheck] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var head, var arity, { Kind: BeamOperandParameters.Atom } firstTag]
                }
                || body[retryCheck] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var tested, var retryArity, { Kind: BeamOperandParameters.Atom } retryTag]
                }
                || tested != head || retryArity != arity || arity.Value <= 0 || firstTag == retryTag
                || !IsBareX(head))
            {
                return null;
            }

            bool sawSharedTag = false;
            for (int offset = 0; offset < count; offset++)
            {
                BeamInstruction first = body[firstStart + offset];
                BeamInstruction retry = body[retryStart + offset];
                byte opcode = first.Definition.Opcode;
                int skip = opcode is IsNonemptyList or IsTaggedTuple ? 1 : 0;
                if (opcode != retry.Definition.Opcode
                    || !first.Operands.Skip(skip).SequenceEqual(retry.Operands.Skip(skip)))
                {
                    return null;
                }

                sawSharedTag |= opcode == IsTaggedTuple;
            }

            if (!sawSharedTag)
            {
                return null;
            }

            atomNames ??= atoms.Where(atom => MapEmittedAtom(atom.Value) > 0)
                .ToDictionary(atom => MapEmittedAtom(atom.Value), atom => atom.Key);
            if (!atomNames.TryGetValue((int)firstTag.Value, out string? firstName)
                || !atomNames.TryGetValue((int)retryTag.Value, out string? retryName))
            {
                return null;
            }

            for (int index = scratchStart; index < FirstReservedXRegister; index++)
            {
                var scratch = new BeamOperand(BeamOperandParameters.X, index);
                if (scratch != head && flow is not null
                    && flow.IsEntryTemporaryDead(firstCheck + 1, scratch)
                    && flow.IsEntryTemporaryDead(retryCheck + 1, scratch)
                    && flow.IsEntryTemporaryDead(fallback, scratch))
                {
                    bool firstTagSortsBeforeRetry = TermOrder.Compare(
                        new Atom(firstName),
                        new Atom(retryName),
                        exact: true
                    ) < 0;

                    return (firstCheck, retryCheck, head, (int)arity.Value, scratch, firstTag, retryTag,
                        firstTagSortsBeforeRetry);
                }
            }

            return null;
        }

        void WriteDelimiterSelect(
            (int First, int Retry, BeamOperand Head, int Arity, BeamOperand Scratch, BeamOperand FirstTag,
                BeamOperand RetryTag, bool FirstTagSortsBeforeRetry) delimiter,
            int fallback,
            int retry,
            int completed
        )
        {
            output.WriteByte(IsTuple);
            Failure(fallback);
            WriteRegister(output, delimiter.Head);
            output.WriteByte(TestArity);
            Failure(fallback);
            WriteRegister(output, delimiter.Head);
            Unsigned(delimiter.Arity);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, delimiter.Head);
            Unsigned(0);
            WriteRegister(output, delimiter.Scratch);
            output.WriteByte(SelectValue);
            WriteRegister(output, delimiter.Scratch);
            Failure(fallback);
            output.WriteByte(ExtendedListOperand);
            Unsigned(4);
            if (delimiter.FirstTagSortsBeforeRetry)
            {
                WriteCase(delimiter.FirstTag, completed);
                WriteCase(delimiter.RetryTag, retry);
            }
            else
            {
                WriteCase(delimiter.RetryTag, retry);
                WriteCase(delimiter.FirstTag, completed);
            }

            void WriteCase(BeamOperand tag, int target)
            {
                BeamCompactWriter.Write(output, AtomTag, tag.Value);
                Failure(target);
            }
        }

        bool HasOwnedSuffix(int index, int end)
        {
            if (body[index].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                return false;
            }

            for (; index < end; index++)
            {
                byte opcode = body[index].Definition.Opcode;
                if (opcode is LocalCall or ExternalCall)
                {
                    if (body[index - 1].Definition.Opcode != Line)
                    {
                        return false;
                    }
                }
                else if (opcode is not (Allocate or AllocateHeap or InitYRegisters or Move or PutTuple
                    or PutList or Line or TestHeap or Deallocate or Return))
                {
                    return false;
                }
            }

            return true;
        }

        void CopyInstruction(int index) =>
            output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void Unsigned(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);

        void Failure(int value) => BeamCompactWriter.Write(output, LabelTag, value);

        void TestTag(
            BeamOperand head,
            BeamOperand scratch,
            BeamOperand tag,
            int failure
        )
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, head);
            Unsigned(0);
            WriteRegister(output, scratch);
            output.WriteByte(ExactEqualTest);
            Failure(failure);
            WriteRegister(output, scratch);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
        }

        void WriteArm(
            int start,
            int end,
            IReadOnlyList<int> guards,
            int failure
        )
        {
            foreach (int guard in guards)
            {
                if (guard < start || guard >= end)
                {
                    continue;
                }

                int operand = body[guard].Offset + 1;
                var cursor = new BeamCursor(code[operand..]);
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[body[start].Offset..operand]);
                Failure(failure);
                output.Write(code.Span[(operand + cursor.Position)..body[guard + 1].Offset]);
                start = guard + 1;
            }

            output.Write(code.Span[body[start].Offset..body[end].Offset]);
        }
    }
}
