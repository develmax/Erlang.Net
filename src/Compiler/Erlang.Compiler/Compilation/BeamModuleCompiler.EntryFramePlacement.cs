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

            var plan = PlanEntryFramePlacement(body, start, end);
            if (plan is { } placement)
            {
                int allocation = body[start + 1].Offset;
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

    private static (int First, Dictionary<int, int> Saved, HashSet<int> Delayed, int FrameSite)?
        PlanEntryFramePlacement(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        if (end - start < 3 || body[start].Definition.Opcode != Label
            || body[start + 1].Definition.Opcode is not (Allocate or AllocateHeap))
        {
            return null;
        }

        BeamInstruction allocation = body[start + 1];
        int frameSize = (int)allocation.Operands[0].Value;
        int live = (int)allocation.Operands[^1].Value;
        var saved = new Dictionary<int, int>();
        int first = start + 2;
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

        if (first == end || !IsEntryFramePredicate(body[first].Definition.Opcode))
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
        int frame = -1;
        var work = new Queue<int>();
        work.Enqueue(first);
        while (work.TryDequeue(out int index))
        {
            if (index < first)
            {
                return null;
            }

            if (delayed.Contains(index) || index == frame)
            {
                continue;
            }

            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            bool deallocates = index + 1 < end && IsEntryFrameReturn(index + 1);
            bool supported = IsEntryFramePredicate(opcode)
                || opcode is Label or Line or Jump or BadMatch or BadRecord or CaseEnd or IfEnd or Return
                || opcode == Move && deallocates && instruction.Operands[1] is { Kind: BeamOperandParameters.X, Value.IsZero: true }
                || IsEntryFrameReturn(index);
            if (!supported || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    && !saved.ContainsKey((int)operand.Value)))
            {
                // Keep a shared frame above branches that have multiple framed continuations.
                if (frame >= 0)
                {
                    return null;
                }

                frame = index;
                continue;
            }

            delayed.Add(index);
            foreach (int next in Successors(index))
            {
                work.Enqueue(next);
            }
        }

        if (!delayed.Contains(first))
        {
            return null;
        }

        // An already allocated path must not enter the delayed region or a frame insertion.
        if (frame >= 0)
        {
            foreach (int next in Successors(frame))
            {
                work.Enqueue(next);
            }
        }

        var framed = new HashSet<int>();
        while (work.TryDequeue(out int index))
        {
            if (index < first || delayed.Contains(index) || index == frame)
            {
                return null;
            }

            if (framed.Add(index))
            {
                foreach (int next in Successors(index))
                {
                    work.Enqueue(next);
                }
            }
        }

        // Leave line information with the first instruction that needs the frame.
        int site = frame;
        while (delayed.Contains(site - 1) && body[site - 1].Definition.Opcode == Line)
        {
            site--;
        }

        return (first, saved, delayed, site);

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
    }

    private static bool IsEntryFramePredicate(byte opcode) => IsTrimTest(opcode)
        && opcode is not (GetMapElements or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree);
}
