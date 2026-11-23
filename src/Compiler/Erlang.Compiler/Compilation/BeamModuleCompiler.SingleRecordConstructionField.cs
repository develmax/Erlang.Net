using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SinkSingleOwnedRecordConstructionField(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo)
            || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        bool unprotected = false;
        for (int start = 0; start + 12 < body.Count; start++)
        {
            if (body[start].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, start + 1);
                continue;
            }

            if (!unprotected || body[start].Definition.Opcode != Label
                || body[start + 1] is not { Definition.Opcode: IsTaggedTuple, Operands: [var rejected, var state, var width, _] }
                || !IsBareX(state) || rejected.Value.IsZero
                || !ReadDeferredRecordField(
                    body[start + 2],
                    out var countRecord,
                    out var countField,
                    out var countList
                )
                || !IsOwnedRecordNonemptyCheck(body[start + 3], countList, rejected)
                || !ReadDeferredRecordField(
                    body[start + 4],
                    out var childRecord,
                    out var childField,
                    out var children
                )
                || body[start + 5] is not { Definition.Opcode: GetList, Operands: [var count, var position, var countTail] }
                || count != countList || countTail != countList
                || !IsOwnedRecordNonemptyCheck(body[start + 6], children, rejected)
                || body[start + 7] is not { Definition.Opcode: GetList, Operands: [var list, var child, var tail] }
                || list != children || tail != children
                || body[start + 8] is not
                { Definition.Opcode: IsTaggedTuple, Operands: [var childRejected, var childTuple, var childWidth, _] }
                || childRejected != rejected || childTuple != child
                || !ReadDeferredRecordField(
                    body[start + 9],
                    out var fieldRecord,
                    out var field,
                    out var fieldResult
                )
                || fieldRecord != child || field.Value >= childWidth.Value
                || countRecord != state || childRecord != state || countField == childField
                || countField.Value >= width.Value || childField.Value >= width.Value
                || !IsBareX(position) || !IsBareX(child)
                || !(state.Value < fieldResult.Value && fieldResult.Value < countList.Value
                    && countList.Value < position.Value && position.Value < children.Value
                    && children.Value < child.Value))
            {
                continue;
            }

            int projection = start + 9;
            int arithmetic = projection + 1;
            while (arithmetic < body.Count && body[arithmetic].Definition.Opcode == Line)
            {
                arithmetic++;
            }

            if (arithmetic == projection + 1 || arithmetic + 3 >= body.Count
                || body[arithmetic] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [{ Kind: BeamOperandParameters.Failure, Value.IsZero: true }, var live, var import, var left, var right, var result]
                } || import.Value < 0 || import.Value >= imported.Length
                || !IsLeafArithmeticGcBif(body[arithmetic], names, imported)
                || !IsBareX(result) || result.Value >= FirstReservedXRegister || result.Value != child.Value + 1
                || live.Value != result.Value
                || !(SameProjectionRegister(left, position) && IsNonRegisterValue(right)
                    || SameProjectionRegister(right, position) && IsNonRegisterValue(left))
                || body[arithmetic + 1] is not { Definition.Opcode: TestHeap, Operands: [_, var heapLive] }
                || heapLive.Value != live.Value + 1
                || !(flow ??= new BeamProjectionFlow(body)).TryGetForwardLabel(
                    start + 8,
                    (int)rejected.Value,
                    out int fallback
                ) || !IsOwnedRecordReturnFallback(body, fallback, state))
            {
                continue;
            }

            int end = arithmetic + 2;
            while (end < body.Count && body[end].Definition.Opcode is (Move or PutTuple or PutList or UpdateRecord))
            {
                end++;
            }

            if (end == arithmetic + 2 || end >= fallback || body[end].Definition.Opcode != Return
                || body[end - 1].Definition.Opcode != UpdateRecord)
            {
                continue;
            }

            // The original field occupied an early X register. Its slot can
            // hold the first list, then the list carriers and child compress
            // into the holes they leave. The checked child stays live across
            // the unchanged F0 arithmetic and is projected after the heap.
            int newLive = (int)child.Value;
            var registers = new Dictionary<int, int>
            {
                [(int)countList.Value] = (int)fieldResult.Value,
                [(int)children.Value] = (int)countList.Value,
                [(int)child.Value] = (int)children.Value,
                [(int)result.Value] = newLive,
                [(int)fieldResult.Value] = (int)result.Value
            };

            using var replacement = new MemoryStream();
            bool complete = true;
            for (int index = start + 2; index < end; index++)
            {
                if (index == projection)
                {
                    continue;
                }

                if (body[index].Definition.Opcode == Line)
                {
                    replacement.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                    continue;
                }

                int roots = index == arithmetic ? newLive : index == arithmetic + 1 ? newLive + 1 : -1;
                if (!WriteDeferredRecordInstruction(
                    replacement,
                    code,
                    body,
                    index,
                    roots >= 0 ? 1 : -1,
                    roots,
                    registers
                ))
                {
                    complete = false;
                    break;
                }

                if (index == arithmetic + 1 && !WriteDeferredRecordInstruction(
                    replacement,
                    code,
                    body,
                    projection,
                    -1,
                    0,
                    registers
                ))
                {
                    complete = false;
                    break;
                }
            }

            if (!complete)
            {
                continue;
            }

            output.Write(code.Span[copied..body[start + 2].Offset]);
            replacement.WriteTo(output);
            copied = body[end].Offset;
            start = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool IsOwnedRecordNonemptyCheck(
        BeamInstruction instruction,
        BeamOperand register,
        BeamOperand failure
    ) => instruction is { Definition.Opcode: IsNonemptyList, Operands: [var rejected, var tested] }
        && rejected == failure && tested == register;

    private static bool IsOwnedRecordReturnFallback(
        IReadOnlyList<BeamInstruction> body,
        int fallback,
        BeamOperand carrier
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);

        return fallback + 2 < body.Count && IsMove(body[fallback + 1], carrier, x0)
            && body[fallback + 2].Definition.Opcode == Return;
    }
}
