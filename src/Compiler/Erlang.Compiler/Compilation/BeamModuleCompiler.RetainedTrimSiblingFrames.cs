using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record RetainedTrimSiblingPlan(
        (int Size, int[] Slots)?[] Layouts,
        int InitialClear,
        int[] InitialSaveSlots,
        Dictionary<int, (BeamOperand Source, BeamOperand Destination)[]> Copies
    );

    private byte[]? CompactRetainedTrimSiblingFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteDisjointFrameLayouts(
            code,
            decoded.Body,
            decoded.StackOperands,
            atomicOnly: false,
            completeTrimsOnly: false,
            entriesToCompact: null,
            reuseOccupiedEntrySlots: false,
            onlyAllocation: null,
            compactRetainedTrimSiblings: true
        );
    }

    private byte[]? CompactLoadEntryTrimSiblingFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(OnLoad)
            || !code.Span.Contains(TrimFrame) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteDisjointFrameLayouts(
            code,
            decoded.Body,
            decoded.StackOperands,
            atomicOnly: false,
            completeTrimsOnly: false,
            entriesToCompact: null,
            reuseOccupiedEntrySlots: false,
            onlyAllocation: null,
            compactRetainedTrimSiblings: true,
            loadEntriesOnly: true
        );
    }

    private static RetainedTrimSiblingPlan? ReadRetainedTrimSiblingPlan(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        bool loadEntriesOnly
    )
    {
        if (start + 8 >= end || loadEntriesOnly && (start == 0 || body[start + 1].Definition.Opcode != OnLoad
                || body[start - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, { Value.IsZero: true }] })
            || !HasUnprotectedCallKillSchedule(body, start)
            || ReadDisjointFrameOwnership(
                body,
                start,
                end,
                trackSiblingTrimEpochs: true
            ) is not { } frame)
        {
            return null;
        }

        int[] allocations = frame.Uses.Keys.Where(index => body[index].Definition.Opcode == Allocate).ToArray();
        int[] trims = frame.Uses.Keys.Where(index => body[index].Definition.Opcode == TrimFrame).ToArray();
        if (frame.Uses.Count != 2 || allocations is not [var allocation] || trims is not [var trim]
            || trim <= allocation + 2 || body[trim].Operands is not [_, { Value.IsOne: true }]
            || !frame.Owners.Skip(trim - start + 1).Contains(allocation)
            || loadEntriesOnly && (allocation != start + 2 || !body[allocation].Operands[1].Value.IsZero))
        {
            return null;
        }

        int clear = allocation + 1;
        bool hasClosedEntrySave = loadEntriesOnly && body[clear] is
        {
            Definition.Opcode: Move,
            Operands:
            [
            {
                Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil
                    or BeamOperandParameters.LiteralValue, TypeIndex: null
            },
            { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null }
            ]
        };
        if (hasClosedEntrySave)
        {
            clear++;
        }

        if (body[clear].Definition.Opcode != InitYRegisters)
        {
            return null;
        }

        int cleanup = trim + 1;
        while (cleanup < end && body[cleanup].Definition.Opcode == Line)
        {
            cleanup++;
        }

        if (cleanup + 3 >= end || body[cleanup] is not
            { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
            || arity.Value != (loadEntriesOnly ? 2 : 1)
            || body[cleanup + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null, Value.IsZero: true },
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
                ]
            }
            || body[cleanup + 2].Definition.Opcode != Deallocate || body[cleanup + 3].Definition.Opcode != Return)
        {
            return null;
        }

        int width = frame.Uses[allocation].Length;
        int live = (int)body[allocation].Operands[1].Value;
        var saves = new List<(int Source, int Slot)>();
        int last = clear + 1;
        int previous = -1;
        while (last < trim && body[last] is
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.X, TypeIndex: null } input,
                { Kind: BeamOperandParameters.Y, TypeIndex: null } target
                ]
            } && target.Value == saves.Count && input.Value > previous && input.Value < live)
        {
            previous = (int)input.Value;
            saves.Add((previous, (int)target.Value));
            last++;
        }

        int initialized = hasClosedEntrySave ? 1 : saves.Count;
        var initial = body[clear].Operands.Skip(1).Select(slot => (int)slot.Value).ToHashSet();
        if ((!loadEntriesOnly && saves.Count < 2) || !initial.SetEquals(Enumerable.Range(initialized, width - initialized))
            || PlanBranchStackSlots(
                body,
                start,
                end,
                linearAllocation: allocation,
                allowGcBifExceptionExit: true,
                trimEpochOwners: frame.Owners
            ) is not { Trim: { Kept: 1 } } plan
            || !loadEntriesOnly && plan.Slots.Any(slot => slot < 0)
            || plan.Slots[width - 1] != plan.Count - 1)
        {
            return null;
        }

        var ordered = saves.Select(save => (save.Source, Slot: plan.Slots[save.Slot]))
            .OrderBy(save => save.Slot).ToArray();
        if (ordered.Select(save => save.Slot).Distinct().Count() != ordered.Length)
        {
            return null;
        }

        var layouts = new (int Size, int[] Slots)?[end - start];
        for (int index = allocation; index < end; index++)
        {
            if (index == allocation || frame.Owners[index - start] == allocation)
            {
                layouts[index - start] = (plan.Count, plan.Slots);
            }
        }

        if (loadEntriesOnly)
        {
            // The zero-input entry has either one preserved literal or only
            // cleared slots. Cold siblings still own the frame before trim.
            return new(
                layouts,
                clear,
                hasClosedEntrySave ? [plan.Slots[0]] : [],
                []
            );
        }

        var copies = new Dictionary<int, (BeamOperand Source, BeamOperand Destination)[]>
        {
            [allocation + 2] = ordered.Select(save =>
                (new BeamOperand(BeamOperandParameters.X, save.Source), new BeamOperand(BeamOperandParameters.Y, save.Slot)))
                .ToArray()
        };
        AddCompactedStackRestores(
            body,
            start,
            end,
            allocation,
            frame.Owners,
            plan.Slots,
            copies
        );

        // The owned suffix still keeps a single Y0. Its existing references
        // and frame release stay unchanged after the rewritten partial trim.
        return new(
            layouts,
            allocation + 1,
            ordered.Select(save => save.Slot).ToArray(),
            copies
        );
    }

    private static void AddCompactedStackRestores(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int allocation,
        IReadOnlyList<int> owners,
        IReadOnlyList<int> slots,
        Dictionary<int, (BeamOperand Source, BeamOperand Destination)[]> copies
    )
    {
        for (int call = allocation + 1; call < end; call++)
        {
            if (owners[call - start] != allocation || body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
                || arity.Value < 3 || arity.Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int beforeLine = call;
            while (beforeLine > allocation && body[beforeLine - 1].Definition.Opcode == Line)
            {
                beforeLine--;
            }

            int count = (int)arity.Value;
            int first = beforeLine - count - 1;
            if (first < allocation + 4 || body[first - 3] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var value, { Kind: BeamOperandParameters.Y, TypeIndex: null } retained]
                } || !IsBareX0(value) || body[first - 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var tested, var tupleArity, _]
                } || !IsBareX0(tested) || body[first - 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var tuple, var field, { Kind: BeamOperandParameters.Y, TypeIndex: null } receiver]
                } || !IsBareX0(tuple) || receiver == retained || field.Value >= tupleArity.Value
                || ReadSplitStackArgumentOrder(
                    body,
                    beforeLine,
                    count,
                    ascendingDestinations: true
                ) is not { } reads || body[beforeLine - 1].Operands[0] != receiver)
            {
                continue;
            }

            // The independent Y-to-X moves precede both the root clear and
            // the last receiver move. Sort the block by the new physical Y.
            copies.Add(
                first,
                reads.Select(index =>
                (Source: body[index].Operands[0] with { Value = slots[(int)body[index].Operands[0].Value] },
                 Destination: body[index].Operands[1]))
                .OrderBy(move => move.Source.Value).ToArray()
            );
        }
    }
}
