using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DeferCheckedRecordTailArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(IsTaggedTuple) || !code.Span.Contains(TailExternalCallWithFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteCheckedRecordTailArguments(code, decoded.Body);
    }

    private static byte[]? RewriteCheckedRecordTailArguments(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
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
            if (entry + 8 >= end || !HasUnprotectedCallKillSchedule(body, entry))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int pair = entry + 2; pair + 4 < end; pair++)
            {
                if (body[pair] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } tuple, var first, var saved]
                    } || saved is not { Kind: BeamOperandParameters.Y, TypeIndex: null } || saved == tuple
                    || body[pair + 1] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var sameTuple, var second, var overwritten]
                    } || sameTuple != tuple || overwritten != tuple || second.Value != first.Value + 1
                    || body[pair].Offset < copied)
                {
                    continue;
                }

                int check = pair - 1;
                while (check > entry && body[check] is
                    { Definition.Opcode: GetTupleElement, Operands: [var source, _, var target] }
                    && source == tuple && target != tuple && target != saved)
                {
                    check--;
                }

                if (body[check] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [_, var checkedTuple, var arity, _]
                    } || checkedTuple != tuple || arity.Value != second.Value + 1
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        entry,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } frame)
                {
                    continue;
                }

                int allocation = frame.Owners[pair - entry];
                flow ??= new BeamProjectionFlow(body);
                if (allocation < 0 || frame.Owners[check - entry] != allocation
                    || frame.Owners[pair + 1 - entry] != allocation
                    || flow.ReadCheckedRecordTailConsumers(
                        pair + 1,
                        tuple,
                        saved,
                        entry,
                        allocation,
                        frame.Owners
                    ) is not { } consumers)
                {
                    continue;
                }

                output.Write(code.Span[copied..body[pair].Offset]);
                copied = body[pair + 2].Offset;
                if (consumers.Saved < consumers.Carrier)
                {
                    ReplaceMove(consumers.Saved, first, tuple);
                    ReplaceMove(consumers.Carrier, second, tuple);
                }
                else
                {
                    ReplaceMove(consumers.Carrier, second, tuple);
                    ReplaceMove(consumers.Saved, first, tuple);
                }

                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void ReplaceMove(int index, BeamOperand field, BeamOperand tuple)
        {
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(GetTupleElement);
            BeamCompactWriter.Write(output, YRegisterTag, tuple.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            BeamCompactWriter.Write(output, XRegisterTag, body[index].Operands[1].Value);
            copied = body[index + 1].Offset;
        }
    }

    private sealed partial class BeamProjectionFlow
    {
        internal (int Saved, int Carrier)? ReadCheckedRecordTailConsumers(
            int definition,
            BeamOperand carrier,
            BeamOperand saved,
            int entry,
            int allocation,
            IReadOnlyList<int> owners
        )
        {
            int carrierRead = -1;
            int savedRead = -1;
            var visited = new HashSet<int> { definition };
            var work = new Stack<int>(successors[definition]);
            while (work.TryPop(out int index))
            {
                if (!visited.Add(index))
                {
                    continue;
                }

                if (index <= definition || index - entry >= owners.Count || owners[index - entry] != allocation)
                {
                    return null;
                }

                BeamInstruction instruction = body[index];
                bool usesCarrier = instruction.Operands.Any(operand => SameProjectionRegister(operand, carrier));
                bool usesSaved = instruction.Operands.Any(operand => SameProjectionRegister(operand, saved));
                if (usesCarrier || usesSaved)
                {
                    if (usesCarrier && usesSaved || instruction is not
                        {
                            Definition.Opcode: Move,
                            Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null }]
                        })
                    {
                        return null;
                    }

                    if (usesCarrier && (carrierRead >= 0 || source != carrier)
                        || usesSaved && (savedRead >= 0 || source != saved))
                    {
                        return null;
                    }

                    if (usesCarrier)
                    {
                        carrierRead = index;
                    }
                    else
                    {
                        savedRead = index;
                    }
                }

                if (instruction.Definition.Opcode is Deallocate or TailLocalCallWithFrame or TailExternalCallWithFrame)
                {
                    continue;
                }

                foreach (int next in successors[index])
                {
                    if (next <= index)
                    {
                        return null;
                    }

                    work.Push(next);
                }
            }

            if (carrierRead < 0 || savedRead < 0
                || !TargetsDominated(carrierRead) || !TargetsDominated(savedRead))
            {
                return null;
            }

            int first = Math.Min(carrierRead, savedRead);
            int tail = first;
            while (tail - entry < owners.Count && body[tail].Definition.Opcode is Move or Line)
            {
                tail++;
            }

            if (tail - entry >= owners.Count || body[tail].Definition.Opcode != TailExternalCallWithFrame
                || tail <= Math.Max(carrierRead, savedRead)
                || body[carrierRead].Operands[1].Value >= body[tail].Operands[0].Value
                || body[savedRead].Operands[1].Value >= body[tail].Operands[0].Value)
            {
                return null;
            }

            return (savedRead, carrierRead);

            bool TargetsDominated(int use)
            {
                var seen = new HashSet<int>();
                work.Push(use);
                while (work.TryPop(out int index))
                {
                    if (index == definition || !seen.Add(index))
                    {
                        continue;
                    }

                    if (index <= definition || predecessors[index].Count == 0
                        || predecessors[index].Any(previous => previous >= index))
                    {
                        return false;
                    }

                    foreach (int previous in predecessors[index])
                    {
                        work.Push(previous);
                    }
                }

                return true;
            }
        }
    }
}
