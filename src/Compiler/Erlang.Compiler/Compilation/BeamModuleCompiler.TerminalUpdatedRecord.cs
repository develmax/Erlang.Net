using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepTerminalUpdatedRecordInX(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        int functionStart = 0;
        bool unprotected = false;
        for (int update = 0; update + 4 < body.Count; update++)
        {
            if (body[update].Definition.Opcode == FunctionInfo)
            {
                functionStart = update + 1;
                unprotected = false;
            }

            bool joined = body[update + 1].Definition.Opcode == Label;
            int save = update + (joined ? 2 : 1);
            int tupleIndex = save + (joined ? 2 : 1);
            if (tupleIndex + 2 >= body.Count
                || body[update] is not { Definition.Opcode: UpdateRecord, Operands: [_, _, _, var result, ..] }
                || !IsBareX0(result)
                || !TryReadRecordOperandSave(body[save], out BeamOperand saved)
                || joined && body[save + 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                || body[tupleIndex] is not { Definition.Opcode: PutTuple, Operands: [var target, _, ..] } tuple
                || !IsBareX0(target)
                || body[tupleIndex + 1].Definition.Opcode != Deallocate
                || body[tupleIndex + 2].Definition.Opcode != Return
                || tuple.Operands.Skip(2).Where(operand => operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y)
                    .ToArray() is not [var field]
                || !SameProjectionRegister(field, saved)
                || field.TypeIndex is not null)
            {
                continue;
            }

            unprotected |= HasUnprotectedCallKillSchedule(body, functionStart);
            if (!unprotected || joined && (code.Span[tuple.Offset - 1] != 0
                || !(flow ??= new BeamProjectionFlow(body)).IsInitialized(
                    save,
                    saved,
                    allowFramePredicates: true,
                    allowRegisterRecordUpdates: true
                )))
            {
                continue;
            }

            int first = tuple.Offset;
            int end = body[tupleIndex + 1].Offset;
            int operand = FirstStackOperandAtOrAfter(stackOperands, first);
            while (operand < stackOperands.Count && stackOperands[operand].Start < end)
            {
                var (from, to, slot) = stackOperands[operand++];
                if (slot != saved.Value)
                {
                    continue;
                }

                // This constructor consumes X0 before writing its result to X0.
                // A collecting join also needs X0 in its root set. Its old Y
                // operand is already initialized on every incoming path.
                edits.Add(new CodeEdit(
                    body[save].Offset,
                    body[save + 1].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));
                if (joined)
                {
                    edits.Add(new CodeEdit(
                        first - 1,
                        first,
                        BeamWire.UnsignedTag,
                        1
                    ));
                }

                edits.Add(new CodeEdit(
                    from,
                    to,
                    XRegisterTag,
                    0
                ));
                break;
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }
}
