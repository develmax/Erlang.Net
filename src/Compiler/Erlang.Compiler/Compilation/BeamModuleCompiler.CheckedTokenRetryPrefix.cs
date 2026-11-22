using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareCheckedTokenRetryPrefix(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectValue)
            || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int select = 4; select + 10 < body.Count; select++)
        {
            if (body[select].Definition.Opcode != SelectValue || body[select].Offset < copied
                || body[select - 4] is not { Definition.Opcode: GetList, Operands: [var list, var head, var tail] }
                || !IsBareX(list) || !IsBareX(head) || !IsBareX(tail) || list == head || list == tail || head == tail
                || body[select - 3] is not { Definition.Opcode: IsTuple, Operands: [_, var tuple] } || tuple != head
                || body[select - 2] is not { Definition.Opcode: TestArity, Operands: [_, var carrier, _] } || carrier != head
                || body[select - 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var checkedTuple, { Value.IsZero: true }, var tag]
                }
                || checkedTuple != head || tag != head || body[select].Operands[0] != head)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            foreach (BeamOperand entry in body[select].Operands.Skip(4).Where((_, index) => (index & 1) == 0))
            {
                if (entry.Kind != BeamOperandParameters.Label
                    || !flow.TryGetSingleForwardTarget(select, (int)entry.Value, out int longer)
                    || longer + 1 >= body.Count || body[longer + 1] is not
                    {
                        Definition.Opcode: IsNonemptyList,
                        Operands: [var retryLabel, var remaining]
                    }
                    || remaining != tail || !flow.TryGetForwardLabel(longer + 1, (int)retryLabel.Value, out int retry)
                    || retry + 2 >= body.Count || body[retry + 1].Definition.Opcode != GetList
                    || !body[retry + 1].Operands.SequenceEqual(body[select - 4].Operands)
                    || body[retry + 2] is not
                    {
                        Definition.Opcode: IsNonemptyList,
                        Operands: [var fallbackLabel, var restored]
                    }
                    || restored != tail || !flow.TryGetForwardLabel(retry + 2, (int)fallbackLabel.Value, out int fallback)
                    || !HasUnprotectedCheckedFailureRegion(body, select, fallback)
                    || ReadRun(longer + 1, retry, retryLabel) is not { } longRun
                    || ReadRun(retry + 2, fallback, fallbackLabel) is not { } shortRun
                    || shortRun.Cells < 2 || longRun.Cells <= shortRun.Cells
                    || longRun.Instructions.Count <= shortRun.Instructions.Count
                    || !flow.HasExclusiveCheckedTokenFailures(retry, longRun.Instructions)
                    || !SamePrefix(longRun.Instructions, shortRun.Instructions))
                {
                    continue;
                }

                int common = shortRun.Instructions.Count;
                if (body[longRun.Instructions[common]].Definition.Opcode != IsNonemptyList
                    || body[longRun.Instructions[common + 1]].Definition.Opcode != GetList
                    || !shortRun.Definitions.All(value => flow.IsEntryTemporaryDead(fallback, value))
                    || !flow.IsEntryTemporaryDead(fallback, head) || !flow.IsEntryTemporaryDead(fallback, tail))
                {
                    continue;
                }

                int scratch = Math.Max(longRun.Live, shortRun.Live);
                if (longRun.Definitions.Append(head).Append(tail).Append(list).Any(value => value.Value >= scratch)
                    || longRun.Definitions.Except(shortRun.Definitions).Any(value => value.Value < shortRun.Live)
                    || scratch + 1 >= FirstReservedXRegister)
                {
                    continue;
                }

                var nextHead = new BeamOperand(BeamOperandParameters.X, scratch);
                var nextTail = new BeamOperand(BeamOperandParameters.X, scratch + 1);
                output.Write(code.Span[copied..body[longer + 1].Offset]);
                for (int position = 0; position < longRun.Instructions.Count; position++)
                {
                    BeamInstruction instruction = body[longRun.Instructions[position]];
                    bool shared = position < common;
                    output.WriteByte(instruction.Definition.Opcode);
                    for (int operand = 0; operand < instruction.Operands.Count; operand++)
                    {
                        BeamOperand value = instruction.Operands[operand];
                        if (value.Kind == BeamOperandParameters.Label)
                        {
                            BeamCompactWriter.Write(output, LabelTag, shared ? fallbackLabel.Value : retryLabel.Value);
                        }
                        else if (value.Kind == BeamOperandParameters.X)
                        {
                            // The first extension split reads the original tail.
                            // Its destinations and all later scratch uses are private.
                            bool originalTail = position == common || position == common + 1 && operand == 0;
                            if (!shared && !originalTail)
                            {
                                value = value == head ? nextHead : value == tail ? nextTail : value;
                            }

                            WriteRegister(output, value);
                        }
                        else
                        {
                            BeamCompactWriter.Write(
                                output,
                                value.Kind == BeamOperandParameters.Atom ? AtomTag : BeamWire.UnsignedTag,
                                value.Value
                            );
                        }
                    }
                }

                MoveValue(nextHead, head);
                MoveValue(nextTail, tail);
                output.Write(code.Span[body[longRun.Allocation].Offset..body[retry + 1].Offset]);
                copied = body[shortRun.Allocation].Offset;
                select = shortRun.Allocation - 1;

                break;

                (List<int> Instructions, HashSet<BeamOperand> Definitions, int Allocation, int Live, int Cells)?
                    ReadRun(int at, int end, BeamOperand failure)
                {
                    var ordered = new List<int>();
                    var fields = new HashSet<BeamOperand>();
                    bool checkedHead = false;
                    int pending = 0;
                    int cells = 0;
                    for (int index = at; index < end; index++)
                    {
                        BeamInstruction instruction = body[index];
                        switch (instruction)
                        {
                            case { Definition.Opcode: IsNonemptyList, Operands: [var noList, var input] }
                                when pending == 0 && noList == failure && input == tail:
                                pending = 1;
                                break;
                            case { Definition.Opcode: GetList, Operands: [var input, var item, var rest] }
                                when pending == 1 && input == tail && item == head && rest == tail:
                                pending = 2;
                                checkedHead = false;
                                break;
                            case
                            {
                                Definition.Opcode: IsTaggedTuple,
                                Operands: [var different, var input, { Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 },
                                { Kind: BeamOperandParameters.Atom }]
                            } when pending == 2 && different == failure && input == head:
                                pending = 0;
                                checkedHead = true;
                                cells++;
                                break;
                            case
                            {
                                Definition.Opcode: GetTupleElement,
                                Operands: [var input, { Kind: BeamOperandParameters.Unsigned }, var field]
                            }
                                when pending <= 1 && checkedHead && input == head
                                && IsBareX(field) && field != head && field != tail && field != list && fields.Add(field):
                                if (pending == 1)
                                {
                                    // This read is from the preceding checked tuple
                                    // and has no effect on the next cons check.
                                    ordered.Insert(ordered.Count - 1, index);
                                    continue;
                                }

                                break;
                            case { Definition.Opcode: Allocate or AllocateHeap, Operands: [.., var live] }
                                when pending == 0 && checkedHead && live.Value > 0 && live.Value < FirstReservedXRegister:
                                return (ordered, fields, index, (int)live.Value, cells);
                            default:
                                return null;
                        }

                        ordered.Add(index);
                    }

                    return null;
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool SamePrefix(IReadOnlyList<int> longer, IReadOnlyList<int> shorter)
        {
            for (int index = 0; index < shorter.Count; index++)
            {
                BeamInstruction left = body[longer[index]];
                BeamInstruction right = body[shorter[index]];
                int skip = left.Definition.Opcode is IsNonemptyList or IsTaggedTuple ? 1 : 0;
                if (left.Definition.Opcode != right.Definition.Opcode
                    || !left.Operands.Skip(skip).SequenceEqual(right.Operands.Skip(skip)))
                {
                    return false;
                }
            }

            return true;
        }

        void MoveValue(BeamOperand from, BeamOperand to)
        {
            output.WriteByte(Move);
            WriteRegister(output, from);
            WriteRegister(output, to);
        }
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasExclusiveCheckedTokenFailures(int retry, IReadOnlyList<int> allowed) =>
            predecessors[retry].Count != 0 && predecessors[retry].All(allowed.Contains);
    }
}
