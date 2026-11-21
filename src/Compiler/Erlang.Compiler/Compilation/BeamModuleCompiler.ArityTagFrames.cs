using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? DelayArityTagFrames(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int failure = EntryFrameFailureLabel(body, start);
            int selector = start + 2;
            if (failure < 0 || selector >= end
                || body[start].Definition.Opcode != Label
                || body[start + 1] is not
                {
                    Definition.Opcode: IsTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } failed, var input]
                } || failed.Value != failure
                || input is not { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }
                || body[selector] is not
                {
                    Definition.Opcode: SelectTupleArity,
                    Operands: [var tuple, var unmatched, _, ..]
                } dispatch || !SameProjectionRegister(input, tuple) || unmatched != failed
                || body.Skip(start).Take(end - start).Any(instruction =>
                    instruction.Definition.Opcode is TrimFrame or CatchEnter or TryEnter
                        || instruction.Definition.Name is "debug_line" or "executable_line"))
            {
                continue;
            }

            int arity = (int)body[start - 1].Operands[^1].Value;
            var plans = new List<(int Allocation, int Init, int Projection, int Scratch, int Slot, BeamOperand Tag)>();
            for (int option = 3; option + 1 < dispatch.Operands.Count; option += 2)
            {
                BeamOperand count = dispatch.Operands[option];
                BeamOperand target = dispatch.Operands[option + 1];
                if (count.Value <= 0 || target.Kind != BeamOperandParameters.Label
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(selector, (int)target.Value, out int label)
                    || label + 6 >= end)
                {
                    continue;
                }

                int allocation = label + 1;
                if (body[allocation].Definition.Opcode != Allocate
                    || body[allocation].Operands[0].Value > BeamOperandParameters.MaximumRegister
                    || body[allocation].Operands[^1].Value < arity)
                {
                    continue;
                }

                int frameSize = (int)body[allocation].Operands[0].Value;
                int init = body[allocation + 1].Definition.Opcode == InitYRegisters ? allocation + 1 : -1;
                int save = init >= 0 ? init + 1 : allocation + 1;
                int projection = save;
                var savedArguments = new Dictionary<BeamOperand, BeamOperand>();
                while (projection < end && body[projection] is
                    {
                        Definition.Opcode: Move,
                        Operands: [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } argument,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } slot
                        ]
                    } && argument.Value < arity && slot.Value < frameSize
                    && savedArguments.TryAdd(slot, argument))
                {
                    projection++;
                }

                int guard = projection + 1;
                if (guard + 1 >= end || body[projection] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var source, { Value.IsZero: true }, { Kind: BeamOperandParameters.Y, TypeIndex: null } tagSlot]
                    } || !savedArguments.TryGetValue(source, out var original) || original != input
                    || savedArguments.ContainsKey(tagSlot) || tagSlot.Value >= frameSize
                    || body[guard] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [
                        { Kind: BeamOperandParameters.Label } cleanupLabel, var tested,
                        { Kind: BeamOperandParameters.Atom } tag
                        ]
                    } || tested != tagSlot
                    || !TryFindArityTagCleanup(
                        body,
                        guard,
                        end,
                        cleanupLabel,
                        out int cleanup
                    )
                    || !IsArityTagFailure(
                        body,
                        cleanup,
                        end,
                        savedArguments,
                        source,
                        failure,
                        frameSize
                    ))
                {
                    continue;
                }

                int scratch = Math.Max(arity, (int)body[allocation].Operands[^1].Value);
                if (scratch >= FirstReservedXRegister || !(flow ??= new BeamProjectionFlow(body)).HasUnusedArityTagSlot(
                    start,
                    end,
                    guard + 1,
                    frameSize,
                    tagSlot
                )
                    || !(flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(
                        allocation,
                        new BeamOperand(BeamOperandParameters.X, scratch)
                    ))
                {
                    continue;
                }

                plans.Add((allocation, init, projection, scratch, (int)tagSlot.Value, tag));
            }

            foreach (var (allocation, init, projection, scratch, slot, tag) in plans.OrderBy(plan => plan.Allocation))
            {
                int offset = body[allocation].Offset;
                output.Write(code.Span[copied..offset]);
                output.WriteByte(GetTupleElement);
                BeamCompactWriter.Write(output, XRegisterTag, 0);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                BeamCompactWriter.Write(output, XRegisterTag, scratch);
                output.WriteByte(ExactEqualTest);
                BeamCompactWriter.Write(output, LabelTag, failure);
                BeamCompactWriter.Write(output, XRegisterTag, scratch);
                BeamCompactWriter.Write(output, AtomTag, tag.Value);
                output.Write(code.Span[offset..body[allocation + 1].Offset]);
                int[] initialized = init < 0
                    ? [slot]
                    : [.. body[init].Operands.Skip(1).Select(operand => (int)operand.Value).Append(slot).Distinct().Order()];
                WriteFrameInitialization(output, initialized);
                int save = init < 0 ? allocation + 1 : init + 1;
                output.Write(code.Span[body[save].Offset..body[projection].Offset]);
                copied = body[projection + 2].Offset;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool TryFindArityTagCleanup(
        IReadOnlyList<BeamInstruction> body,
        int guard,
        int end,
        BeamOperand failure,
        out int cleanup
    )
    {
        for (cleanup = guard + 1; cleanup < end; cleanup++)
        {
            if (body[cleanup].Definition.Opcode == Label && body[cleanup].Operands[0].Value == failure.Value)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsArityTagFailure(
        IReadOnlyList<BeamInstruction> body,
        int cleanup,
        int end,
        IReadOnlyDictionary<BeamOperand, BeamOperand> savedArguments,
        BeamOperand carrier,
        int failure,
        int frameSize
    )
    {
        bool carrierRestored = false;
        int release = cleanup + 1;
        while (release < end && body[release] is { Definition.Opcode: Move, Operands: [var source, var restored] })
        {
            if (!savedArguments.TryGetValue(source, out var argument) || argument != restored)
            {
                return false;
            }

            carrierRestored |= source == carrier;
            release++;
        }

        return carrierRestored && release + 1 < end
            && body[release] is { Definition.Opcode: Deallocate, Operands: [var size] } && size.Value == frameSize
            && body[release + 1] is { Definition.Opcode: Jump, Operands: [var target] } && target.Value == failure;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasUnusedArityTagSlot(
            int functionStart,
            int functionEnd,
            int success,
            int frameSize,
            BeamOperand tag
        )
        {
            var pending = new Stack<int>();
            var visited = new HashSet<int>();
            bool exited = false;
            pending.Push(success);
            while (pending.TryPop(out int index))
            {
                if (index < functionStart || index >= functionEnd)
                {
                    return false;
                }

                if (!visited.Add(index))
                {
                    continue;
                }

                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is Allocate or AllocateHeap or Return or TailLocalCall or TailExternalCall
                    || (opcode != InitYRegisters
                        && instruction.Operands.Any(operand => SameProjectionRegister(operand, tag)))
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y && operand.Value >= frameSize))
                {
                    return false;
                }

                int width = LinearFrameSizeOperand(opcode);
                if (width >= 0)
                {
                    if (instruction.Operands[width].Value != frameSize)
                    {
                        return false;
                    }

                    exited = true;
                    continue;
                }

                if (opcode is CaseEnd or BadMatch or BadRecord or IfEnd or Raise)
                {
                    exited = true;
                    continue;
                }

                if (successors[index].Length == 0)
                {
                    return false;
                }

                foreach (int next in successors[index])
                {
                    pending.Push(next);
                }
            }

            return exited;
        }
    }
}
