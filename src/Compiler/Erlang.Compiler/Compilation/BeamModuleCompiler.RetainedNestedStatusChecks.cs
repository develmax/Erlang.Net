using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? OrderRetainedNestedStatusChecks(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(ExactNotEqualTest)
            || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        int nextLabel = labelCount;
        for (int index = 1; index + 8 < body.Count; index++)
        {
            if (body[index - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index] is not
                {
                    Definition.Opcode: ExactNotEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } joined, var selected, { Kind: BeamOperandParameters.Atom }]
                }
                || selected != x0 || body[index + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var rejected, var outer, { Value: var outerSize }, { Kind: BeamOperandParameters.Atom } outerTag]
                }
                || outer != x0 || outerSize != 2 || body[index + 2] is not
                { Definition.Opcode: GetTupleElement, Operands: [var source, { Value.IsOne: true }, var scratch] }
                || source != x0 || scratch != x1 || body[index + 3] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var innerFailure, var payload, { Value: var innerSize }, { Kind: BeamOperandParameters.Atom } innerTag]
                }
                || innerFailure != rejected || payload != x1 || innerSize != 2 || body[index + 4] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var retained, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                }
                || retained != x0 || body[index + 5] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
                || body[index + 6] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsZero: true }, _] })
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryFindNestedStatusJoins(
                index,
                (int)rejected.Value,
                (int)joined.Value,
                out int rejectedEntry,
                out int joinEntry
            ) || joinEntry + 1 >= body.Count
                || !IsMove(body[rejectedEntry + 1], x0, slot)
                || body[rejectedEntry - 1] is not { Definition.Opcode: Jump, Operands: [var completed] }
                || completed != joined || body[joinEntry + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var status, { Kind: BeamOperandParameters.Y, TypeIndex: null } resultSlot]
                }
                || status != x0 || resultSlot == slot
                || !flow.IsEntryTemporaryDead(joinEntry + 1, x1)
                || !HasDedicatedOriginalStatus(
                    body,
                    flow,
                    index,
                    joinEntry,
                    slot
                ))
            {
                continue;
            }

            // Join-side users need the original value, even when the checked
            // payload replaces X0. The successful branch's first call has
            // no arguments and already discards the old X registers.
            int nonTuple = nextLabel++;
            ReadOnlySpan<byte> wire = code.Span;
            output.Write(wire[copied..body[index].Offset]);
            output.Write(wire[body[index + 4].Offset..body[index + 5].Offset]);
            output.WriteByte(IsTuple);
            BeamCompactWriter.Write(output, LabelTag, nonTuple);
            WriteRegister(output, x0);
            WriteTaggedTuple(outerTag, typed: true);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            WriteRegister(output, x0);
            WriteTaggedTuple(innerTag, typed: false);
            output.Write(wire[body[index + 5].Offset..body[rejectedEntry].Offset]);
            output.WriteByte(Label);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, nonTuple);
            output.Write(wire[body[index].Offset..body[index + 1].Offset]);
            output.Write(wire[body[rejectedEntry].Offset..body[rejectedEntry + 1].Offset]);
            output.WriteByte(Move);
            WriteRegister(output, slot);
            WriteRegister(output, x0);
            copied = body[joinEntry].Offset;
            index = joinEntry - 1;

            void WriteTaggedTuple(BeamOperand tag, bool typed)
            {
                output.WriteByte(IsTaggedTuple);
                BeamCompactWriter.Write(output, LabelTag, rejected.Value);
                if (typed)
                {
                    output.WriteByte(ExtendedTypedRegisterOperand);
                }

                WriteRegister(output, x0);
                if (typed)
                {
                    int type = GetOperandTypeIndex((TupleTypeBits, null, null, null));
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
                }

                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
                BeamCompactWriter.Write(output, AtomTag, tag.Value);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();
        var ordered = OrderFinalPhysicalLabelsCore(
            rewritten,
            nextLabel,
            formMultiKeySelections: false,
            relocateOnly: true
        ) ?? (Code: (ReadOnlyMemory<byte>)rewritten, LabelCount: nextLabel);

        return (OrderTypeOperandsByFirstUse(ordered.Code, ordered.LabelCount), ordered.LabelCount);
    }

    private static bool HasDedicatedOriginalStatus(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int selected,
        int join,
        BeamOperand slot
    )
    {
        int entry = selected;
        while (entry > 0 && body[entry - 1].Definition.Opcode != FunctionInfo)
        {
            entry--;
        }

        int end = join + 1;
        while (end + 1 < body.Count && body[end].Definition.Opcode is not (FunctionInfo or EndCode))
        {
            end++;
        }

        if (ReadDisjointFrameOwnership(
            body,
            entry,
            end,
            trackSiblingTrimEpochs: true
        ) is not { } frame || frame.Uses.Count > 2 || !flow.HasForwardNestedStatusSuffix(join, end))
        {
            return false;
        }

        int allocation = frame.Owners[selected - entry];
        if (allocation < entry || frame.Owners[join - entry] != allocation
            || !frame.Uses.ContainsKey(allocation) || frame.Uses.Keys.Any(index => index != allocation
                && (index <= join || body[index].Definition.Opcode != TrimFrame || body[index].Operands[0].Value <= slot.Value)))
        {
            return false;
        }

        // The old atom edge skipped the save. Nothing in the joined suffix
        // may see the additional definition in that original frame.
        return Enumerable.Range(join + 1, end - join - 1).All(index =>
            frame.Owners[index - entry] != allocation || body[index].Definition.Opcode == InitYRegisters
            || !body[index].Operands.Any(operand => SameProjectionRegister(operand, slot)));
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool TryFindNestedStatusJoins(
            int selected,
            int rejected,
            int joined,
            out int rejectedEntry,
            out int joinEntry
        )
        {
            joinEntry = -1;

            return labels.TryGetValue(rejected, out rejectedEntry) && labels.TryGetValue(joined, out joinEntry)
                && rejectedEntry > selected + 6 && joinEntry == rejectedEntry + 2
                && predecessors[rejectedEntry] is [var first, var second] && first == selected + 1 && second == selected + 3
                && HasStraightCheckedCallPrefix(selected - 1, selected + 6);
        }

        internal bool HasForwardNestedStatusSuffix(int join, int end) =>
            Enumerable.Range(join, end - join).All(index => successors[index].All(target => target >= join));
    }
}
