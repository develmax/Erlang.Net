using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record CheckedEntryCarrierFrame(
        int Allocation,
        int Projection,
        int StateCheck,
        int Cleanup,
        int Failure,
        BeamOperand Tag,
        BeamOperand State,
        BeamOperand Scratch
    );

    private byte[]? DelayCheckedEntryCarrierFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectTupleArity)
            || !code.Span.Contains(Allocate) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        var edits = new SortedDictionary<int, (int End, byte[] Bytes, int? AllocationInEdit)>();
        for (int info = 0; info + 4 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || arity.Value < 2 || arity.Value >= FirstReservedXRegister || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not { Definition.Opcode: IsTuple, Operands: [_, var input] }
                || !IsBareX0(input) || body[info + 3] is not
                {
                    Definition.Opcode: SelectTupleArity,
                    Operands: [var selected, var noMatch, _, ..]
                } selector || !SameProjectionRegister(input, selected)
                || !HasUnprotectedCallKillSchedule(body, info + 1)
                || !(flow ??= new BeamProjectionFlow(body)).TryGetForwardLabel(info + 3, (int)noMatch.Value, out int defaultArm))
            {
                continue;
            }

            int end = info + 4;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            if (defaultArm >= end)
            {
                continue;
            }

            for (int option = 3; option + 1 < selector.Operands.Count; option += 2)
            {
                if (!flow!.TryGetSingleForwardTarget(info + 3, (int)selector.Operands[option + 1].Value, out int arm)
                    || selector.Operands[option].Value > int.MaxValue
                    || ReadCheckedEntryCarrierFrame(
                        body,
                        arm,
                        end,
                        defaultArm,
                        (int)arity.Value,
                        (int)selector.Operands[option].Value,
                        flow
                    ) is not { } plan || edits.ContainsKey(body[plan.Cleanup].Offset))
                {
                    continue;
                }

                using var prefix = new MemoryStream();
                prefix.WriteByte(GetTupleElement);
                WriteRegister(prefix, input);
                BeamCompactWriter.Write(prefix, BeamWire.UnsignedTag, 0);
                WriteRegister(prefix, plan.Scratch);
                prefix.WriteByte(ExactEqualTest);
                BeamCompactWriter.Write(prefix, LabelTag, plan.Failure);
                WriteRegister(prefix, plan.Scratch);
                BeamCompactWriter.Write(prefix, AtomTag, plan.Tag.Value);
                prefix.WriteByte(IsTaggedTuple);
                BeamCompactWriter.Write(prefix, LabelTag, plan.Failure);
                WriteRegister(prefix, plan.State);
                BeamCompactWriter.Write(prefix, BeamWire.UnsignedTag, body[plan.StateCheck].Operands[2].Value);
                BeamCompactWriter.Write(prefix, AtomTag, body[plan.StateCheck].Operands[3].Value);

                int allocation = body[plan.Allocation].Offset;
                edits.Add(
                    allocation,
                    (
                    body[plan.Projection].Offset,
                    [.. prefix.ToArray(), .. code.Span[allocation..body[plan.Projection].Offset]],
                    (int)prefix.Length
                )
                );
                edits.Add(body[plan.Projection].Offset, (body[plan.Projection + 2].Offset, [], null));
                edits.Add(body[plan.StateCheck].Offset, (body[plan.StateCheck + 1].Offset, [], null));
                edits.Add(body[plan.Cleanup].Offset, (body[plan.Cleanup + 5].Offset, [], null));
            }

            info = end - 1;
        }

        if (edits.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        var relocatedAllocations = new HashSet<int>();
        foreach (var (start, edit) in edits)
        {
            if (start < copied)
            {
                return null;
            }

            output.Write(code.Span[copied..start]);
            if (edit.AllocationInEdit is int allocation)
            {
                relocatedAllocations.Add((int)output.Length + allocation);
            }

            output.Write(edit.Bytes);
            copied = edit.End;
        }

        output.Write(code.Span[copied..]);

        // The removed framed relay used to end in deallocate+jump. Retry
        // value coloring only for allocations whose relay was removed.
        byte[] moved = output.ToArray();

        return CompactFramesBesideSiblingTrims(moved, labelCount, relocatedAllocations) ?? moved;
    }

    private static CheckedEntryCarrierFrame? ReadCheckedEntryCarrierFrame(
        IReadOnlyList<BeamInstruction> body,
        int arm,
        int end,
        int fallback,
        int arguments,
        int tupleArity,
        BeamProjectionFlow flow
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        int allocation = arm + 1;
        if (arm + 8 >= end || body[allocation] is not { Definition.Opcode: Allocate, Operands: [var size, var live] }
            || live.Value != arguments || arguments >= FirstReservedXRegister)
        {
            return null;
        }

        int save = allocation + 1;
        if (body[save].Definition.Opcode == InitYRegisters)
        {
            save++;
        }

        if (body[save] is not { Definition.Opcode: Move, Operands: [var eventArg, var eventSlot] }
            || eventArg != x0 || !IsEntryCarrierSlot(eventSlot, size)
            || body[save + 1] is not { Definition.Opcode: Move, Operands: [var stateArg, var stateSlot] }
            || !IsBareX(stateArg) || stateArg == x0 || stateArg.Value >= arguments
            || !IsEntryCarrierSlot(stateSlot, size) || stateSlot == eventSlot)
        {
            return null;
        }

        int projection = save + 2;
        int tagCheck = projection + 1;
        if (body[projection] is not
            { Definition.Opcode: GetTupleElement, Operands: [var carrier, { Value.IsZero: true }, var oldTag] }
            || carrier != eventSlot || !IsBareX(oldTag) || oldTag.Value >= live.Value
            || body[tagCheck] is not
            { Definition.Opcode: ExactEqualTest, Operands: [var reject, var test, { Kind: BeamOperandParameters.Atom } tag] }
            || test != oldTag || reject.Value.IsZero || tupleArity < 1)
        {
            return null;
        }

        int stateCheck = tagCheck + 1;
        while (stateCheck < end && body[stateCheck] is
            { Definition.Opcode: GetTupleElement, Operands: [var tuple, var field, var result] }
            && tuple == eventSlot && field.Value < tupleArity && IsEntryCarrierSlot(result, size)
            && result != eventSlot && result != stateSlot)
        {
            stateCheck++;
        }

        var scratch = new BeamOperand(BeamOperandParameters.X, arguments);
        if (stateCheck >= end || body[stateCheck] is not
            { Definition.Opcode: IsTaggedTuple, Operands: [var badState, var state, _, { Kind: BeamOperandParameters.Atom }] }
            || state != stateSlot || badState != reject
            || !flow.TryGetExclusiveForwardTarget(
                tagCheck,
                stateCheck,
                (int)reject.Value,
                out int cleanup
            )
            || cleanup + 5 > fallback || cleanup + 5 >= end
            || !IsMove(body[cleanup + 1], eventSlot, eventArg) || !IsMove(body[cleanup + 2], stateSlot, stateArg)
            || body[cleanup + 3] is not { Definition.Opcode: Deallocate, Operands: [var release] } || release != size
            || body[cleanup + 4] is not { Definition.Opcode: Jump, Operands: [var target] }
            || target.Value != body[fallback].Operands[0].Value
            || !flow.IsFallthroughTemporaryDead(arm, scratch, trackUnprotectedBifs: true)
            || !flow.IsFallthroughTemporaryDead(stateCheck, oldTag, trackUnprotectedBifs: true)
            || !flow.IsEntryTemporaryDead(fallback, scratch))
        {
            return null;
        }

        return new(
            allocation,
            projection,
            stateCheck,
            cleanup,
            (int)target.Value,
            tag,
            stateArg,
            scratch
        );
    }

    private static bool IsEntryCarrierSlot(BeamOperand slot, BeamOperand size) =>
        slot is { Kind: BeamOperandParameters.Y, TypeIndex: null } && slot.Value < size.Value;
}
