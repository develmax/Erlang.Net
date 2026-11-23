using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record CheckedRecordPairFrame(
        int Allocation,
        int InitialRoots,
        int HotRoots,
        int HotDeallocation,
        int ColdDeallocation,
        int End
    );

    private byte[]? CompactCheckedRecordPairFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded
            || RewriteCheckedRecordPairFrame(code, decoded.Body, decoded.StackOperands) is not { } rewritten)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, TrimFrame);

        return rewritten;
    }

    private static byte[]? RewriteCheckedRecordPairFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int update = 14; update + 16 < body.Count; update++)
        {
            if (ReadCheckedRecordPairFrame(body, update) is not { } plan)
            {
                continue;
            }

            int slot = FirstStackOperandAtOrAfter(stackOperands, body[plan.Allocation].Offset);
            for (int index = plan.Allocation; index < plan.End; index++)
            {
                int offset = body[index].Offset;
                int next = body[index + 1].Offset;
                if (index == plan.InitialRoots || index == plan.HotRoots)
                {
                    output.Write(code.Span[copied..offset]);
                    if (index == plan.HotRoots)
                    {
                        output.WriteByte(TrimFrame);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                    }

                    copied = next;
                }
                else if (index == plan.Allocation || index == plan.HotDeallocation || index == plan.ColdDeallocation)
                {
                    copied = WritePostCallProjectionOperand(
                        output,
                        code,
                        offset,
                        copied,
                        0,
                        index == plan.HotDeallocation ? 0 : 1
                    );
                }

                while (slot < stackOperands.Count && stackOperands[slot].Start < copied)
                {
                    slot++;
                }

                while (slot < stackOperands.Count && stackOperands[slot].Start < next)
                {
                    var (from, to, _) = stackOperands[slot++];
                    output.Write(code.Span[copied..from]);
                    BeamCompactWriter.Write(output, YRegisterTag, 0);
                    copied = to;
                }
            }

            update = plan.End - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static CheckedRecordPairFrame? ReadCheckedRecordPairFrame(IReadOnlyList<BeamInstruction> body, int update)
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        if (body[update] is not
            {
                Definition.Opcode: UpdateRecord,
                Operands: [_, _, { Kind: BeamOperandParameters.Y } saved, var output, { Value: var fields }, _, var replacement]
            } || output != x0 || fields != 2 || replacement != x0
            || !ReadsField(
                update - 1,
                x1,
                1,
                x0
            )
            || body[update - 2] is not { Definition.Opcode: TestHeap, Operands: [_, { Value: var live }] } || live != 2
            || body[update - 3] is not
            { Definition.Opcode: TestArity, Operands: [var failed, var tuple, { Value: var pairWidth }] }
            || tuple != x1 || pairWidth != 2
            || body[update - 4] is not { Definition.Opcode: IsTuple, Operands: [var invalid, var pair] }
            || invalid != failed || pair != x1 || !ReadsField(
                update - 5,
                x0,
                1,
                x1
            )
            || body[update - 6] is not
            { Definition.Opcode: IsTaggedTuple, Operands: [var invalidTag, var tagged, { Value: var taggedWidth }, _] }
            || invalidTag != failed || !SameProjectionRegister(tagged, x0) || taggedWidth != 2
            || body[update - 7] is not { Definition.Opcode: IsTuple, Operands: [var alternate, var produced] }
            || produced != x0 || alternate == failed
            || !IsTwoArgumentCall(update - 8) || !ReadsField(
                update + 1,
                x1,
                0,
                x1
            ) || !IsExchange(update + 2))
        {
            return null;
        }

        int initialRoots = update - 9;
        while (initialRoots >= 0 && body[initialRoots].Definition.Opcode == Line)
        {
            initialRoots--;
        }

        int allocation = initialRoots - 4;
        int hotRoots = update + 3;
        if (allocation < 1 || !IsExchange(initialRoots - 1)
            || body[initialRoots - 2] is not
            { Definition.Opcode: GetTupleElement, Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } source, var field, var outgoing] }
            || outgoing != x0 || source.Value < 2
            || body[initialRoots - 3] is not { Definition.Opcode: Move, Operands: [var input, var carrier] }
            || input != source || carrier.TypeIndex is not null || !SameProjectionRegister(carrier, saved)
            || body[allocation] is not { Definition.Opcode: Allocate, Operands: [{ Value: var width }, var roots] }
            || width != 3 || source.Value >= roots.Value || !ReadClears(initialRoots, includesCarrier: false)
            || !ReadClears(hotRoots, includesCarrier: true)
            || body[allocation - 1] is not
            { Definition.Opcode: IsTaggedTuple, Operands: [var outside, var checkedSource, var originalWidth, _] }
            || !SameProjectionRegister(checkedSource, source) || field.Value >= originalWidth.Value || outside == failed || outside == alternate)
        {
            return null;
        }

        int call = SkipLines(hotRoots + 1);
        if (call + 12 >= body.Count || !IsTwoArgumentCall(call)
            || body[call + 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
            || !ReturnsTuple(call + 2, usesCarrier: false)
            || !Deallocates(call + 3)
            || !Labels(call + 5, alternate)
            || body[call + 6] is not
            { Definition.Opcode: ExactEqualTest, Operands: [var badAtom, var checkedAtom, { Kind: BeamOperandParameters.Atom }] }
            || badAtom != failed || checkedAtom != x0
            || body[call + 7] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
            || !ReturnsTuple(call + 8, usesCarrier: true)
            || !Deallocates(call + 9) || !Labels(call + 11, failed))
        {
            return null;
        }

        int error = SkipLines(call + 12);
        if (error + 1 >= body.Count || body[error] is not { Definition.Opcode: CaseEnd, Operands: [var raw] } || raw != x0)
        {
            return null;
        }

        int entry = allocation - 1;
        while (entry > 0 && (body[entry].Definition.Opcode == Line
            || IsEntryFramePredicate(body[entry].Definition.Opcode) && body[entry].Operands[0] == outside))
        {
            entry--;
        }

        int end = error + 1;
        if (body[entry].Definition.Opcode != Label
            || Enumerable.Range(0, body.Count).Any(index => (index < entry || index >= end)
                && body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Label && (operand == failed || operand == alternate)))
            || ReadDisjointFrameOwnership(
                body,
                entry,
                end,
                ignoredBranchTarget: (int)outside.Value
            ) is not { } frame
            || frame.Uses.Count != 1 || frame.Owners[error - entry] != allocation
            || !frame.Uses.TryGetValue(allocation, out var slots) || slots.Count(used => used) != 1 || !slots[(int)carrier.Value])
        {
            return null;
        }

        // Every use of the only owned Y value stays before the hot trim or
        // belongs to the cold return. Sibling clauses retain their own frames.
        return new(
            allocation,
            initialRoots,
            hotRoots,
            call + 3,
            call + 9,
            end
        );

        bool ReadClears(int index, bool includesCarrier) => body[index] is { Definition.Opcode: InitYRegisters, Operands: var terms }
            && terms.Skip(1).Select(term => (int)term.Value).SequenceEqual(
                Enumerable.Range(0, 3).Where(slot => includesCarrier || slot != saved.Value));

        bool IsTwoArgumentCall(int index) => body[index] is
        { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] } && arity == 2;

        bool IsExchange(int index) => body[index] is { Definition.Opcode: Swap, Operands: [var left, var right] }
            && left == x0 && right == x1;

        bool ReadsField(
            int index,
            BeamOperand input,
            int field,
            BeamOperand destination
        ) =>
            body[index] is { Definition.Opcode: GetTupleElement, Operands: [var from, var selected, var to] }
            && from == input && selected.Value == field && to == destination;

        bool Labels(int index, BeamOperand target) => body[index] is { Definition.Opcode: Label, Operands: [var label] }
            && label.Value == target.Value;

        bool Deallocates(int index) => body[index] is { Definition.Opcode: Deallocate, Operands: [{ Value: var width }] }
            && width == 3 && body[index + 1].Definition.Opcode == Return;

        bool ReturnsTuple(int index, bool usesCarrier) => body[index] is
        { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] } && destination == x0
            && body[index].Operands.Skip(2).Any(operand => SameProjectionRegister(operand, saved)) == usesCarrier
            && body[index].Operands.Skip(2).All(operand => operand.Kind != BeamOperandParameters.Y
                || usesCarrier && SameProjectionRegister(operand, saved));

        int SkipLines(int index)
        {
            while (index < body.Count && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }
    }
}
