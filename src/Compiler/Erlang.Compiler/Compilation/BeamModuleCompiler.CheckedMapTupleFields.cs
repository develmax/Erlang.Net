using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CoalesceCheckedMapTupleFields(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetMapElements)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteCheckedMapTupleFields(code, body);
    }

    private static byte[]? RewriteCheckedMapTupleFields(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        var scratch = new BeamOperand(BeamOperandParameters.X, 0);
        int copied = 0;
        int start = 0;
        BeamProjectionFlow? flow = null;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (entry + 12 >= end || !HasUnprotectedCallKillSchedule(body, entry))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int first = entry + 2; first + 8 < end; first++)
            {
                if (body[first] is not
                    {
                        Definition.Opcode: GetMapElements,
                        Operands:
                        [
                        { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } failure,
                        { Kind: BeamOperandParameters.Y, TypeIndex: not null } map,
                            _,
                        { Kind: BeamOperandParameters.Atom } firstKey,
                            var firstValue
                        ]
                    } || firstValue != scratch
                    || body[first - 1] is not { Definition.Opcode: IsMap, Operands: [var checkFailure, var checkedMap] }
                    || checkFailure != failure || !SameProjectionRegister(checkedMap, map)
                    || body[first + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var firstSource, { Kind: BeamOperandParameters.Y, TypeIndex: null } tuple]
                    } || firstSource != scratch || SameProjectionRegister(tuple, map)
                    || body[first + 2] is not { Definition.Opcode: IsTuple, Operands: [var tupleFailure, var testedTuple] }
                    || tupleFailure != failure || testedTuple != tuple
                    || body[first + 3] is not
                    {
                        Definition.Opcode: TestArity,
                        Operands: [var arityFailure, var arityInput, var arity]
                    } || arityFailure != failure || arityInput != tuple || arity.Value < 2 || arity.Value >= end - first - 6)
                {
                    continue;
                }

                var changed = new HashSet<BeamOperand> { scratch, tuple };
                int field = first + 4;
                int second = field + (int)arity.Value;
                for (; field < second; field++)
                {
                    if (body[field] is not
                        {
                            Definition.Opcode: GetTupleElement,
                            Operands: [var carrier, var position, { Kind: BeamOperandParameters.Y, TypeIndex: null } value]
                        } || carrier != tuple || position.Value != field - first - 4
                        || SameProjectionRegister(value, map) || !changed.Add(value))
                    {
                        break;
                    }
                }

                if (field != second || body[second] is not
                    {
                        Definition.Opcode: GetMapElements,
                        Operands: [var secondFailure, var secondMap, _, { Kind: BeamOperandParameters.Atom } secondKey, var secondValue]
                    } || secondFailure != failure || secondMap != map || firstKey == secondKey || secondValue != scratch
                    || body[second + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var copiedValue, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || copiedValue != scratch || SameProjectionRegister(saved, map) || !changed.Add(saved)
                    || body[second + 2] is not
                    {
                        Definition.Opcode: GcBifTwo,
                        Operands: [var guardFailure, { Value.IsZero: true }, _, { Kind: BeamOperandParameters.Y },
                        { Kind: BeamOperandParameters.Y }, var destination]
                    } || guardFailure != failure || destination != scratch)
                {
                    continue;
                }

                if ((ownership ??= ReadDisjointFrameOwnership(body, entry, end)) is not { } frame
                    || frame.Uses.Count != 1)
                {
                    continue;
                }

                int allocation = frame.Owners[first - entry];
                if (allocation < entry || frame.Owners[second - entry] != allocation
                    || body[allocation + 1] is not { Definition.Opcode: InitYRegisters } initial
                    || !changed.Where(slot => slot.Kind == BeamOperandParameters.Y)
                        .All(slot => initial.Operands.Contains(slot)))
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.TryGetForwardLabel(second, (int)failure.Value, out int rejected)
                    || changed.Any(slot => !flow.IsDiscardedMapMatchValue(
                        rejected,
                        slot,
                        entry,
                        allocation,
                        frame.Owners
                    )))
                {
                    continue;
                }

                output.Write(code.Span[copied..body[first].Offset]);
                output.WriteByte(GetMapElements);
                BeamCompactWriter.Write(output, LabelTag, failure.Value);
                output.WriteByte(ExtendedTypedRegisterOperand);
                WriteRegister(output, map);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, map.TypeIndex!.Value);
                output.WriteByte(ExtendedListOperand);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 4);
                BeamCompactWriter.Write(output, AtomTag, secondKey.Value);
                WriteRegister(output, saved);
                BeamCompactWriter.Write(output, AtomTag, firstKey.Value);
                WriteRegister(output, tuple);
                output.Write(code.Span[body[first + 2].Offset..body[second].Offset]);
                copied = body[second + 2].Offset;
                first = second + 1;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool IsDiscardedMapMatchValue(
            int first,
            BeamOperand slot,
            int entry,
            int allocation,
            IReadOnlyList<int> owners
        )
        {
            var seen = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(first);
            while (pending.TryPop(out int index))
            {
                if (!seen.Add(index))
                {
                    continue;
                }

                if (index < entry || index - entry >= owners.Count || owners[index - entry] != allocation)
                {
                    return false;
                }

                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                bool inX = slot.Kind == BeamOperandParameters.X;
                if (opcode == Deallocate)
                {
                    if (inX)
                    {
                        return false;
                    }

                    continue;
                }

                int implicitLive = opcode switch
                {
                    TailLocalCallWithFrame or TailExternalCallWithFrame => (int)instruction.Operands[0].Value,
                    GcBifOne or GcBifTwo or GcBifThree => (int)instruction.Operands[1].Value,
                    TestHeap => (int)instruction.Operands[^1].Value,
                    _ => 0
                };
                if (inX && slot.Value < implicitLive)
                {
                    return false;
                }

                bool ordinaryWrite = opcode is Move or GetTupleElement or PutTuple or PutList or GetHead or GetTail;
                bool conditionalWrite = opcode is GcBifOne or GcBifTwo or GcBifThree;
                int destination = ordinaryWrite ? StackWriteOperand(instruction)
                    : conditionalWrite ? instruction.Operands.Count - 1 : -1;
                bool overwrites = false;
                for (int position = 0; position < instruction.Operands.Count; position++)
                {
                    if (!SameProjectionRegister(instruction.Operands[position], slot))
                    {
                        continue;
                    }

                    if (position != destination && opcode != InitYRegisters)
                    {
                        return false;
                    }

                    overwrites = true;
                }

                if (opcode is TailLocalCallWithFrame or TailExternalCallWithFrame or CaseEnd or BadMatch or BadRecord or IfEnd
                    || overwrites && !conditionalWrite)
                {
                    continue;
                }

                if (!(ordinaryWrite || conditionalWrite || IsEntryFramePredicate(opcode)
                    || opcode is Label or Line or Jump or InitYRegisters or TestHeap or SelectValue or SelectTupleArity))
                {
                    return false;
                }

                int failure = conditionalWrite && instruction.Operands[0].Kind == BeamOperandParameters.Label
                    ? labels.GetValueOrDefault((int)instruction.Operands[0].Value, -1) : -1;
                foreach (int next in successors[index])
                {
                    if (next <= index)
                    {
                        return false;
                    }

                    if (!overwrites || next != index + 1 || next == failure)
                    {
                        pending.Push(next);
                    }
                }
            }

            return true;
        }
    }
}
