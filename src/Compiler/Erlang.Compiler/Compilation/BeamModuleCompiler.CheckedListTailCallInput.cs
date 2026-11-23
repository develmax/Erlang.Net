using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReleaseReusedListCallSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetList)
            || !code.Span.Contains(PutList) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var entries = new HashSet<int>();
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (start + 15 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var function] }
                || Enumerable.Range(start, end - start).Any(index => body[index].Definition.Opcode == ExecutableLine))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int list = start + 1; list + 14 < end; list++)
            {
                if (body[list] is not
                    {
                        Definition.Opcode: GetList,
                        Operands:
                        [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } input,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } slot,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } tail
                        ]
                    }
                    || slot == tail || body[list + 1] is not { Definition.Opcode: Jump, Operands: [var resumed] }
                    || body[list + 2].Definition.Opcode != Label)
                {
                    continue;
                }

                int cold = SkipLines(list + 3);
                flow ??= new BeamProjectionFlow(body);
                if (cold + 3 >= end
                    || body[cold] is not { Definition.Opcode: BadMatch, Operands: [{ Kind: BeamOperandParameters.Nil }] }
                    || !flow.TryGetSingleForwardTarget(list + 1, (int)resumed.Value, out int resume)
                    || resume != cold + 1
                    || body[resume + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var loaded, { Kind: BeamOperandParameters.X, TypeIndex: null } firstArg]
                    } || loaded != slot || firstArg == input)
                {
                    continue;
                }

                int clear = resume + 2;
                while (clear < end && body[clear] is
                    {
                        Definition.Opcode: Move,
                        Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                    } && destination != firstArg)
                {
                    clear++;
                }

                int call = SkipLines(clear + 1);
                if (call + 4 >= end || body[clear] is not { Definition.Opcode: InitYRegisters } initialized
                    || !initialized.Operands.Skip(1).Contains(slot)
                    || body[call] is not { Definition.Opcode: LocalCall, Operands: [var arity, _] }
                    || firstArg.Value >= arity.Value
                    || body[call + 1].Definition.Opcode != TestHeap
                    || body[call + 2] is not
                    {
                        Definition.Opcode: PutList, Operands: [var head, var oldTail, var consResult]
                    } || head != x0 || oldTail != tail || consResult != x0 || !IsMove(body[call + 3], x0, slot))
                {
                    continue;
                }

                int reload = call + 4;
                while (reload < end && ReadsLinearArgumentSetup(body[reload])
                    && !body[reload].Operands.Any(value => SameProjectionRegister(value, slot)))
                {
                    reload++;
                }

                if (reload >= end || body[reload] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var saved, { Kind: BeamOperandParameters.X, TypeIndex: null } finalArg]
                    } || saved != slot || finalArg == x0
                    || Enumerable.Range(call + 4, reload - call - 4).Any(index =>
                        body[index].Operands.Any(value => SameProjectionRegister(value, finalArg))))
                {
                    continue;
                }

                int terminal = reload + 1;
                while (terminal < end && ReadsLinearArgumentSetup(body[terminal])
                    && !body[terminal].Operands.Any(value => SameProjectionRegister(value, finalArg)))
                {
                    terminal++;
                }

                if (terminal >= end || body[terminal] is not
                    {
                        Definition.Opcode: TailLocalCallWithFrame, Operands: [var finalArity, _, var release]
                    }
                    || finalArg.Value >= finalArity.Value
                    || !OverwritesOldCons(call + 4, terminal)
                    || (ownership ??= ReadDisjointFrameOwnership(body, start, end)) is not { Owners: var owners, Uses: var frames }
                    || frames.Count != 1 || owners[list - start] < 0
                    || owners[terminal - start] != owners[list - start]
                    || body[owners[list - start]].Definition.Opcode != Allocate
                    || frames[owners[list - start]].Length != release.Value
                    || Enumerable.Range(start, end - start)
                        .Where(index => body[index].Definition.Opcode != InitYRegisters)
                        .Sum(index => body[index].Operands.Count(value => SameProjectionRegister(value, slot))) != 4)
                {
                    continue;
                }

                // The Y slot has two definitions. The first is a list head
                // consumed by the first call and the second is a new cons
                // consumed by the tail call. Forward each into its own argument.
                output.Write(code.Span[copied..body[list].Offset]);
                output.WriteByte(GetList);
                WriteRegister(output, input);
                WriteRegister(output, firstArg);
                WriteRegister(output, tail);
                output.Write(code.Span[body[list + 1].Offset..body[resume + 1].Offset]);
                output.Write(code.Span[body[resume + 2].Offset..body[call + 2].Offset]);
                output.WriteByte(PutList);
                WriteRegister(output, x0);
                WriteRegister(output, tail);
                WriteRegister(output, finalArg);
                output.Write(code.Span[body[call + 4].Offset..body[reload].Offset]);
                copied = body[reload + 1].Offset;
                entries.Add((int)function.Value);
                break;
            }

            int SkipLines(int index)
            {
                while (index < end && body[index].Definition.Opcode == Line)
                {
                    index++;
                }

                return index;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();
        if (DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } compacted)
        {
            return null;
        }

        return RewriteDisjointFrames(
            rewritten,
            compacted.Body,
            compacted.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );

        bool OverwritesOldCons(int from, int terminal)
        {
            for (int index = from; index < terminal; index++)
            {
                if (body[index].Definition.Opcode == Line)
                {
                    continue;
                }

                // A permitted BIF can exit only through f(0). Its register
                // inputs are read before the successful destination write.
                var operands = body[index].Operands;
                if (operands.Take(operands.Count - 1).Any(value => SameProjectionRegister(value, x0)))
                {
                    return false;
                }

                if (SameProjectionRegister(operands[^1], x0))
                {
                    return true;
                }
            }

            return false;
        }

        static bool ReadsLinearArgumentSetup(BeamInstruction instruction) =>
            instruction.Definition.Opcode is Line or Move
            || instruction.Definition.Opcode is (BifOne or BifTwo or BifThree)
                && instruction.Operands[0].Kind == BeamOperandParameters.Failure;
    }

    private byte[]? KeepCheckedListTailInCallInput(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteCheckedListTailCallInput(code, body, AnnotateBound);

        BeamOperand AnnotateBound(BeamInstruction info, BeamOperand bound)
        {
            if (bound.TypeIndex is not null || sourceModule is not { DeclaredFunctions: var functions }
                || info is not { Definition.Opcode: FunctionInfo, Operands: [_, var name, var arity] })
            {
                return bound;
            }

            FunctionDefinition? definition = functions.FirstOrDefault(function =>
                function.Arity == arity.Value && atoms.TryGetValue(function.Name, out int atom)
                && MapEmittedAtom(atom) == name.Value);

            return definition is not null && bound.Value < definition.Arity
                && HasClosedNumericEntryArgument(definition, (int)bound.Value, functions)
                ? bound with { TypeIndex = GetOperandTypeIndex((NumberTypeBits, null, null, null)) }
                : bound;
        }
    }

    private static byte[]? RewriteCheckedListTailCallInput(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Func<BeamInstruction, BeamOperand, BeamOperand>? annotateBound
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int split = 2; split + 13 < body.Count; split++)
        {
            if (body[split - 2].Definition.Opcode != Label || body[split - 1] is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands: [var outside, var checkedList]
                }
                || body[split] is not
                {
                    Definition.Opcode: GetList,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } list,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } head,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } tail]
                }
                || checkedList != list || head.Value != list.Value + 1 || tail.Value != head.Value + 1
                || body[split + 1] is not { Definition.Opcode: IsTuple, Operands: [var badTuple, var tuple] }
                || badTuple != outside || tuple != head
                || body[split + 2] is not { Definition.Opcode: TestArity, Operands: [var badArity, var original, _] }
                || badArity != outside || original != head
                || body[split + 3] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var carrier, var position, { Kind: BeamOperandParameters.X, TypeIndex: null } field]
                }
                || carrier != head || field.Value != tail.Value + 1
                || body[split + 4] is not { Definition.Opcode: IsLess, Operands: [var next, var low, var bound] }
                || (!SameProjectionRegister(low, field) || bound.Kind != BeamOperandParameters.X || bound.Value >= list.Value)
                || body[split + 5] is not { Definition.Opcode: Move, Operands: [var firstTail, var firstArg] }
                || firstTail != tail || firstArg != list
                || body[split + 6] is not { Definition.Opcode: TailLocalCall, Operands: [var tailArity, _] }
                || tailArity.Value != head.Value
                || body[split + 7] is not { Definition.Opcode: Label, Operands: [var retry] }
                || retry.Value != next.Value)
            {
                continue;
            }

            if (body[split + 8] is not { Definition.Opcode: IsGreaterOrEqual, Operands: [var rejected, var limit, var high] }
                || rejected != outside || (!SameProjectionRegister(high, field) || limit.Kind != BeamOperandParameters.X || limit.Value >= list.Value)
                || body[split + 9] is not { Definition.Opcode: Move, Operands: [var secondTail, var secondArg] }
                || secondTail != tail || secondArg != list
                || body[split + 10] is not { Definition.Opcode: Allocate, Operands: [{ Value.IsZero: true }, var live] }
                || live.Value != tail.Value || body[split + 11].Definition.Opcode != Line
                || body[split + 12] is not { Definition.Opcode: LocalCall, Operands: [var callArity, _] }
                || callArity.Value != live.Value)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(split + 4, (int)next.Value, out int ownedRetry) || ownedRetry != split + 7
                || !flow.IsGuardTemporaryDeadOnFailure(split + 1, list, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(split + 1, tail, preserveHeapRoots: true)
                || !flow.IsGuardTemporaryDeadOnFailure(split + 8, field, preserveHeapRoots: true))
            {
                continue;
            }

            // OTP allocates the projected tail in its outgoing argument slot when
            // failure never observes the original list. The old tail temporary is
            // then free for the comparison field and is outside both call envelopes.
            output.Write(code.Span[copied..body[split].Offset]);
            output.WriteByte(GetList);
            WriteRegister(output, list);
            WriteRegister(output, head);
            WriteRegister(output, list);
            output.Write(code.Span[body[split + 1].Offset..body[split + 3].Offset]);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, head);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, position.Value);
            WriteRegister(output, tail);
            BeamInstruction? info = split > 2 ? body[split - 3] : null;
            output.WriteByte(IsLess);
            BeamCompactWriter.Write(output, LabelTag, next.Value);
            WriteMapJoinOperand(output, low with { Value = tail.Value });
            WriteMapJoinOperand(output, info is null ? bound : annotateBound?.Invoke(info, bound) ?? bound);
            output.Write(code.Span[body[split + 6].Offset..body[split + 8].Offset]);
            output.WriteByte(IsGreaterOrEqual);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            WriteMapJoinOperand(output, info is null ? limit : annotateBound?.Invoke(info, limit) ?? limit);
            WriteMapJoinOperand(output, high with { Value = tail.Value });
            copied = body[split + 10].Offset;
            split += 12;

        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
