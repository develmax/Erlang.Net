using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? SelectCheckedListHeadTags(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectTupleArity)
            || !code.Span.Contains(TrimFrame) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        var category = new BeamOperand(BeamOperandParameters.Y, 0);
        var checkedTag = new BeamOperand(BeamOperandParameters.Y, 1);
        var carrier = new BeamOperand(BeamOperandParameters.Y, 2);
        var savedTail = new BeamOperand(BeamOperandParameters.Y, 3);
        BeamProjectionFlow? flow = null;
        Dictionary<int, string>? names = null;
        using var output = new MemoryStream();
        int copied = 0;
        int added = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (entry == 0 || end - entry < 20 || !body[entry - 1].Operands[^1].Value.IsOne
                || body[entry] is not { Definition.Opcode: Label, Operands: [var recursive] }
                || body[entry + 1] is not { Definition.Opcode: IsNonemptyList, Operands: [var empty, var list] }
                || list != x0
                || body[entry + 2] is not { Definition.Opcode: GetList, Operands: [var from, var head, var tail] }
                || from != x0 || head != x1 || tail != x0
                || body[entry + 3] is not { Definition.Opcode: IsTuple, Operands: [var failed, var tested] }
                || tested != x1
                || body[entry + 4] is not
                {
                    Definition.Opcode: SelectTupleArity,
                    Operands: [var selected, var rejected, _, _, _, ..]
                } arities || !SameProjectionRegister(selected, x1) || rejected != failed
                || body.Skip(entry + 1).Take(end - entry - 1).Any(instruction => instruction.Definition.Opcode is
                    CatchEnter or TryEnter or DebugLine or ExecutableLine))
            {
                continue;
            }

            var ownership = flow ??= new BeamProjectionFlow(body);
            if (!ownership.TryGetExclusiveForwardTarget(
                entry + 3,
                entry + 4,
                (int)failed.Value,
                out int tupleFailure
            ) || !ownership.TryGetSingleForwardTarget(entry + 1, (int)empty.Value, out int listEnd)
                || !ReadsCaseFailure(
                    tupleFailure,
                    x1,
                    end,
                    out int afterTupleFailure
                )
                || afterTupleFailure != listEnd || listEnd + 2 >= end
                || body[listEnd + 1] is not { Definition.Opcode: IsNil, Operands: [var unmatched, var original] }
                || original != x0 || unmatched.Value != EntryFrameFailureLabel(body, entry)
                || body[listEnd + 2].Definition.Opcode != Return)
            {
                continue;
            }

            for (int option = 3; option + 1 < arities.Operands.Count; option += 2)
            {
                if (arities.Operands[option].Value != 2 || arities.Operands[option + 1].Kind != BeamOperandParameters.Label
                    || !ownership.TryGetSingleForwardTarget(entry + 4, (int)arities.Operands[option + 1].Value, out int label)
                    || label + 6 >= tupleFailure
                    || body[label + 1] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                    || width.Value != 4 || live.Value != 2
                    || body[label + 2] is not
                    {
                        Definition.Opcode: InitYRegisters, Operands: [_, var first, var second]
                    } || first != category || second != checkedTag
                    || !ReadsMove(label + 3, x1, carrier) || !ReadsMove(label + 4, x0, savedTail)
                    || !ReadsTag(label + 5, carrier, x0))
                {
                    continue;
                }

                int check = label + 6;
                int target = labelCount + added;
                var arms = new List<(BeamOperand Key, int Target, int Body, int Next)>();
                while (check < tupleFailure && body[check] is
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [{ Kind: BeamOperandParameters.Label } retry, var discriminator,
                        { Kind: BeamOperandParameters.Atom } key]
                    } && discriminator == x0)
                {
                    if (!ownership.TryGetSingleForwardTarget(check, (int)retry.Value, out int next)
                        || next >= tupleFailure || !IsRetainedArm(
                            check + 1,
                            next,
                            (int)recursive.Value,
                            out int retained
                        ))
                    {
                        arms.Clear();
                        break;
                    }

                    arms.Add((key, target, retained, next));
                    target = (int)retry.Value;
                    check = next + 1;
                }

                if (arms.Count < 2 || arms.Select(arm => arm.Key.Value).Distinct().Count() != arms.Count
                    || check + 3 >= tupleFailure || !ReadsTag(check, carrier, checkedTag)
                    || !ReadsMove(check + 1, checkedTag, x0))
                {
                    continue;
                }

                int classify = SkipLines(check + 2, tupleFailure);
                int selection = classify + 1;
                if (selection >= tupleFailure || body[classify] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] }
                    || body[selection] is not
                    {
                        Definition.Opcode: SelectValue,
                        Operands: [var result, { Kind: BeamOperandParameters.Label } invalidClass, _, _, _, ..]
                    } || result != x0
                    || !ownership.TryGetSingleForwardTarget(selection, (int)invalidClass.Value, out int classFailure)
                    || classFailure != listEnd + 3
                    || !ReadsCaseFailure(
                        classFailure,
                        x0,
                        end,
                        out int afterClassFailure
                    )
                    || afterClassFailure >= end || body[tupleFailure - 1].Definition.Opcode != Return)
                {
                    continue;
                }

                names ??= atoms.Where(pair => MapEmittedAtom(pair.Value) > 0)
                    .ToDictionary(pair => MapEmittedAtom(pair.Value), pair => pair.Key);
                if (arms.Any(arm => !names.ContainsKey((int)arm.Key.Value)))
                {
                    continue;
                }

                // Put the checked tag in a new X register. Both X0 (tail)
                // and X1 (original head) still hold their entry values.
                output.Write(code.Span[copied..body[label + 1].Offset]);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, x1);
                WriteUnsigned(0);
                WriteRegister(output, x2);
                output.WriteByte(SelectValue);
                WriteRegister(output, x2);
                BeamCompactWriter.Write(output, LabelTag, target);
                output.WriteByte(ExtendedListOperand);
                WriteUnsigned(arms.Count * 2);
                var atomOrder = Comparer<string>.Create((left, right) => TermOrder.Compare(
                    new Atom(left),
                    new Atom(right),
                    exact: true
                ));
                foreach (var arm in arms.OrderBy(item => names[(int)item.Key.Value], atomOrder))
                {
                    BeamCompactWriter.Write(output, AtomTag, arm.Key.Value);
                    BeamCompactWriter.Write(output, LabelTag, arm.Target);
                }

                foreach (var arm in arms)
                {
                    WriteLabel(arm.Target);
                    WriteAllocation(1, 2);
                    WriteMove(x1, category);
                    output.Write(code.Span[body[arm.Body].Offset..body[arm.Next].Offset]);
                }

                WriteLabel(target);
                WriteAllocation(4, 3);
                WriteFrameInitialization(output, [0]);
                WriteMove(x2, checkedTag);
                WriteMove(x1, carrier);
                WriteMove(x0, savedTail);
                WriteMove(x2, x0);
                output.Write(code.Span[body[check + 2].Offset..body[tupleFailure].Offset]);
                output.Write(code.Span[body[listEnd].Offset..body[afterClassFailure].Offset]);
                output.Write(code.Span[body[tupleFailure].Offset..body[listEnd].Offset]);
                copied = body[afterClassFailure].Offset;
                added++;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return OrderFinalPhysicalLabelsCore(
            output.ToArray(),
            labelCount + added,
            formMultiKeySelections: false,
            relocateOnly: true
        );

        bool IsRetainedArm(
            int index,
            int next,
            int recursive,
            out int retained
        )
        {
            retained = index + 3;
            int call = SkipLines(retained, next);

            return call + 5 == next && ReadsMove(index, savedTail, x0) && ReadsMove(index + 1, carrier, savedTail)
                && body[index + 2] is { Definition.Opcode: TrimFrame, Operands: [var drop, { Value.IsOne: true }] }
                && drop.Value == 3
                && body[call] is { Definition.Opcode: LocalCall, Operands: [{ Value.IsOne: true }, var target] }
                && target.Value == recursive
                && body[call + 1] is { Definition.Opcode: TestHeap, Operands: [var heap, { Value.IsOne: true }] }
                && heap.Value == ConsHeapWords
                && body[call + 2] is { Definition.Opcode: PutList, Operands: [var head, var tail, var result] }
                && head == category && tail == x0 && result == x0
                && body[call + 3] is { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                && body[call + 4].Definition.Opcode == Return;
        }

        bool ReadsCaseFailure(
            int label,
            BeamOperand value,
            int end,
            out int after
        )
        {
            int failure = SkipLines(label + 1, end);
            after = failure + 1;

            return failure < end && body[failure] is { Definition.Opcode: CaseEnd, Operands: [var rejected] }
                && value == rejected;
        }

        int SkipLines(int index, int end)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        bool ReadsMove(int index, BeamOperand source, BeamOperand target) =>
            body[index] is { Definition.Opcode: Move, Operands: [var from, var to] } && from == source && to == target;

        bool ReadsTag(int index, BeamOperand source, BeamOperand target) =>
            body[index] is { Definition.Opcode: GetTupleElement, Operands: [var from, { Value.IsZero: true }, var to] }
            && from == source && to == target;

        void WriteLabel(int value)
        {
            output.WriteByte(Label);
            WriteUnsigned(value);
        }

        void WriteAllocation(int width, int live)
        {
            output.WriteByte(Allocate);
            WriteUnsigned(width);
            WriteUnsigned(live);
        }

        void WriteMove(BeamOperand from, BeamOperand to)
        {
            output.WriteByte(Move);
            WriteRegister(output, from);
            WriteRegister(output, to);
        }

        void WriteUnsigned(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);
    }
}
