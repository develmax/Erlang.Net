using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? DelayEntryStackFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int allocationIndex = FindEntryFrameAllocation(body, start, end);
            var plan = allocationIndex < 0 ? null : PlanEntryFramePlacement(
                body,
                start,
                allocationIndex,
                end
            );
            if (plan is { } placement)
            {
                int allocation = body[allocationIndex].Offset;
                int first = body[placement.First].Offset;
                ReadOnlySpan<byte> setup = code.Span[allocation..first];
                output.Write(code.Span[copied..allocation]);
                copied = first;
                for (int index = placement.First; index < end; index++)
                {
                    BeamInstruction instruction = body[index];
                    if (placement.FrameSite == index)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        output.Write(setup);
                        copied = instruction.Offset;
                    }

                    if (!placement.Delayed.Contains(index))
                    {
                        continue;
                    }

                    int next = body[index + 1].Offset;
                    if (instruction.Definition.Opcode == Deallocate)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        copied = next;
                        continue;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                    {
                        operand++;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                    {
                        var (from, to, slot) = stackOperands[operand++];
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, XRegisterTag, placement.Saved[slot]);
                        copied = to;
                    }
                }
            }

            start = end + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int FindEntryFrameAllocation(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        if (end - start < 3 || body[start].Definition.Opcode != Label)
        {
            return -1;
        }

        int allocation = start + 1;
        if (body[allocation].Definition.Opcode is Allocate or AllocateHeap)
        {
            return allocation;
        }

        int infoLabel = start - 2;
        while (infoLabel >= 0 && body[infoLabel].Definition.Opcode == Line)
        {
            infoLabel--;
        }

        int failure = infoLabel >= 0 && body[infoLabel].Definition.Opcode == Label
            ? (int)body[infoLabel].Operands[0].Value : -1;
        bool sawBif = false;
        for (; allocation < end; allocation++)
        {
            BeamInstruction instruction = body[allocation];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Allocate or AllocateHeap)
            {
                return sawBif ? allocation : -1;
            }

            bool bif = opcode is BifZero or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree;
            if (!(bif || opcode == Line || IsEntryFramePredicate(opcode))
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    || operand.Kind == BeamOperandParameters.Label && operand.Value != failure))
            {
                return -1;
            }

            // Earlier branches may leave through func_info, but cannot bypass the moved frame.
            sawBif |= bif;
        }

        return -1;
    }

    private static (int First, Dictionary<int, int> Saved, HashSet<int> Delayed, int FrameSite)?
        PlanEntryFramePlacement(
            IReadOnlyList<BeamInstruction> body,
            int start,
            int allocationIndex,
            int end
        )
    {
        BeamInstruction allocation = body[allocationIndex];
        int frameSize = (int)allocation.Operands[0].Value;
        int live = (int)allocation.Operands[^1].Value;
        var saved = new Dictionary<int, int>();
        int first = allocationIndex + 1;
        while (first < end)
        {
            BeamInstruction instruction = body[first];
            if (instruction is { Definition.Opcode: Move, Operands: [var source, var destination] }
                && source.Kind == BeamOperandParameters.X && source.Value < live
                && destination.Kind == BeamOperandParameters.Y)
            {
                saved[(int)destination.Value] = (int)source.Value;
            }
            else if (instruction.Definition.Opcode == InitYRegisters)
            {
                foreach (var slot in instruction.Operands.Skip(1))
                {
                    saved.Remove((int)slot.Value);
                }
            }
            else
            {
                break;
            }

            first++;
        }

        int predicate = first;
        if (allocationIndex != start + 1)
        {
            while (predicate < end && body[predicate].Definition.Opcode == Line)
            {
                predicate++;
            }
        }

        if (predicate == end || !IsDelayedFramePredicate(body[predicate].Definition.Opcode))
        {
            return null;
        }

        var labels = new Dictionary<int, int>();
        for (int index = start; index < end; index++)
        {
            if (body[index] is { Definition.Opcode: Label, Operands: [var label] })
            {
                labels.Add((int)label.Value, index);
            }
        }

        var delayed = new HashSet<int>();
        var required = new HashSet<int>();
        var parents = new Dictionary<int, int> { [first] = -1 };
        var work = new Queue<int>();
        work.Enqueue(first);
        while (work.TryDequeue(out int index))
        {
            if (index < first)
            {
                return null;
            }

            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            bool deallocates = index + 1 < end && IsEntryFrameReturn(index + 1);
            bool supported = IsDelayedFramePredicate(opcode)
                || opcode is Label or Line or Jump or BadMatch or BadRecord or CaseEnd or IfEnd or Return
                || opcode == Move && deallocates && instruction.Operands[1] is { Kind: BeamOperandParameters.X, Value.IsZero: true }
                || IsEntryFrameReturn(index);
            if (!supported || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    && !saved.ContainsKey((int)operand.Value)))
            {
                required.Add(index);
                continue;
            }

            delayed.Add(index);
            foreach (int next in Successors(index))
            {
                if (parents.TryAdd(next, index))
                {
                    work.Enqueue(next);
                }
            }
        }

        if (!delayed.Contains(first))
        {
            return null;
        }

        if (required.Count == 0)
        {
            return (first, saved, delayed, -1);
        }

        // Every common dominator lies on the recorded path to any required instruction.
        for (int frame = required.First(); frame != first; frame = parents[frame])
        {
            HashSet<int>? prefix = required.Count == 1 ? delayed : CollectUnframedPrefix(frame);
            if (prefix is null || HasMixedFramePaths(frame, prefix))
            {
                if (required.Count == 1)
                {
                    return null;
                }

                continue;
            }

            // Branches enter through the allocation label. Keep trailing line information with its code.
            int site = frame;
            if (body[frame].Definition.Opcode == Label)
            {
                site++;
            }
            else
            {
                while (prefix.Contains(site - 1) && body[site - 1].Definition.Opcode == Line)
                {
                    site--;
                }
            }

            return (first, saved, prefix, site);
        }

        return null;

        HashSet<int>? CollectUnframedPrefix(int frame)
        {
            var seen = new HashSet<int>();
            var pending = new Queue<int>();
            pending.Enqueue(first);
            while (pending.TryDequeue(out int index))
            {
                if (index == frame || !seen.Add(index))
                {
                    continue;
                }

                if (required.Contains(index))
                {
                    return null;
                }

                foreach (int next in Successors(index))
                {
                    pending.Enqueue(next);
                }
            }

            return seen;
        }

        bool HasMixedFramePaths(int frame, HashSet<int> prefix)
        {
            var seen = new HashSet<int>();
            var pending = new Queue<int>(Successors(frame));
            while (pending.TryDequeue(out int index))
            {
                if (index < first || index == frame || prefix.Contains(index))
                {
                    return true;
                }

                if (seen.Add(index))
                {
                    foreach (int next in Successors(index))
                    {
                        pending.Enqueue(next);
                    }
                }
            }

            return false;
        }

        bool IsEntryFrameReturn(int index) => index + 1 < end
            && body[index].Definition.Opcode == Deallocate && body[index].Operands[0].Value == frameSize
            && body[index + 1].Definition.Opcode == Return;

        IEnumerable<int> Successors(int index)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame))
            {
                foreach (var operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.Label && labels.TryGetValue((int)operand.Value, out int target))
                    {
                        yield return target;
                    }
                }
            }

            if (index + 1 < end && opcode != IfEnd && !EndsBeamBlock(opcode))
            {
                yield return index + 1;
            }
        }

        bool IsDelayedFramePredicate(byte opcode) => IsEntryFramePredicate(opcode)
            || allocationIndex != start + 1 && opcode is SelectValue or SelectTupleArity;
    }

    private static bool IsEntryFramePredicate(byte opcode) => IsTrimTest(opcode)
        && opcode is not (GetMapElements or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree);
}
