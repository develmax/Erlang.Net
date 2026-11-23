using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record CheckedTupleChildDelivery(
        int BeforeNameTest,
        int NameField,
        int NameSlot,
        int ContentLoad,
        int ContentField,
        int ClearBefore
    );

    private sealed record CheckedTupleOriginalChildDelivery(
        int BeforeNameTest,
        int NameField,
        int ContentLoad,
        int ContentField,
        int AttributesProjection,
        int ColdMove
    );

    private static CheckedTupleCarrierPlan? PlanRenamedCheckedTupleChild(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        bool completeTrims
    ) => PlanCheckedTupleChild(
        body,
        entry,
        end,
        completeTrims,
        originalName: false
    );

    private static CheckedTupleCarrierPlan? PlanOriginalCheckedTupleChild(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        bool completeTrims
    ) => PlanCheckedTupleChild(
        body,
        entry,
        end,
        completeTrims,
        originalName: true
    );

    private static CheckedTupleCarrierPlan? PlanCheckedTupleChild(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        bool completeTrims,
        bool originalName
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
            if (body[frame] is not { Definition.Opcode: Allocate, Operands: [_, var live] })
            {
                continue;
            }

            int first = frame + 1;
            while (first < end && body[first].Definition.Opcode is InitYRegisters or Move)
            {
                if (body[first].Definition.Opcode == Move && body[first].Operands[1].Kind != BeamOperandParameters.Y)
                {
                    break;
                }

                first++;
            }

            const int childArity = 3;
            if (first + childArity >= end || body[first] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } tuple, _, _]
                } || tuple.Value >= live.Value || !HasCheckedChildTuple(
                    body,
                    entry,
                    frame,
                    tuple
                ))
            {
                continue;
            }

            var fields = new Dictionary<int, int>();
            for (int field = 0; field < childArity; field++)
            {
                if (body[first + field] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var source, var position, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || source != tuple || position.Value != field || !fields.TryAdd((int)saved.Value, field))
                {
                    break;
                }
            }

            if (fields.Count != childArity || ReadClosedCheckedTupleFrame(
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
            ) is not { } owner)
            {
                continue;
            }

            int offset = frame - 1;
            int firstLocal = first - offset;
            if (originalName)
            {
                if (ReadOriginalCheckedTupleChildDelivery(
                    region,
                    firstLocal,
                    fields,
                    owner.Owners
                ) is not { } original)
                {
                    continue;
                }

                return new CheckedTupleCarrierPlan(
                    allocationOrdinal,
                    first,
                    first + childArity,
                    (int)tuple.Value,
                    original.Carrier,
                    [],
                    OriginalChild: original.Delivery with
                    {
                        BeforeNameTest = original.Delivery.BeforeNameTest + offset,
                        ContentLoad = original.Delivery.ContentLoad + offset,
                        AttributesProjection = original.Delivery.AttributesProjection + offset,
                        ColdMove = original.Delivery.ColdMove + offset
                    }
                );
            }

            if (ReadCheckedTupleChildDelivery(
                region,
                firstLocal,
                fields,
                owner.Owners
            ) is not { } plan)
            {
                continue;
            }

            return new CheckedTupleCarrierPlan(
                allocationOrdinal,
                first,
                first + childArity,
                (int)tuple.Value,
                plan.Carrier,
                [(plan.Use.Instruction + offset, plan.Use.Slot, plan.Use.Field, plan.Use.Scratch)],
                plan.Delivery with
                {
                    BeforeNameTest = plan.Delivery.BeforeNameTest + offset,
                    ContentLoad = plan.Delivery.ContentLoad + offset,
                    ClearBefore = plan.Delivery.ClearBefore + offset
                }
            );
        }

        return null;
    }

    private static bool HasCheckedChildTuple(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int frame,
        BeamOperand tuple
    )
    {
        for (int index = frame - 1; index > entry; index--)
        {
            var instruction = body[index];
            if (instruction is { Definition.Opcode: TestArity, Operands: [var failure, var source, var arity] }
                && source == tuple)
            {
                return arity.Value == 3
                    && body[index - 1] is { Definition.Opcode: IsTuple, Operands: [var otherFailure, var tested] }
                    && failure == otherFailure && tested == source;
            }

            if (!(instruction.Definition.Opcode is IsTuple or TestArity or IsNonemptyList
                || instruction.Definition.Opcode == GetList && instruction.Operands.Skip(1).All(operand => !SameProjectionRegister(operand, tuple))))
            {
                return false;
            }
        }

        return false;
    }

    private static (int Carrier, (int Instruction, int Slot, int Field, int Scratch) Use, CheckedTupleChildDelivery Delivery)?
        ReadCheckedTupleChildDelivery(
            IReadOnlyList<BeamInstruction> region,
            int first,
            IReadOnlyDictionary<int, int> fields,
            int[] owners
        )
    {
        int end = region.Count - 1;
        int firstCall = CheckedTupleChildFirstCall(region, first + fields.Count);
        int comparison = firstCall + 1;
        if (comparison >= end || !IsCheckedTupleChildUnaryCall(region[firstCall])
            || region[comparison] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [
                { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } mismatch, var result,
                { Kind: BeamOperandParameters.Y, TypeIndex: null } name
                ]
            } || !IsBareX0(result) || !fields.TryGetValue((int)name.Value, out int nameField)
            || Enumerable.Range(first + fields.Count, firstCall - first - fields.Count + 1)
                .Any(index => region[index].Operands.Any(operand => SameProjectionRegister(operand, name))))
        {
            return null;
        }

        int nameSlot = (int)name.Value;
        foreach (int contentSlot in fields.Keys.Where(slot => slot != nameSlot))
        {
            int contentField = fields[contentSlot];
            int attributes = fields.Keys.Single(slot => slot != nameSlot && slot != contentSlot);
            int attributesField = fields[attributes];
            int contentLoad = -1;
            for (int index = first + fields.Count; index < end; index++)
            {
                if (owners[index] != 1 || !region[index].Operands.Any(operand =>
                    operand.Kind == BeamOperandParameters.Y && operand.Value == contentSlot))
                {
                    continue;
                }

                if (contentLoad >= 0 || index <= comparison || region[index] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null }, var outgoing]
                    } || !IsBareX0(outgoing))
                {
                    contentLoad = -1;
                    break;
                }

                contentLoad = index;
            }

            if (contentLoad < 0 || region[contentLoad - 1] is not
                {
                    Definition.Opcode: Move, Operands: [var renamed, var updated]
                } || !IsBareX0(renamed) || updated != name)
            {
                continue;
            }

            int contentCall = contentLoad + 1;
            while (contentCall < end && region[contentCall].Definition.Opcode == Line)
            {
                contentCall++;
            }

            var finalField = new Dictionary<int, int> { [attributes] = attributesField };
            if (contentCall + 1 >= end || !IsCheckedTupleChildUnaryCall(region[contentCall])
                || region[contentCall + 1].Definition.Opcode != TestHeap
                || ReadCheckedTupleConstructorUses(
                    region,
                    0,
                    end,
                    1,
                    first + attributesField,
                    finalField,
                    owners
                ) is not [var use] || use.Instruction < contentCall + 2
                || Enumerable.Range(contentCall + 2, use.Instruction - contentCall - 2)
                    .Any(index => !IsCheckedTupleConstructorInstruction(region[index].Definition.Opcode)))
            {
                continue;
            }

            int cold = use.Instruction + 1;
            while (cold < end && IsCheckedTupleConstructorInstruction(region[cold].Definition.Opcode))
            {
                cold++;
            }

            if (cold + 2 >= end || region[cold].Definition.Opcode != Deallocate
                || region[cold + 1].Definition.Opcode != Return || region[cold + 2] is not
                {
                    Definition.Opcode: Label, Operands: [var label]
                } || label.Value != mismatch.Value || owners[cold + 2] != 1
                || region.Take(end).SelectMany(CheckedTupleFrameTargets).Count(target => target == (int)label.Value) != 1)
            {
                continue;
            }

            return (attributes, use, new CheckedTupleChildDelivery(
                comparison,
                nameField,
                nameSlot,
                contentLoad,
                contentField,
                cold + 3
            ));
        }

        return null;

    }

    private static (int Carrier, CheckedTupleOriginalChildDelivery Delivery)? ReadOriginalCheckedTupleChildDelivery(
        IReadOnlyList<BeamInstruction> region,
        int first,
        IReadOnlyDictionary<int, int> fields,
        int[] owners
    )
    {
        int end = region.Count - 1;
        int afterProjections = first + fields.Count;
        int firstCall = CheckedTupleChildFirstCall(region, afterProjections);
        int comparison = firstCall + 1;
        int contentLoad = comparison + 1;
        if (contentLoad >= end || !IsCheckedTupleChildUnaryCall(region[firstCall])
            || region[comparison] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [
                { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } mismatch,
                    var result,
                { Kind: BeamOperandParameters.Y, TypeIndex: null } name
                ]
            } || !IsBareX0(result) || !fields.TryGetValue((int)name.Value, out int nameField)
            || region[contentLoad] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } content, var outgoing]
            } || !IsBareX0(outgoing) || content == name
            || !fields.TryGetValue((int)content.Value, out int contentField))
        {
            return null;
        }

        // Keep the third projection in its original place. Only the child name
        // and the content move across the first call.
        int nameSlot = (int)name.Value;
        int attributes = fields.Keys.Single(slot => slot != nameSlot && slot != (int)content.Value);
        if (Enumerable.Range(afterProjections, comparison - afterProjections)
                .Any(index => region[index].Operands.Any(operand => SameProjectionRegister(operand, name)))
            || Enumerable.Range(afterProjections, end - afterProjections)
                .Any(index => index != contentLoad && owners[index] == 1
                    && region[index].Operands.Any(operand => SameProjectionRegister(operand, content))))
        {
            return null;
        }

        int contentCall = contentLoad + 1;
        while (contentCall < end && region[contentCall].Definition.Opcode == Line)
        {
            contentCall++;
        }

        // X1 is defined after the unary call. Its only new hot use is the
        // comparison and name move, before the next unary call kills X1.
        if (contentCall + 2 >= end || !IsCheckedTupleChildUnaryCall(region[contentCall])
            || region[contentCall + 1].Definition.Opcode != TestHeap)
        {
            return null;
        }

        int cold = contentCall + 2;
        while (cold < end && IsCheckedTupleConstructorInstruction(region[cold].Definition.Opcode))
        {
            cold++;
        }

        if (cold + 6 >= end || region[cold].Definition.Opcode != Deallocate
            || region[cold + 1].Definition.Opcode != Return || region[cold + 2] is not
            {
                Definition.Opcode: Label, Operands: [var label]
            } || label.Value != mismatch.Value || owners[cold + 2] != 1
            || region.Take(end).SelectMany(CheckedTupleFrameTargets).Count(target => target == (int)label.Value) != 1
            || region[cold + 3] is not { Definition.Opcode: Move, Operands: [var saved, var coldArgument] }
            || saved != name || coldArgument is not { Kind: BeamOperandParameters.X, Value.IsOne: true, TypeIndex: null }
            || region[cold + 4] is not { Definition.Opcode: Move, Operands: [var coldSource, var firstArgument] }
            || SameProjectionRegister(coldSource, name) || !IsBareX0(firstArgument)
            || region[cold + 5] is not
            {
                Definition.Opcode: InitYRegisters, Operands: [{ Value.IsOne: true }, var cleared]
            } || cleared != name)
        {
            return null;
        }

        return (nameSlot, new CheckedTupleOriginalChildDelivery(
            comparison,
            nameField,
            contentLoad,
            contentField,
            first + fields[attributes],
            cold + 3
        ));
    }

    private static int CheckedTupleChildFirstCall(IReadOnlyList<BeamInstruction> region, int firstCall)
    {
        int end = region.Count - 1;
        while (firstCall < end && region[firstCall].Definition.Opcode is Move or GetTupleElement or Line)
        {
            firstCall++;
        }

        return firstCall;
    }

    private static bool IsCheckedTupleChildUnaryCall(BeamInstruction instruction) =>
        instruction.Definition.Opcode is (LocalCall or ExternalCall) && instruction.Operands[0].Value.IsOne;
}
