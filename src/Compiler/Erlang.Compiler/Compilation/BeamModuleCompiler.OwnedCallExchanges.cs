using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ExchangeOwnedCallArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var entriesToCompact = new HashSet<int>();
        int entry = 0;
        int copied = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int allocation = FindEntryFrameAllocation(body, start, end);
            if (allocation < 0)
            {
                continue;
            }

            for (int spill = allocation + 1; spill + 3 < end; spill++)
            {
                int clear = spill + 2;
                if (body[spill] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } destination]
                    }
                    || result != x0
                    || body[spill + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } source, var argument]
                    }
                    || argument != x0 || source == destination
                    || body[clear] is not { Definition.Opcode: InitYRegisters, Operands: [_, var retired] }
                    || retired != source)
                {
                    continue;
                }

                int call = clear + 1;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call >= end || body[call] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [{ Value.IsOne: true }, _]
                    }
                    || Enumerable.Range(start, end - start).Any(index => body[index].Definition.Opcode is
                        TryEnter or CatchEnter or TryEnd or TryCase or CatchEnd or TrimFrame or LoopReceive
                        or LoopReceiveEnd or Wait or WaitTimeout or RemoveMessage or ReceiveTimeout or DebugLine or ExecutableLine)
                    || Enumerable.Range(allocation + 1, spill - allocation - 1).Any(index =>
                        body[index].Definition.Opcode != InitYRegisters
                        && body[index].Operands.Any(operand => SameProjectionRegister(operand, destination)))
                    || ReadDisjointFrameOwnership(body, start, end) is not { } frame || frame.Uses.Count != 1
                    || frame.Owners[spill - start] != allocation
                    || frame.Owners[clear - start] != allocation)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.IsInitialized(spill, destination, allowFramePredicates: true)
                    || !flow.IsDead(call, source, allowBadRecordExit: true)
                    || flow.ReadExclusiveExchangeContinuation(
                        start,
                        end,
                        clear,
                        allocation,
                        frame.Owners
                    ) is not { } continuation)
                {
                    continue;
                }

                // The initialized destination holds nil. After the swap the Y
                // roots carry the same terms, with these two roles exchanged.
                output.Write(code.Span[copied..body[spill].Offset]);
                output.WriteByte(Swap);
                WriteRegister(output, source);
                WriteRegister(output, x0);
                copied = body[clear + 1].Offset;

                int operand = FirstStackOperandAtOrAfter(stackOperands, copied);
                foreach (int index in continuation.Order())
                {
                    BeamInstruction instruction = body[index];
                    int next = body[index + 1].Offset;
                    if (instruction.Definition.Opcode == InitYRegisters
                        && instruction.Operands.Skip(1).Any(slot => MapSlot((int)slot.Value) != slot.Value))
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        WriteFrameInitialization(output, instruction.Operands.Skip(1)
                            .Select(slot => MapSlot((int)slot.Value)).Order().ToArray());
                        copied = next;
                        continue;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                    {
                        operand++;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                    {
                        var use = stackOperands[operand++];
                        int replacement = MapSlot(use.Slot);
                        if (replacement != use.Slot)
                        {
                            output.Write(code.Span[copied..use.Start]);
                            BeamCompactWriter.Write(output, YRegisterTag, replacement);
                            copied = use.End;
                        }
                    }
                }

                // This exchange can eliminate the final semantic reference to
                // the fresh destination. Only revisit that frame after the rewrite.
                int producer = spill - 1;
                while (producer > allocation && body[producer].Definition.Opcode == Line)
                {
                    producer--;
                }

                if (body[producer].Definition.Opcode is (LocalCall or ExternalCall)
                    && !Enumerable.Range(clear + 1, end - clear - 1).Any(index =>
                    body[index].Definition.Opcode != InitYRegisters
                    && body[index].Operands.Any(operand => SameProjectionRegister(
                        operand,
                        continuation.Contains(index) ? source : destination
                    ))))
                {
                    entriesToCompact.Add((int)body[start].Operands[0].Value);
                }

                break;

                int MapSlot(int slot) => slot == source.Value ? (int)destination.Value
                    : slot == destination.Value ? (int)source.Value : slot;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, Swap);
        output.Write(code.Span[copied..]);

        byte[] exchanged = output.ToArray();
        if (entriesToCompact.Count == 0 || DecodeLateAtomicProjectionSlots(exchanged, labelCount) is not { } reduced)
        {
            return exchanged;
        }

        return RewriteDisjointFrames(
            exchanged,
            reduced.Body,
            reduced.StackOperands,
            atomicOnly: false,
            entriesToCompact: entriesToCompact
        ) ?? exchanged;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal IReadOnlySet<int>? ReadExclusiveExchangeContinuation(
            int start,
            int end,
            int clear,
            int allocation,
            IReadOnlyList<int> owners
        )
        {
            int first = clear + 1;
            var continuation = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(first);
            while (pending.TryPop(out int index))
            {
                if (index < first || index >= end || owners[index - start] != allocation)
                {
                    return null;
                }

                if (continuation.Add(index) && LinearFrameSizeOperand(body[index].Definition.Opcode) < 0)
                {
                    foreach (int next in successors[index])
                    {
                        pending.Push(next);
                    }
                }
            }

            return continuation.All(index => predecessors[index].All(previous =>
                continuation.Contains(previous) || index == first && previous == clear)) ? continuation : null;
        }
    }
}
