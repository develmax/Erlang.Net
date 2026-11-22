using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveRepeatedSavedRecordChecks(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        if (!code.Span.Contains(IsTaggedTuple)
            || (!code.Span.Contains(BifOne) && !code.Span.Contains(BifTwo) && !code.Span.Contains(BifThree)))
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var removed = new HashSet<int>();
        int copied = 0;

        int start = -1;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            if (start >= 0)
            {
                RemoveOrdinaryChecks(start, end);
            }

            start = end + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void RemoveOrdinaryChecks(int first, int end)
        {
            for (int index = first; index < end; index++)
            {
                if (body[index].Definition.Opcode is CatchEnter or CatchEnd or TryEnter or TryEnd or TryCase
                    or LoopReceive or LoopReceiveEnd or RemoveMessage or ReceiveTimeout or Wait or WaitTimeout
                    or ReceiveMarkerBind or ReceiveMarkerClear or ReceiveMarkerReserve or ReceiveMarkerUse
                    or ExecutableLine or DebugLine)
                {
                    return;
                }
            }

            Dictionary<(BeamOperand Source, BeamOperand Size, BeamOperand Tag), int>? firstChecks = null;
            int call = -1;
            int guardedBif = -1;
            for (int index = first; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode is LocalCall or ExternalCall)
                {
                    call = index;
                }
                else if (IsGuardedXResultBif(instruction))
                {
                    guardedBif = index;
                }

                if (!TryReadSavedRecordCheck(instruction, out var record))
                {
                    continue;
                }

                firstChecks ??= [];
                if (firstChecks.TryAdd(record, index))
                {
                    continue;
                }

                int original = firstChecks[record];
                if (original >= call || original >= guardedBif)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                bool requiresEntry = false;
                bool proven = flow.IsTaggedTupleBefore(
                    index,
                    record.Source,
                    record.Size,
                    record.Tag,
                    static _ => false,
                    (_, _) => requiresEntry = true,
                    candidate => candidate >= original && candidate < index && !removed.Contains(candidate)
                        && TryReadSavedRecordCheck(body[candidate], out var checkedRecord) && checkedRecord == record,
                    preserveGuardedXResults: true
                );
                if (!proven || requiresEntry)
                {
                    continue;
                }

                removed.Add(index);
                output.Write(code.Span[copied..instruction.Offset]);
                copied = body[index + 1].Offset;
            }
        }
    }

    private static bool IsGuardedXResultBif(BeamInstruction instruction) =>
        instruction is
        {
            Definition.Opcode: BifOne or BifTwo or BifThree,
            Operands: [
            { Kind: BeamOperandParameters.Label, Value.Sign: > 0 },
                ..,
            { Kind: BeamOperandParameters.X }
            ]
        };

    private static bool TryReadSavedRecordCheck(
        BeamInstruction instruction,
        out (BeamOperand Source, BeamOperand Size, BeamOperand Tag) record
    )
    {
        if (instruction is
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [
                { Kind: BeamOperandParameters.Label, Value.Sign: > 0 },
                { Kind: BeamOperandParameters.Y, TypeIndex: null } source,
                { Kind: BeamOperandParameters.Unsigned } size,
                { Kind: BeamOperandParameters.Atom } tag
                ]
            })
        {
            record = (source, size, tag);

            return true;
        }

        record = default!;

        return false;
    }
}
