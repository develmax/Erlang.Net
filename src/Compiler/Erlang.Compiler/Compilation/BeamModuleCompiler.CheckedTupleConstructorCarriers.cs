using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record CheckedTupleCarrierPlan(
        int AllocationOrdinal,
        int FirstProjection,
        int AfterProjections,
        int Tuple,
        int CarrierSlot,
        (int Instruction, int Slot, int Field, int Scratch)[] Uses,
        CheckedTupleChildDelivery? Child = null,
        CheckedTupleOriginalChildDelivery? OriginalChild = null
    );

    private byte[]? RetainCheckedTupleConstructorCarriers(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestArity)
            || !code.Span.Contains(PutList) && !code.Span.Contains(PutTuple))
        {
            return null;
        }

        var ordinary = Rewrite(completeTrims: false, code);

        var parents = Rewrite(completeTrims: true, ordinary is null ? code : ordinary) ?? ordinary;

        return Rewrite(completeTrims: true, parents is null ? code : parents, deliverChild: true) ?? parents;

        byte[]? Rewrite(bool completeTrims, ReadOnlyMemory<byte> original, bool deliverChild = false)
        {
            if (DecodeLateAtomicProjectionSlots(original, labelCount) is not { } decoded)
            {
                return null;
            }

            var body = decoded.Body;
            var allocations = new Dictionary<int, int>();
            using var output = new MemoryStream();
            int copied = 0;
            int start = 0;
            for (int end = 0; end < body.Count; end++)
            {
                if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    continue;
                }

                int entry = start;
                start = end + 1;
                if (entry >= end || body[entry] is not { Definition.Opcode: Label, Operands: [var label] }
                    || (deliverChild
                        ? PlanRenamedCheckedTupleChild(
                            body,
                            entry,
                            end,
                            completeTrims
                        ) ?? PlanOriginalCheckedTupleChild(
                            body,
                            entry,
                            end,
                            completeTrims
                        ) : PlanCheckedTupleConstructorCarrier(
                            body,
                            entry,
                            end,
                            completeTrims
                        )) is not { } plan)
                {
                    continue;
                }

                int first = body[plan.FirstProjection].Offset;
                output.Write(original.Span[copied..first]);
                if (plan.OriginalChild is { } preserved)
                {
                    int attributes = body[preserved.AttributesProjection].Offset;
                    int afterAttributes = body[preserved.AttributesProjection + 1].Offset;
                    output.Write(original.Span[attributes..afterAttributes]);
                }

                output.WriteByte(Move);
                BeamCompactWriter.Write(output, XRegisterTag, plan.Tuple);
                BeamCompactWriter.Write(output, YRegisterTag, plan.CarrierSlot);
                copied = body[plan.AfterProjections].Offset;
                allocations.Add((int)label.Value, plan.AllocationOrdinal);
                if (plan.Child is { } delivery)
                {
                    WriteCarrierClear(output, delivery.NameSlot);
                    int comparison = body[delivery.BeforeNameTest].Offset;
                    output.Write(original.Span[copied..comparison]);
                    WriteCarrierField(
                        output,
                        plan.CarrierSlot,
                        delivery.NameField,
                        YRegisterTag,
                        delivery.NameSlot
                    );

                    int content = body[delivery.ContentLoad].Offset;
                    output.Write(original.Span[comparison..content]);
                    WriteCarrierField(
                        output,
                        plan.CarrierSlot,
                        delivery.ContentField,
                        XRegisterTag,
                        0
                    );
                    copied = body[delivery.ContentLoad + 1].Offset;
                }

                if (plan.OriginalChild is { } originalChild)
                {
                    int comparison = body[originalChild.BeforeNameTest].Offset;
                    int content = body[originalChild.ContentLoad].Offset;
                    output.Write(original.Span[copied..comparison]);
                    WriteCarrierField(
                        output,
                        plan.CarrierSlot,
                        originalChild.NameField,
                        XRegisterTag,
                        1
                    );
                    int nameOperand = FirstStackOperandAtOrAfter(decoded.StackOperands, comparison);
                    if (nameOperand >= decoded.StackOperands.Count)
                    {
                        return null;
                    }

                    var (operandStart, operandEnd, slot) = decoded.StackOperands[nameOperand];
                    if (operandStart >= content || operandEnd != content || slot != plan.CarrierSlot)
                    {
                        return null;
                    }

                    output.Write(original.Span[comparison..operandStart]);
                    BeamCompactWriter.Write(output, XRegisterTag, 1);
                    WriteCarrierField(
                        output,
                        plan.CarrierSlot,
                        originalChild.ContentField,
                        XRegisterTag,
                        0
                    );
                    output.WriteByte(Move);
                    BeamCompactWriter.Write(output, XRegisterTag, 1);
                    BeamCompactWriter.Write(output, YRegisterTag, plan.CarrierSlot);
                    copied = body[originalChild.ContentLoad + 1].Offset;
                }

                foreach (var group in plan.Uses.GroupBy(use => use.Instruction))
                {
                    int at = group.Key;
                    int offset = body[at].Offset;
                    output.Write(original.Span[copied..offset]);
                    copied = offset;
                    foreach (var use in group)
                    {
                        WriteCarrierField(
                            output,
                            plan.CarrierSlot,
                            use.Field,
                            XRegisterTag,
                            use.Scratch
                        );
                    }

                    int next = body[at + 1].Offset;
                    int operand = FirstStackOperandAtOrAfter(decoded.StackOperands, offset);
                    int replaced = 0;
                    while (operand < decoded.StackOperands.Count && decoded.StackOperands[operand].Start < next)
                    {
                        var (operandStart, operandEnd, slot) = decoded.StackOperands[operand++];
                        var use = group.FirstOrDefault(item => item.Slot == slot);
                        if (use.Instruction != at)
                        {
                            continue;
                        }

                        output.Write(original.Span[copied..operandStart]);
                        BeamCompactWriter.Write(output, XRegisterTag, use.Scratch);
                        copied = operandEnd;
                        replaced++;
                    }

                    if (replaced != group.Count())
                    {
                        return null;
                    }
                }

                if (plan.OriginalChild is { } originalCold)
                {
                    int coldMove = body[originalCold.ColdMove].Offset;
                    output.Write(original.Span[copied..coldMove]);
                    copied = body[originalCold.ColdMove + 1].Offset;
                }

                if (plan.Child is { } cold)
                {
                    int clear = body[cold.ClearBefore].Offset;
                    output.Write(original.Span[copied..clear]);
                    WriteCarrierClear(output, plan.CarrierSlot);
                    copied = clear;
                }
            }

            if (allocations.Count == 0)
            {
                return null;
            }

            output.Write(original.Span[copied..]);
            byte[] rewritten = output.ToArray();
            if (DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
            {
                return null;
            }

            return CompactCheckedTupleCarrierRegions(
                rewritten,
                updated.Body,
                updated.StackOperands,
                allocations,
                completeTrims
            );
        }
    }

    private static void WriteCarrierClear(Stream output, int slot)
    {
        output.WriteByte(InitYRegisters);
        output.WriteByte(ExtendedListOperand);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
        BeamCompactWriter.Write(output, YRegisterTag, slot);
    }

    private static void WriteCarrierField(
        Stream output,
        int carrier,
        int field,
        int tag,
        int destination
    )
    {
        output.WriteByte(GetTupleElement);
        BeamCompactWriter.Write(output, YRegisterTag, carrier);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field);
        BeamCompactWriter.Write(output, tag, destination);
    }

    private static byte[]? CompactCheckedTupleCarrierRegions(
        byte[] code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        IReadOnlyDictionary<int, int> allocations,
        bool completeTrims
    )
    {
        var regions = new List<BeamInstruction[]>();
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (entry >= end || body[entry] is not { Definition.Opcode: Label, Operands: [var label] }
                || !allocations.TryGetValue((int)label.Value, out int ordinal))
            {
                continue;
            }

            int frame = Enumerable.Range(entry, end - entry)
                .Where(index => body[index].Definition.Opcode is Allocate or AllocateHeap).ElementAt(ordinal);
            if (ReadClosedCheckedTupleFrame(
                body,
                entry,
                end,
                frame,
                completeTrims
            ) is not { } region || PlanDisjointFramesCore(
                region,
                0,
                region.Length - 1,
                atomicOnly: false,
                completeTrimsOnly: completeTrims
            ) is not { } frames || frames[1] is not { } layout
                || layout.Size >= body[frame].Operands[0].Value)
            {
                return null;
            }

            regions.Add(region);
        }

        if (regions.Count != allocations.Count)
        {
            return null;
        }

        // A later region never shifts the saved byte offsets of an earlier one.
        foreach (var region in regions.AsEnumerable().Reverse())
        {
            if (RewriteDisjointFrames(
                code,
                region,
                stackOperands,
                atomicOnly: false,
                completeTrimsOnly: completeTrims
            ) is not { } compacted)
            {
                return null;
            }

            code = compacted;
        }

        return code;
    }

    private static BeamInstruction[]? ReadClosedCheckedTupleFrame(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        int frame,
        bool completeTrims
    )
    {
        var labels = Enumerable.Range(entry, end - entry)
            .Where(index => body[index].Definition.Opcode == Label)
            .ToDictionary(index => (int)body[index].Operands[0].Value);
        int furthest = frame;
        int limit = end;
        for (int index = frame; index < end; index++)
        {
            foreach (int target in CheckedTupleFrameTargets(body[index]))
            {
                if (!labels.TryGetValue(target, out int destination) || destination <= index)
                {
                    return null;
                }

                furthest = Math.Max(furthest, destination);
            }

            if (index >= furthest && (EndsBeamBlock(body[index].Definition.Opcode)
                || body[index].Definition.Opcode == IfEnd))
            {
                limit = index + 1;
                break;
            }
        }

        // The allocation has no label and can only be entered via its checked
        // fallthrough. Any label within this owner must be private to it.
        if (Enumerable.Range(frame + 1, limit - frame - 1).Any(index =>
                body[index].Definition.Opcode is Allocate or AllocateHeap)
            || Enumerable.Range(frame, limit - frame).Any(index => body[index].Definition.Opcode == TrimFrame) != completeTrims
            || Enumerable.Range(entry, end - entry).Where(index => index < frame || index >= limit)
                .SelectMany(index => CheckedTupleFrameTargets(body[index])).Any(target =>
                    labels.TryGetValue(target, out int location) && location >= frame && location < limit))
        {
            return null;
        }

        // Give the existing owner and frame compaction walkers a local entry
        // and terminator. Real byte offsets in the retained region are global.
        var region = new BeamInstruction[limit - frame + 2];
        region[0] = body[entry];
        for (int index = frame; index < limit; index++)
        {
            region[index - frame + 1] = body[index];
        }

        region[^1] = new BeamInstruction(body[limit].Offset, body[^1].Definition, []);

        return region;
    }

    private static IEnumerable<int> CheckedTupleFrameTargets(BeamInstruction instruction) =>
        instruction.Definition.Opcode is LocalCall or TailLocalCall or TailLocalCallWithFrame
            ? []
            : instruction.Operands
                .Where(operand => operand.Kind == BeamOperandParameters.Label && operand.Value.Sign > 0)
                .Select(operand => (int)operand.Value);

    private static CheckedTupleCarrierPlan? PlanCheckedTupleConstructorCarrier(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        bool completeTrims
    )
    {
        int allocationOrdinal = -1;
        for (int frame = entry; frame < end; frame++)
        {
            if (body[frame].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                continue;
            }

            allocationOrdinal++;
            if (frame < entry + 2 || body[frame] is not { Definition.Opcode: Allocate, Operands: [_, var live] }
                || body[frame - 1] is not
                {
                    Definition.Opcode: TestArity,
                    Operands: [var failure, { Kind: BeamOperandParameters.X, TypeIndex: null } tuple, var arity]
                }
                || body[frame - 2] is not { Definition.Opcode: IsTuple, Operands: [var otherFailure, var tested] }
                || failure != otherFailure || tested != tuple || tuple.Value >= live.Value
                || arity.Value < 2 || arity.Value >= FirstReservedXRegister)
            {
                continue;
            }

            int width = (int)arity.Value;
            int first = frame + 1;
            while (first < end && IsFrameSetup(body[first]) && !ReadsCarrierField(body[first], tuple))
            {
                first++;
            }

            if (first + width >= end)
            {
                continue;
            }

            var slots = new Dictionary<int, int>();
            for (int index = 0; index < width; index++)
            {
                var projection = body[first + index];
                if (!ReadsCarrierField(projection, tuple) || projection.Operands[1].Value != index
                    || !slots.TryAdd((int)projection.Operands[2].Value, index))
                {
                    break;
                }
            }

            if (slots.Count != width)
            {
                continue;
            }

            if (ReadClosedCheckedTupleFrame(
                body,
                entry,
                end,
                frame,
                completeTrims
            ) is not { } region || ReadDisjointFrameOwnership(
                region,
                0,
                region.Length - 1,
                allowCompleteTrims: completeTrims
            ) is not { } owner || !owner.Uses.ContainsKey(1)
                || Enumerable.Range(first - frame + 1, width).Any(index => owner.Owners[index] != 1)
                || ReadCheckedTupleConstructorUses(
                    region,
                    0,
                    region.Length - 1,
                    1,
                    first - frame + 1,
                    slots,
                    owner.Owners
                ) is not { } uses)
            {
                continue;
            }

            return new CheckedTupleCarrierPlan(
                allocationOrdinal,
                first,
                first + width,
                (int)tuple.Value,
                (int)body[first].Operands[2].Value,
                uses.Select(use => (use.Instruction + frame - 1, use.Slot, use.Field, use.Scratch)).ToArray()
            );
        }

        return null;

        static bool IsFrameSetup(BeamInstruction instruction) =>
            instruction.Definition.Opcode == InitYRegisters || instruction is
            {
                Definition.Opcode: Move or GetTupleElement,
                Operands: [.., { Kind: BeamOperandParameters.Y }]
            };

        static bool ReadsCarrierField(BeamInstruction instruction, BeamOperand tuple) =>
            instruction is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var source, _, { Kind: BeamOperandParameters.Y, TypeIndex: null }]
            } && source == tuple;
    }

    private static (int Instruction, int Slot, int Field, int Scratch)[]? ReadCheckedTupleConstructorUses(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        int frame,
        int first,
        IReadOnlyDictionary<int, int> slots,
        int[] owners
    )
    {
        var seen = new HashSet<int>();
        var uses = new List<(int Instruction, int Slot, int Field)>();
        for (int index = frame + 1; index < end; index++)
        {
            if (owners[index - entry] != frame || index >= first && index < first + slots.Count
                || index < first && body[index].Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            BeamInstruction instruction = body[index];
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (operand.Kind != BeamOperandParameters.Y || !slots.TryGetValue((int)operand.Value, out int field))
                {
                    continue;
                }

                int slot = (int)operand.Value;
                if (operand.TypeIndex is not null || index < first || !seen.Add(slot)
                    || !(instruction.Definition.Opcode == PutTuple && position >= 2
                        || instruction.Definition.Opcode == PutList && position < 2))
                {
                    return null;
                }

                uses.Add((index, slot, field));
            }
        }

        if (uses.Count != slots.Count)
        {
            return null;
        }

        int heap = uses[0].Instruction - 1;
        while (heap > first && IsCheckedTupleConstructorInstruction(body[heap].Definition.Opcode))
        {
            heap--;
        }

        if (heap <= first || body[heap].Definition.Opcode != TestHeap
            || body[heap - 1].Definition.Opcode is not (LocalCall or ExternalCall))
        {
            return null;
        }

        int finish = heap + 1;
        while (finish < end && IsCheckedTupleConstructorInstruction(body[finish].Definition.Opcode))
        {
            finish++;
        }

        if (finish + 1 >= end || body[finish].Definition.Opcode != Deallocate
            || body[finish + 1].Definition.Opcode != Return || uses[^1].Instruction >= finish
            || Enumerable.Range(heap, finish - heap + 1).Any(index => owners[index - entry] != frame))
        {
            return null;
        }

        // No branch or safepoint follows the reservation. Use only X registers
        // untouched by the straight-line constructor and reserve its return X0.
        var unavailable = Enumerable.Range(heap + 1, finish - heap)
            .SelectMany(index => body[index].Operands)
            .Where(operand => operand.Kind == BeamOperandParameters.X)
            .Select(operand => (int)operand.Value).ToHashSet();
        var byInstruction = uses.GroupBy(use => use.Instruction).ToArray();
        int needed = byInstruction.Max(group => group.Count());
        int[] scratch = Enumerable.Range(1, FirstReservedXRegister - 1)
            .Where(register => !unavailable.Contains(register)).Take(needed).ToArray();

        return scratch.Length != needed ? null
            : byInstruction.SelectMany(group => group.Select((use, index) =>
                (use.Instruction, use.Slot, use.Field, scratch[index]))).ToArray();
    }

    private static bool IsCheckedTupleConstructorInstruction(byte opcode) =>
        opcode is Move or GetTupleElement or PutTuple or PutList or UpdateRecord or Line;
}
