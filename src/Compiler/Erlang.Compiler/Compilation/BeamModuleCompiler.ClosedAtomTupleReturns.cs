using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ReadOnlyMemory<byte>? PartitionLateSavedEffectfulTupleReturns(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || !code.Span.Contains(CaseEnd) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var references = body.SelectMany(instruction => instruction.Operands
            .Where(operand => operand.Kind == BeamOperandParameters.Label)
            .Select(operand => (Start: instruction.Offset, End: instruction.Offset, Label: (int)operand.Value)))
            .ToArray();
        if (PartitionClosedAtomTupleReturns(
            code,
            body,
            references,
            savedEffectsOnly: true
        ) is not { } rewritten)
        {
            return null;
        }

        return OrderTypeOperandsByFirstUse(rewritten, labelCount);
    }

    private byte[]? PartitionClosedAtomTupleReturns(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> references,
        bool savedEffectsOnly = false
    )
    {
        if (!code.Span.Contains(IsTaggedTuple) || !code.Span.Contains(CaseEnd))
        {
            return null;
        }

        var inputs = references.GroupBy(reference => reference.Label)
            .ToDictionary(group => group.Key, group => group.Count());
        var callResult = new BeamOperand(BeamOperandParameters.X, 0);
        int begin = 0;
        int end = 0;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        while (end < body.Count)
        {
            byte opcode = body[end].Definition.Opcode;
            if (opcode is not (FunctionInfo or EndCode))
            {
                end++;
                continue;
            }

            if (!body.Skip(begin).Take(end - begin).Any(instruction => instruction.Definition.Opcode is
                TryEnter or CatchEnter or LoopReceive or Wait or WaitTimeout or DebugLine or ExecutableLine))
            {
                for (int index = begin + 1; index + 6 < end; index++)
                {
                    if (body[index] is not
                        {
                            Definition.Opcode: ExactEqualTest,
                            Operands: [var other, var selector, { Kind: BeamOperandParameters.Atom } literal]
                        } || selector != callResult && selector is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                        || !inputs.TryGetValue((int)other.Value, out int count) || count != 1
                        || (savedEffectsOnly
                            ? selector.Kind != BeamOperandParameters.Y || !IsMove(body[index - 1], callResult, selector)
                            : body[index - 1].Definition.Opcode != ExternalCall))
                    {
                        continue;
                    }

                    int atomReturn = ReadClosedReturn(index + 1, end);
                    if (atomReturn < 0 || atomReturn + 2 >= end
                        || body[atomReturn + 1] is not { Definition.Opcode: Label, Operands: [var otherEntry] }
                        || otherEntry.Value != other.Value
                        || body[atomReturn + 2] is not
                        {
                            Definition.Opcode: IsTaggedTuple,
                            Operands: [var wrongValue, var tested, var arity, { Kind: BeamOperandParameters.Atom } tag]
                        } || tested != selector || arity.Value != 2)
                    {
                        continue;
                    }

                    int tupleStart = atomReturn + 3;
                    int tupleReturn = ReadClosedReturn(tupleStart, end);
                    int expectedFailureInputs = 1;
                    bool carryAcrossEffect = false;
                    if (savedEffectsOnly)
                    {
                        if (ReadSavedEffectfulTupleReturn(
                            tupleStart,
                            end,
                            selector,
                            wrongValue
                        ) is not { } planned)
                        {
                            continue;
                        }

                        tupleReturn = planned.Return;
                        expectedFailureInputs += planned.Checks;
                    }
                    else if (tupleReturn < 0 && selector == callResult
                        && ReadEffectfulTupleReturn(tupleStart, end) is { } effect
                        && !body.Skip(index + 1).Take(atomReturn - index)
                            .Any(operation => operation.Operands.Any(value => SameProjectionRegister(value, effect.Saved))))
                    {
                        carryAcrossEffect = true;
                        tupleReturn = effect.Return;
                    }

                    if (tupleReturn < 0 || tupleReturn + 2 >= end
                        || body[tupleReturn + 1] is not { Definition.Opcode: Label, Operands: [var failure] }
                        || failure.Value != wrongValue.Value)
                    {
                        continue;
                    }

                    bool saved = selector.Kind == BeamOperandParameters.Y;
                    int failureLoad = tupleReturn + 2;
                    if (saved && (!inputs.TryGetValue((int)wrongValue.Value, out int failureInputs) || failureInputs != expectedFailureInputs
                        || body[failureLoad] is not { Definition.Opcode: Move, Operands: [var failed, var loaded] }
                        || failed != selector || loaded != callResult))
                    {
                        continue;
                    }

                    int caseEnd = saved ? failureLoad + 1 : failureLoad;
                    bool hasFailureLine = caseEnd < end && body[caseEnd] is
                    { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] };
                    if (savedEffectsOnly && !hasFailureLine)
                    {
                        continue;
                    }

                    if (caseEnd < end && body[caseEnd].Definition.Opcode == Line)
                    {
                        caseEnd++;
                    }

                    if (caseEnd >= end || body[caseEnd] is not
                        { Definition.Opcode: CaseEnd, Operands: [var value] } || value != callResult)
                    {
                        continue;
                    }

                    // Adjacent constructor types are disjoint in beam_core_to_ssa.
                    // These arms contain only root-preserving constructors and returns.
                    output.Write(code.Span[copied..body[index].Offset]);
                    if (carryAcrossEffect)
                    {
                        // The matched tuple is a frame root across the ordinary effect.
                        output.Write(code.Span[body[tupleStart].Offset..body[tupleStart + 1].Offset]);
                    }

                    output.WriteByte(IsTuple);
                    BeamCompactWriter.Write(output, LabelTag, other.Value);
                    WriteRegister(output, selector);
                    output.WriteByte(IsTaggedTuple);
                    BeamCompactWriter.Write(output, LabelTag, wrongValue.Value);
                    output.WriteByte(ExtendedTypedRegisterOperand);
                    WriteRegister(output, selector);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, GetOperandTypeIndex((TupleTypeBits, null, null, null)));
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                    BeamCompactWriter.Write(output, AtomTag, tag.Value);
                    int tupleBody = carryAcrossEffect ? tupleStart + 1 : tupleStart;
                    if (saved && IsIdenticalSavedTupleReturn(
                        tupleBody,
                        tupleReturn,
                        selector,
                        tag
                    ))
                    {
                        // sys_core_alias retains the already matched tuple for an identity reconstruction.
                        output.WriteByte(Move);
                        WriteRegister(output, selector);
                        WriteRegister(output, callResult);
                        tupleBody += 3;
                    }
                    output.Write(code.Span[body[tupleBody].Offset..body[tupleReturn + 1].Offset]);
                    output.Write(code.Span[body[atomReturn + 1].Offset..body[atomReturn + 2].Offset]);
                    output.WriteByte(ExactEqualTest);
                    BeamCompactWriter.Write(output, LabelTag, wrongValue.Value);
                    WriteRegister(output, selector);
                    BeamCompactWriter.Write(output, AtomTag, literal.Value);
                    output.Write(code.Span[body[index + 1].Offset..body[atomReturn + 1].Offset]);
                    copied = body[tupleReturn + 1].Offset;
                    index = tupleReturn;
                    if (saved)
                    {
                        output.Write(code.Span[copied..body[failureLoad].Offset]);
                        output.Write(code.Span[body[failureLoad + 1].Offset..body[caseEnd].Offset]);
                        output.WriteByte(CaseEnd);
                        WriteRegister(output, selector);
                        copied = body[caseEnd + 1].Offset;
                        index = caseEnd;
                    }
                }
            }

            begin = ++end;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsIdenticalSavedTupleReturn(
            int start,
            int finish,
            BeamOperand saved,
            BeamOperand matchedTag
        ) =>
            finish == start + 4
            && body[start] is { Definition.Opcode: TestHeap, Operands: [var words, { Value.IsZero: true }] }
            && body[start + 1] is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var tuple, { Value.IsOne: true }, var payload]
            } && tuple == saved && payload == callResult
            && body[start + 2] is { Definition.Opcode: PutTuple, Operands: [var result, var arity, var tag, var field] }
            && result == callResult && arity.Value == 2 && tag == matchedTag && field == callResult
            && words.Value == arity.Value + 1 && body[start + 3].Definition.Opcode == Deallocate;

        (int Return, BeamOperand Saved)? ReadEffectfulTupleReturn(int start, int limit)
        {
            if (body[start] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var input, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || input != callResult)
            {
                return null;
            }

            var aliases = new HashSet<int> { (int)saved.Value };
            int index = start + 1;
            while (index < limit && body[index].Definition.Opcode is Move or Line or TrimFrame)
            {
                var operation = body[index++];
                if (operation is
                    {
                        Definition.Opcode: Move,
                        Operands: [var source, { Kind: BeamOperandParameters.Y } destination]
                    })
                {
                    int slot = (int)destination.Value;
                    if (source.Kind == BeamOperandParameters.Y && aliases.Contains((int)source.Value))
                    {
                        aliases.Add(slot);
                    }
                    else
                    {
                        aliases.Remove(slot);
                    }
                }
                else if (operation is { Definition.Opcode: TrimFrame, Operands: [var dropped, _] })
                {
                    int count = (int)dropped.Value;
                    aliases = aliases.Where(slot => slot >= count).Select(slot => slot - count).ToHashSet();
                }
            }

            if (index + 3 >= limit || body[index].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y } result, var output]
                } || !aliases.Contains((int)result.Value) || output != callResult
                || body[index + 2].Definition.Opcode != Deallocate || body[index + 3].Definition.Opcode != Return)
            {
                return null;
            }

            return (index + 3, saved);
        }

        (int Return, int Checks)? ReadSavedEffectfulTupleReturn(
            int start,
            int limit,
            BeamOperand saved,
            BeamOperand failure
        )
        {
            var aliases = new HashSet<int> { (int)saved.Value };
            int checks = 0;
            bool trimmed = false;
            int index = start;
            for (; index < limit; index++)
            {
                BeamInstruction operation = body[index];
                byte opcode = operation.Definition.Opcode;
                if (opcode is IsTuple or TestArity or IsTaggedTuple or ExactEqualTest or IsNil)
                {
                    if (trimmed || !aliases.Contains((int)saved.Value) || operation.Operands[0] != failure)
                    {
                        return null;
                    }

                    checks++;
                    continue;
                }

                if (opcode == TrimFrame)
                {
                    if (trimmed)
                    {
                        return null;
                    }

                    int dropped = (int)operation.Operands[0].Value;
                    aliases = aliases.Where(slot => slot >= dropped).Select(slot => slot - dropped).ToHashSet();
                    trimmed = true;
                    continue;
                }

                if (opcode == InitYRegisters)
                {
                    aliases.ExceptWith(operation.Operands.Skip(1).Select(slot => (int)slot.Value));
                    continue;
                }

                if (opcode == TestHeap)
                {
                    continue;
                }

                if (opcode is not (Move or GetTupleElement or PutList or PutTuple))
                {
                    break;
                }

                BeamOperand destination = operation.Operands[StackWriteOperand(operation)];
                if (destination.Kind == BeamOperandParameters.Y)
                {
                    if (opcode == Move && operation.Operands[0] is { Kind: BeamOperandParameters.Y } source
                        && aliases.Contains((int)source.Value))
                    {
                        aliases.Add((int)destination.Value);
                    }
                    else
                    {
                        aliases.Remove((int)destination.Value);
                    }
                }
            }

            // The effect keeps its signalling line and the retained status.
            // Check failures all occur while that status still owns its old slot.
            if (checks == 0 || index + 4 >= limit
                || body[index] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
                || body[index + 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } result, var output]
                }
                || !aliases.Contains((int)result.Value) || output != callResult
                || body[index + 3].Definition.Opcode != Deallocate || body[index + 4].Definition.Opcode != Return)
            {
                return null;
            }

            return (index + 4, checks);
        }

        int ReadClosedReturn(int start, int limit)
        {
            int index = start;
            while (index < limit && body[index].Definition.Opcode is
                TestHeap or GetTupleElement or PutTuple or PutList or Move)
            {
                index++;
            }

            if (index < limit && body[index].Definition.Opcode == Deallocate)
            {
                index++;
            }

            return index < limit && body[index].Definition.Opcode == Return ? index : -1;
        }
    }
}
