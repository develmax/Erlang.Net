using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record SiblingListProjectionGroup(
        int Arithmetic,
        int End,
        int Live,
        int[] Delayed,
        IReadOnlyDictionary<int, int> Registers,
        SiblingPayloadConstruction? Payload
    );

    private byte[]? ScheduleSiblingListProjectionGroup(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo)
            || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        int copyHint = atoms.TryGetValue(CopyFreshRecordUpdateHint, out int copy) ? MapEmittedAtom(copy) : -1;
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        bool unprotected = false;
        int copied = 0;
        for (int start = 0; start + 13 < body.Count; start++)
        {
            if (body[start].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, start + 1);
                continue;
            }

            if (!unprotected || body[start].Definition.Opcode != Label
                || ReadSiblingListProjectionGroup(
                    body,
                    start,
                    names,
                    imported,
                    copyHint,
                    ref flow
                ) is not { } plan)
            {
                continue;
            }

            using var replacement = new MemoryStream();
            bool complete = true;
            int tail = plan.Payload?.First ?? plan.End;
            for (int index = start + 2; index < tail; index++)
            {
                if (plan.Delayed.Contains(index) || index == plan.Payload?.Save)
                {
                    continue;
                }

                if (body[index].Definition.Opcode == Line)
                {
                    replacement.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                    continue;
                }

                int roots = index == plan.Arithmetic ? plan.Live
                    : index == plan.Arithmetic + 1 ? plan.Live + (plan.Payload is null ? 1 : 0)
                    : -1;
                if (!WriteInstruction(index, roots)
                    || index == plan.Arithmetic + 1 && plan.Delayed.Any(get => !WriteInstruction(get, -1)))
                {
                    complete = false;
                    break;
                }
            }

            if (!complete || !WriteSiblingPayloadConstruction(
                replacement,
                code,
                body,
                plan
            ))
            {
                continue;
            }

            output.Write(code.Span[copied..body[start + 2].Offset]);
            replacement.WriteTo(output);
            copied = body[plan.End].Offset;
            start = plan.End - 1;

            bool WriteInstruction(int index, int roots) => WriteDeferredRecordInstruction(
                replacement,
                code,
                body,
                index,
                roots >= 0 ? 1 : -1,
                roots,
                plan.Registers
            );
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static SiblingListProjectionGroup? ReadSiblingListProjectionGroup(
        IReadOnlyList<BeamInstruction> body,
        int start,
        string[] names,
        (int Module, int Function, int Arity)[] imported,
        int copyHint,
        ref BeamProjectionFlow? flow
    )
    {
        if (body[start + 1] is not { Definition.Opcode: IsTaggedTuple, Operands: [var failure, var state, var width, _] }
            || !IsBareX(state) || failure.Value.IsZero
            || !ReadDeferredRecordField(
                body[start + 2],
                out var countRecord,
                out var countField,
                out var countList
            )
            || !IsOwnedRecordNonemptyCheck(body[start + 3], countList, failure)
            || !ReadDeferredRecordField(
                body[start + 4],
                out var childRecord,
                out var childField,
                out var children
            )
            || body[start + 5] is not { Definition.Opcode: GetList, Operands: [var count, var position, var countTail] }
            || count != countList || countTail != countList
            || !IsOwnedRecordNonemptyCheck(body[start + 6], children, failure)
            || body[start + 7] is not { Definition.Opcode: GetList, Operands: [var list, var child, var tail] }
            || list != children || tail != children
            || countRecord != state || childRecord != state || countField == childField
            || countField.Value >= width.Value || childField.Value >= width.Value
            || !IsBareX(position) || !IsBareX(child) || state.Value >= countList.Value
            || position.Value != countList.Value + 1 || children.Value != position.Value + 1
            || child.Value != children.Value + 1 || child.Value >= FirstReservedXRegister)
        {
            return null;
        }

        int first = start + 8;
        int live = (int)child.Value + 1;
        int previousLive = live;
        var registers = new Dictionary<int, int>
        {
            [(int)position.Value] = (int)children.Value,
            [(int)children.Value] = (int)position.Value
        };
        int[] delayed = [];
        int payloadSave = -1;
        BeamOperand payloadRegister = default!;
        if (body[first] is
            { Definition.Opcode: IsTaggedTuple, Operands: [var childFailure, var taggedChild, _, _] }
            && childFailure == failure && taggedChild == child)
        {
            first++;
        }
        else if (body[first] is { Definition.Opcode: IsTuple, Operands: [var tupleFailure, var plainChild] }
            && tupleFailure == failure && plainChild == child
            && body[first + 1] is
            { Definition.Opcode: TestArity, Operands: [var arityFailure, var arityChild, var arity] }
            && arityFailure == failure && arityChild == child
            && arity.Value >= 2 && arity.Value < FirstReservedXRegister - live)
        {
            int fields = first + 2;
            int fieldCount = (int)arity.Value;
            if (ReadSiblingPlainTupleFields(
                body,
                fields,
                fieldCount,
                state,
                width,
                child,
                live
            ) is { } group)
            {
                first = fields + group.Length;
                delayed = group;
                previousLive += fieldCount;
                registers.Add(live, previousLive);
                registers.Add(previousLive, live);
            }
            else if (ReadSiblingTupleFields(
                body,
                fields,
                fieldCount,
                child,
                live
            ) is { } childGroup
                && fields + fieldCount < body.Count
                && body[fields + fieldCount] is
                { Definition.Opcode: Move, Operands: [var original, var saved] }
                && IsBareX0(original) && IsBareX(saved) && saved.Value == live + fieldCount - 1)
            {
                // Only the delayed fields and the saved X0 payload occupy
                // this high suffix. The arithmetic overwrites its own input.
                delayed = childGroup;
                payloadRegister = saved;
                payloadSave = fields + fieldCount;
                first = payloadSave + 1;
                previousLive += fieldCount;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        int arithmetic = first;
        while (arithmetic < body.Count && body[arithmetic].Definition.Opcode == Line)
        {
            arithmetic++;
        }

        if (arithmetic == first || arithmetic + 3 >= body.Count
            || body[arithmetic] is not
            {
                Definition.Opcode: GcBifTwo,
                Operands: [{ Kind: BeamOperandParameters.Failure, Value.IsZero: true }, var roots, var import, var left, var right, var result]
            } || import.Value < 0 || import.Value >= imported.Length
            || !IsLeafArithmeticGcBif(body[arithmetic], names, imported)
            || !IsBareX(result) || result.Value != (payloadSave >= 0 ? position.Value : previousLive)
            || roots.Value != previousLive
            || !(SameProjectionRegister(left, position) && IsNonRegisterValue(right)
                || SameProjectionRegister(right, position) && IsNonRegisterValue(left))
            || body[arithmetic + 1] is not { Definition.Opcode: TestHeap, Operands: [_, var heapLive] }
            || heapLive.Value != previousLive + (payloadSave < 0 ? 1 : 0)
            || !(flow ??= new BeamProjectionFlow(body)).TryGetForwardLabel(
                arithmetic,
                (int)failure.Value,
                out int fallback
            ) || !IsOwnedRecordReturnFallback(body, fallback, state))
        {
            return null;
        }

        int end = arithmetic + 2;
        if (payloadSave >= 0)
        {
            if (ReadSiblingPayloadConstruction(
                body,
                end,
                payloadSave,
                position,
                countList,
                children,
                state,
                payloadRegister,
                copyHint
            ) is not { } payload || end + 8 >= fallback)
            {
                return null;
            }

            return new(
                arithmetic,
                end + 8,
                live,
                delayed,
                registers,
                payload
            );
        }

        if (delayed.Length == 0)
        {
            // The tagged child path already sank its two fields. Require the
            // checked child and state uses in that order before reassigning
            // the independent list and position registers.
            if (!ReadDeferredRecordField(
                body[end],
                out var firstSource,
                out _,
                out var firstValue
            )
                || !ReadDeferredRecordField(
                    body[end + 1],
                    out var secondSource,
                    out _,
                    out var secondValue
                )
                || firstSource != child || secondSource != state || firstValue.Value != live + 1
                || secondValue.Value != live + 2)
            {
                return null;
            }

            end += 2;
        }

        while (end < body.Count && body[end].Definition.Opcode is (Move or PutTuple or PutList or UpdateRecord))
        {
            end++;
        }

        if (end >= fallback || body[end].Definition.Opcode != Return || body[end - 1].Definition.Opcode != UpdateRecord)
        {
            return null;
        }

        return new(
            arithmetic,
            end,
            live,
            delayed,
            registers,
            Payload: null
        );
    }

    private static int[]? ReadSiblingPlainTupleFields(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int count,
        BeamOperand state,
        BeamOperand stateWidth,
        BeamOperand child,
        int live
    )
    {
        if (first + count >= body.Count
            || !ReadDeferredRecordField(
                body[first],
                out var carrier,
                out var field,
                out var target
            )
            || carrier != state || field.Value >= stateWidth.Value || target.Value != live)
        {
            return null;
        }

        return ReadSiblingTupleFields(
            body,
            first + 1,
            count,
            child,
            live + 1
        ) is { } order ? [.. order, first] : null;
    }

    private static int[]? ReadSiblingTupleFields(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int count,
        BeamOperand child,
        int firstRegister
    )
    {
        if (first + count > body.Count)
        {
            return null;
        }

        var order = new int[count];
        for (int index = 0; index < count; index++)
        {
            int instruction = first + index;
            if (!ReadDeferredRecordField(
                body[instruction],
                out var carrier,
                out var field,
                out var target
            )
                || carrier != child || field.Value != index
                || target.Value != (index + 1 == count ? child.Value : firstRegister + index))
            {
                return null;
            }

            order[index] = instruction;
        }

        // Keep the final overwrite at the end of its single-carrier group.
        return order;
    }
}
