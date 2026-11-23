using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly Dictionary<int, BeamSourceLocation> caseFailureLocations = [];

    private ReadOnlyMemory<byte> FinalizeSharedFrameExits(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlySet<int> entries
    )
    {
        if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return code;
        }

        var (body, operands) = decoded;
        var flow = new BeamProjectionFlow(body);
        var source = caseFailureLocations.GroupBy(item => MapEmittedLabel(item.Key))
            .Where(group => group.Select(item => item.Value).Distinct().Count() == 1)
            .ToDictionary(group => group.Key, group => group.First().Value);
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int entry = 0; entry + 2 < body.Count; entry++)
        {
            if (body[entry] is not { Definition.Opcode: Label, Operands: [var label] }
                || !entries.Contains((int)label.Value)
                || body[entry + 1] is not { Definition.Opcode: Allocate, Operands: [var frame, _] })
            {
                continue;
            }

            int end = entry + 2;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            if (body[end].Definition.Opcode == FunctionInfo)
            {
                while (body[end - 1].Definition.Opcode == Line)
                {
                    end--;
                }

                end--;
            }

            if (ReadFrameRootOrder(
                body,
                entry + 2,
                end,
                (int)frame.Value
            ) is not { } slots)
            {
                continue;
            }

            var kills = PlanSharedFrameRootKills(
                body,
                flow,
                entry + 2,
                end,
                slots.Length
            );
            var cold = ReadSharedFrameColdExits(
                body,
                entry + 2,
                end,
                source
            );
            output.Write(code.Span[copied..body[entry].Offset]);
            int first = entry;
            foreach (var (start, exit) in cold)
            {
                for (; first < start; first++)
                {
                    WriteFrameInstruction(first);
                }

                first = exit + 1;
            }

            for (; first < end; first++)
            {
                WriteFrameInstruction(first);
            }

            foreach (var (start, exit) in cold)
            {
                WriteFrameInstruction(start);
                var instruction = body[exit];
                if (instruction.Definition.Opcode == CaseEnd
                    && source.TryGetValue((int)body[start].Operands[0].Value, out var location))
                {
                    var normalized = location.File is null ? location with { File = defaultLineFile } : location;
                    output.WriteByte(Line);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, IndexLineLocation(normalized));
                }
                else if (exit != start + 1)
                {
                    WriteFrameInstruction(start + 1);
                }

                WriteFrameInstruction(exit);
            }

            copied = body[end].Offset;
            entry = end - 1;

            void WriteFrameInstruction(int index)
            {
                if (kills.TryGetValue(index, out var cleared))
                {
                    WriteFrameInitialization(output, cleared.Select(slot => slots[slot]).Order().ToArray());
                }

                var operation = body[index];
                int start = operation.Offset;
                int next = body[index + 1].Offset;
                if (operation.Definition.Opcode == InitYRegisters)
                {
                    WriteFrameInitialization(output, operation.Operands.Skip(1).Select(slot => slots[(int)slot.Value]).Order().ToArray());

                    return;
                }

                int operand = FirstStackOperandAtOrAfter(operands, start);
                for (int argument = operand;
                    argument < operands.Count && operands[argument].Start < next;
                    argument++)
                {
                    var (from, to, slot) = operands[argument];
                    output.Write(code.Span[start..from]);
                    BeamCompactWriter.Write(output, YRegisterTag, slots[slot]);
                    start = to;
                }

                output.Write(code.Span[start..next]);
            }
        }

        if (copied == 0)
        {
            return code;
        }

        output.Write(code.Span[copied..]);
        highestOpcode = Math.Max(highestOpcode, InitYRegisters);

        return RebuildLineTable(output.ToArray(), labelCount);
    }

    private static IReadOnlyList<(int Start, int Exit)> ReadSharedFrameColdExits(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        IReadOnlyDictionary<int, BeamSourceLocation> source
    )
    {
        var blocks = new List<(int Start, int Exit)>();
        for (int exit = start + 1; exit < end; exit++)
        {
            if (body[exit] is not
                {
                    Definition.Opcode: CaseEnd or BadMatch,
                    Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true }]
                })
            {
                continue;
            }

            bool hasLine = body[exit - 1].Definition.Opcode == Line;
            int label = hasLine ? exit - 2 : exit - 1;
            if (label > start && body[label] is { Definition.Opcode: Label, Operands: [var target] }
                && EndsBeamBlock(body[label - 1].Definition.Opcode))
            {
                if (!hasLine && (body[exit].Definition.Opcode != CaseEnd || !source.ContainsKey((int)target.Value)))
                {
                    return [];
                }

                blocks.Add((label, exit));
            }
        }

        var removed = blocks.SelectMany(block => Enumerable.Range(block.Start, block.Exit - block.Start + 1)).ToHashSet();

        return blocks.All(block => block.Exit == block.Start + 1 || CanMoveColdFrameLine(
            body,
            block.Exit + 1,
            end,
            removed
        )) ? blocks : [];
    }

    private static bool CanMoveColdFrameLine(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        IReadOnlySet<int> removed
    )
    {
        // Do not change the source line inherited by a later operation that
        // can raise. Short returns and an explicit following line are safe.
        for (int index = start; index < end; index++)
        {
            if (removed.Contains(index))
            {
                continue;
            }

            if (body[index].Definition.Opcode == Line)
            {
                return true;
            }

            if (body[index].Definition.Opcode is not (Label or Move or Deallocate or Return))
            {
                return false;
            }
        }

        return true;
    }

    private static int[]? ReadFrameRootOrder(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int frame
    )
    {
        if (frame is <= 0 or > BeamOperandParameters.MaximumRegister)
        {
            return null;
        }

        var written = new HashSet<int>();
        var slots = new int[frame];
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            int position = StackAllocationDestination(instruction);
            if (position >= 0 && instruction.Operands[position] is { Kind: BeamOperandParameters.Y } slot
                && written.Add((int)slot.Value))
            {
                slots[(int)slot.Value] = frame - written.Count;
            }
        }

        return written.Count == frame ? slots : null;
    }

    private static Dictionary<int, List<int>> PlanSharedFrameRootKills(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int start,
        int end,
        int count
    )
    {
        var clears = new Dictionary<int, List<int>>();
        var knownClears = new HashSet<(int Index, int Slot)>();
        for (int call = start; call < end; call++)
        {
            if (body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] })
            {
                continue;
            }

            int movesEnd = call;
            while (movesEnd > start && body[movesEnd - 1].Definition.Opcode == Line)
            {
                movesEnd--;
            }

            int movesStart = movesEnd;
            while (movesStart > start && body[movesStart - 1] is
                {
                    Definition.Opcode: Move,
                    Operands: [_, { Kind: BeamOperandParameters.X } output]
                } && output.Value < arity.Value)
            {
                movesStart--;
            }

            for (int register = 0; register < count; register++)
            {
                var slot = new BeamOperand(BeamOperandParameters.Y, register);
                if (!flow.HasLiveSharedFrameRoot(call, slot, knownClears)
                    || !flow.IsDead(call, slot))
                {
                    continue;
                }

                int insert = movesStart;
                for (int move = movesStart; move < movesEnd; move++)
                {
                    if (SameProjectionRegister(body[move].Operands[0], slot))
                    {
                        insert = move + 1;
                    }
                }

                if (!clears.TryGetValue(insert, out var roots))
                {
                    roots = [];
                    clears.Add(insert, roots);
                }

                roots.Add(register);
                knownClears.Add((insert, register));
            }
        }

        return clears;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasLiveSharedFrameRoot(
            int use,
            BeamOperand slot,
            IReadOnlySet<(int Index, int Slot)> knownClears,
            bool projectedOnly = false
        )
        {
            var pending = new Stack<int>(predecessors[use]);
            var seen = new HashSet<int>();
            bool defined = false;
            while (pending.TryPop(out int index))
            {
                if (!seen.Add(index))
                {
                    continue;
                }

                BeamInstruction instruction = body[index];
                int destination = StackAllocationDestination(instruction);
                if (destination >= 0 && SameProjectionRegister(instruction.Operands[destination], slot))
                {
                    if (projectedOnly && instruction.Definition.Opcode != GetTupleElement)
                    {
                        return false;
                    }

                    defined = true;
                    continue;
                }

                if (instruction.Definition.Opcode is Allocate or AllocateHeap
                    || instruction.Definition.Opcode == InitYRegisters
                        && instruction.Operands.Any(operand => SameProjectionRegister(operand, slot))
                    || knownClears.Contains((index, (int)slot.Value))
                    || predecessors[index].Count == 0)
                {
                    return false;
                }

                foreach (int previous in predecessors[index])
                {
                    pending.Push(previous);
                }
            }

            return defined;
        }
    }
}
