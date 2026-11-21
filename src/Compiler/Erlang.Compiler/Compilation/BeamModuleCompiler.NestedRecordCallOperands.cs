using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CompactNestedRecordCallOperands(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        using var output = new MemoryStream();
        var entries = new HashSet<int>();
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
            if (PlanNestedRecordCallOperandReuse(body, entry, end) is not { } plan)
            {
                continue;
            }

            entries.Add((int)body[entry].Operands[0].Value);
            output.Write(code.Span[copied..body[plan.OutgoingSave].Offset]);
            output.Write(code.Span[body[plan.OutgoingSave + 1].Offset..body[plan.Projection].Offset]);
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, XRegisterTag, 2);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            output.Write(code.Span[body[plan.Projection].Offset..body[plan.Reload].Offset]);
            output.Write(code.Span[body[plan.Reload + 1].Offset..body[plan.ResultSave].Offset]);
            var (valueStart, valueEnd, _) = stackOperands[FirstStackOperandAtOrAfter(
                stackOperands,
                body[plan.ResultSave + 1].Offset
            ) + 1];
            output.Write(code.Span[body[plan.ResultSave + 1].Offset..valueStart]);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            copied = valueEnd;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();
        if (DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return null;
        }

        return RewriteDisjointFrames(
            rewritten,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );
    }

    private static (int OutgoingSave, int Projection, int Reload, int ResultSave)? PlanNestedRecordCallOperandReuse(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end
    )
    {
        int frame = FindEntryFrameAllocation(
            body,
            entry,
            end,
            allowUnframedProjections: true
        );
        if (frame <= entry + 3 || body[entry + 1].Definition.Opcode != IsTaggedTuple
            || body[entry + 2].Definition.Opcode != GetTupleElement
            || !body.Skip(entry + 3).Take(frame - entry - 3).Any(instruction =>
                instruction.Definition.Opcode == IsTaggedTuple)
            || body.Skip(frame + 1).Take(end - frame - 1).Any(instruction =>
                instruction.Definition.Opcode is CatchEnter or TryEnter or TrimFrame or DebugLine or ExecutableLine)
            || ReadDisjointFrameOwnership(body, entry, end) is not { } ownership || ownership.Uses.Count != 1)
        {
            return null;
        }

        int[] calls = [.. Enumerable.Range(frame + 1, end - frame - 1).Where(index =>
            body[index].Definition.Opcode is LocalCall or ExternalCall)];
        if (calls is not [int call] || call + 7 >= end || body[call].Operands[0].Value != 3)
        {
            return null;
        }

        int reload = call - 1;
        while (reload > frame && body[reload].Definition.Opcode == Line)
        {
            reload--;
        }

        int projection = reload - 2;
        if (projection <= frame + 1 || body[reload] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } scratch, var firstArgument]
            } || !IsBareX0(firstArgument)
            || body[projection + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value: var secondSource },
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsOne: true }
                ]
            } || secondSource < 3
            || body[projection] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [
                { Kind: BeamOperandParameters.Y } carrier, _,
                { Kind: BeamOperandParameters.X, TypeIndex: null, Value: var thirdArgument }
                ]
            } || thirdArgument != 2 || SameProjectionRegister(carrier, scratch))
        {
            return null;
        }

        int outgoingSave = -1;
        for (int index = projection - 1; index > frame; index--)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } original, var saved]
                } || saved.Kind != BeamOperandParameters.Y)
            {
                return null;
            }

            if (SameProjectionRegister(saved, scratch))
            {
                if (original.Value != thirdArgument || saved.TypeIndex is not null)
                {
                    return null;
                }

                outgoingSave = index;
                break;
            }
        }

        if (outgoingSave < 0 || body[call + 1].Definition.Opcode != IsTaggedTuple
            || body[call + 2].Definition.Opcode != TestHeap
            || body[call + 3] is not { Definition.Opcode: UpdateRecord } inner
            || !SameProjectionRegister(inner.Operands[2], carrier) || !IsBareX0(inner.Operands[3])
            || !TryReadRecordOperandSave(body[call + 4], out BeamOperand savedResult)
            || !SameProjectionRegister(savedResult, scratch)
            || body[call + 5] is not
            {
                Definition.Opcode: UpdateRecord,
                Operands: [_, _, { Kind: BeamOperandParameters.Y }, var outerResult, _, _, var replacement]
            } || !IsBareX0(outerResult) || replacement != scratch
            || body[call + 6].Definition.Opcode != PutTuple
            || body[call + 7].Definition.Opcode != Deallocate
            || call + 8 >= end || body[call + 8].Definition.Opcode != Return)
        {
            return null;
        }

        // A single owned path consumes each of the two independent definitions.
        // Their absence must remove the slot entirely rather than leave a GC hole.
        if (Enumerable.Range(outgoingSave, call + 8 - outgoingSave).Any(index =>
                ownership.Owners[index - entry] != frame)
            || body.Skip(entry).Take(end - entry).SelectMany(instruction => instruction.Operands)
                .Count(operand => SameProjectionRegister(operand, scratch)) != 4)
        {
            return null;
        }

        return (outgoingSave, projection, reload, call + 4);
    }
}
