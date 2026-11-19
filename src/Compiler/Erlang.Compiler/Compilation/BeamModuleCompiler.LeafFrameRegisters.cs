using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ReallocateLeafFrameRegisters(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands,
        bool afterCanonicalization = false,
        Func<BeamInstruction, bool>? isArithmetic = null,
        Func<BeamInstruction, bool>? canDiscardExternalTailFrame = null
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        int start = 0;
        int arity = 0;
        for (int end = 0; end < body.Count; end++)
        {
            BeamInstruction boundary = body[end];
            if (boundary.Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int?[]? registers = PlanLeafFrameRegisters(
                body,
                start,
                end,
                arity,
                afterCanonicalization,
                isArithmetic,
                out Dictionary<int, int>? heapRoots,
                canDiscardExternalTailFrame: canDiscardExternalTailFrame
            );
            if (registers is not null)
            {
                for (int index = start; index < end; index++)
                {
                    BeamInstruction instruction = body[index];
                    int next = body[index + 1].Offset;
                    while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                    {
                        operand++;
                    }

                    if (instruction.Definition.Opcode is Allocate or Deallocate or InitYRegisters)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        copied = next;
                        continue;
                    }

                    if (instruction.Definition.Opcode is TailLocalCallWithFrame or TailExternalCallWithFrame)
                    {
                        int frame = (int)instruction.Operands[^1].Value;
                        output.Write(code.Span[copied..instruction.Offset]);
                        output.WriteByte(instruction.Definition.Opcode == TailLocalCallWithFrame ? TailLocalCall : TailExternalCall);
                        output.Write(code.Span[(instruction.Offset + 1)..(next - CompactRegisterCountWidth(frame))]);
                        copied = next;
                        continue;
                    }

                    if (heapRoots is not null && heapRoots.TryGetValue(index, out int live))
                    {
                        if (instruction.Definition.Opcode == GcBifTwo)
                        {
                            copied = WriteLeafArithmeticRoots(
                                code,
                                output,
                                instruction.Offset,
                                copied,
                                live
                            );
                        }
                        else
                        {
                            int original = (int)instruction.Operands[^1].Value;
                            int width = CompactRegisterCountWidth(original);
                            if (instruction.Definition.Opcode == AllocateHeap)
                            {
                                int frame = (int)instruction.Operands[0].Value;
                                output.Write(code.Span[copied..instruction.Offset]);
                                output.WriteByte(TestHeap);
                                copied = instruction.Offset + 1 + CompactRegisterCountWidth(frame);
                            }

                            output.Write(code.Span[copied..(next - width)]);
                            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
                            copied = next;
                        }
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                    {
                        var (from, to, slot) = stackOperands[operand++];
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, XRegisterTag, registers[slot]!.Value);
                        copied = to;
                    }
                }
            }

            start = end + 1;
            if (boundary.Definition.Opcode == FunctionInfo)
            {
                arity = (int)boundary.Operands[2].Value;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int?[]? PlanLeafFrameRegisters(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int arity,
        bool afterCanonicalization,
        Func<BeamInstruction, bool>? isArithmetic,
        out Dictionary<int, int>? heapRoots,
        bool isolatedTailBranch = false,
        Func<BeamInstruction, bool>? canDiscardExternalTailFrame = null,
        IReadOnlySet<int>? postCallSlots = null,
        int postCallFrameSize = 0
    )
    {
        heapRoots = null;
        const int xCount = BeamOperandParameters.MaximumRegister;
        int allocation = postCallSlots is null ? FindEntryFrameAllocation(body, start, end) : -1;
        if (allocation < 0 && postCallSlots is null)
        {
            return null;
        }

        int slots = postCallSlots is null ? (int)body[allocation].Operands[0].Value : postCallFrameSize;
        if (slots <= 0 || slots >= xCount)
        {
            return null;
        }

        int count = end - start;
        var labels = new Dictionary<int, int>();
        var uses = new BigInteger[count];
        var writes = new int[count][];
        var movedFrom = new int[count];
        var slotsUsed = new HashSet<int>();
        bool needsCanonicalization = false;
        for (int index = 0; index < count; index++)
        {
            BeamInstruction instruction = body[start + index];
            byte opcode = instruction.Definition.Opcode;
            bool localTail = opcode == TailLocalCallWithFrame;
            bool externalTail = opcode == TailExternalCallWithFrame
                && (isolatedTailBranch || canDiscardExternalTailFrame?.Invoke(instruction) == true);
            bool arithmetic = opcode == GcBifTwo && isArithmetic?.Invoke(instruction) == true;
            bool guardedBif = IsLeafGuardBif(instruction);
            needsCanonicalization |= opcode == PutTuple || arithmetic;
            if ((!localTail && !externalTail && !IsFramelessInstruction(instruction))
                || (opcode is Allocate or AllocateHeap && start + index != allocation)
                || (!afterCanonicalization && opcode == PutTuple)
                || opcode is GetMapElements or GcBifOne or GcBifThree
                || (opcode is BifOne or BifTwo or BifThree && !guardedBif)
                || (opcode == GcBifTwo && !arithmetic)
                || ((externalTail || opcode is Deallocate or TailLocalCallWithFrame) && instruction.Operands[^1].Value != slots))
            {
                return null;
            }

            if (instruction is { Definition.Opcode: Label, Operands: [var label] })
            {
                labels.Add((int)label.Value, index);
            }

            var destinations = new List<int>(2);
            uses[index] = opcode switch
            {
                Return => BigInteger.One,
                _ when localTail || externalTail => (BigInteger.One << (int)instruction.Operands[0].Value) - 1,
                _ => BigInteger.Zero
            };
            movedFrom[index] = -1;
            for (int position = 0; position < instruction.Operands.Count && opcode != InitYRegisters; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                int register = LeafRegisterIndex(operand);
                if (register < 0)
                {
                    continue;
                }

                if (register >= xCount)
                {
                    int slot = register - xCount;
                    if (slot >= slots)
                    {
                        return null;
                    }

                    if (postCallSlots is null || postCallSlots.Contains(slot))
                    {
                        slotsUsed.Add(slot);
                    }
                }

                if (opcode == Swap || opcode == GetList && position > 0 || opcode == PutTuple && position == 0
                    || (guardedBif || opcode is Move or GetHead or GetTail or GetTupleElement or PutList or GcBifTwo)
                        && position == instruction.Operands.Count - 1)
                {
                    destinations.Add(register);
                    if (opcode != Swap)
                    {
                        continue;
                    }
                }

                uses[index] |= BigInteger.One << register;
                if (opcode == Move)
                {
                    movedFrom[index] = register;
                }
            }

            writes[index] = destinations.ToArray();
        }

        if (afterCanonicalization && !needsCanonicalization && postCallSlots is null)
        {
            return null;
        }

        var successors = new int[count][];
        var conditionalWriteFailures = new int?[count];
        for (int index = 0; index < count; index++)
        {
            BeamInstruction instruction = body[start + index];
            var targets = new List<int>();
            if (index + 1 < count && !EndsBeamBlock(instruction.Definition.Opcode))
            {
                targets.Add(index + 1);
            }

            foreach (var operand in instruction.Operands)
            {
                if (instruction.Definition.Opcode != TailLocalCallWithFrame && operand.Kind == BeamOperandParameters.Label)
                {
                    int target = labels.GetValueOrDefault((int)operand.Value, -1);
                    if (instruction.Definition.Opcode == GcBifTwo || IsLeafGuardBif(instruction))
                    {
                        if (target < 0)
                        {
                            return null;
                        }

                        conditionalWriteFailures[index] = target;
                    }
                    else
                    {
                        targets.Add(target);
                    }
                }
            }

            successors[index] = targets.ToArray();
        }

        var liveIn = new BigInteger[count];
        var liveOut = new BigInteger[count];
        BigInteger arguments = (BigInteger.One << arity) - 1;
        BigInteger externalInputs = postCallSlots is null ? arguments : BigInteger.Zero;
        bool changed;
        do
        {
            changed = false;
            for (int index = count - 1; index >= 0; index--)
            {
                BigInteger after = BigInteger.Zero;
                foreach (int next in successors[index])
                {
                    // A branch back to func_info uses the original arguments for function_clause.
                    after |= next < 0 ? externalInputs : liveIn[next];
                }

                liveOut[index] = after;
                foreach (int destination in writes[index])
                {
                    after &= ~(BigInteger.One << destination);
                }

                // A BIF writes its destination only along its success edge.
                if (conditionalWriteFailures[index] is int failure)
                {
                    after |= liveIn[failure];
                }

                BigInteger before = uses[index] | after;
                changed |= before != liveIn[index];
                liveIn[index] = before;
            }
        }
        while (changed);

        var xMask = (BigInteger.One << xCount) - 1;
        if (isolatedTailBranch)
        {
            BigInteger required = liveIn[0];
            if (postCallSlots is not null)
            {
                // Non-promoted Y operands are already supplied by the retained frame.
                required &= xMask;
                foreach (int slot in postCallSlots)
                {
                    required |= liveIn[0] & (BigInteger.One << (xCount + slot));
                }
            }

            if ((required & ~arguments) != BigInteger.Zero)
            {
                return null;
            }
        }

        var conflicts = new BigInteger[xCount + slots];
        var preferred = new List<int>[slots];
        for (int index = 0; index < count; index++)
        {
            if (body[start + index].Definition.Opcode == InitYRegisters
                && body[start + index].Operands.Skip(1).Any(slot => LeafRegisterIsLive(liveOut[index], LeafRegisterIndex(slot))))
            {
                return null;
            }

            int source = movedFrom[index];
            foreach (int destination in writes[index])
            {
                BigInteger neighbors = liveOut[index];
                if (source >= 0)
                {
                    // A copy can share its source register when no other definition interferes.
                    neighbors &= ~(BigInteger.One << source);
                    if (source < xCount && destination >= xCount)
                    {
                        (preferred[destination - xCount] ??= []).Add(source);
                    }
                    else if (source >= xCount && destination < xCount)
                    {
                        (preferred[source - xCount] ??= []).Add(destination);
                    }
                }

                foreach (int other in writes[index])
                {
                    neighbors |= BigInteger.One << other;
                }

                conflicts[destination] |= neighbors;
            }
        }

        var registers = new int?[slots];
        // Allocate retained values in definition order, as in the pinned interval scan.
        var slotsInOrder = afterCanonicalization
            ? writes.SelectMany(destinations => destinations)
                .Where(register => register >= xCount)
                .Select(register => register - xCount)
                .Concat(slotsUsed.Order()).Distinct()
            : slotsUsed.Order();
        foreach (int slot in slotsInOrder)
        {
            int reg = xCount + slot;
            int candidate = (preferred[slot] ?? []).Concat(Enumerable.Range(0, xCount))
                .FirstOrDefault(CanUse, -1);
            if (candidate < 0)
            {
                return null;
            }

            registers[slot] = candidate;

            bool CanUse(int x) => !Interferes(x) && !slotsUsed.Any(other =>
                registers[other] == x && Interferes(xCount + other));

            bool Interferes(int other) => LeafRegisterIsLive(conflicts[reg], other)
                || LeafRegisterIsLive(conflicts[other], reg);
        }

        for (int index = 0; index < count; index++)
        {
            if (body[start + index].Definition.Opcode is not (TestHeap or AllocateHeap or GcBifTwo))
            {
                continue;
            }

            BigInteger roots = liveIn[index] & xMask;
            foreach (int slot in slotsUsed)
            {
                if (LeafRegisterIsLive(liveIn[index], xCount + slot))
                {
                    roots |= BigInteger.One << registers[slot]!.Value;
                }
            }

            int live = (int)roots.GetBitLength();
            if (live >= xCount)
            {
                return null;
            }

            (heapRoots ??= []).Add(start + index, live);
        }

        return heapRoots is null || HasInitializedLeafHeapRoots(
            start,
            arguments,
            registers,
            heapRoots,
            writes,
            successors,
            conditionalWriteFailures
        ) ? registers : null;
    }

    private static bool IsLeafGuardBif(BeamInstruction instruction) =>
        instruction.Definition.Opcode is BifOne or BifTwo or BifThree
        && instruction.Operands[0].Kind == BeamOperandParameters.Label;

    private static bool HasInitializedLeafHeapRoots(
        int start,
        BigInteger arguments,
        int?[] registers,
        Dictionary<int, int> heapRoots,
        int[][] writes,
        int[][] successors,
        int?[] conditionalWriteFailures
    )
    {
        const int xCount = BeamOperandParameters.MaximumRegister;
        var initialized = new BigInteger?[writes.Length];
        initialized[0] = arguments;
        var work = new Queue<int>();
        work.Enqueue(0);
        while (work.TryDequeue(out int index))
        {
            BigInteger after = initialized[index]!.Value;
            if (heapRoots.TryGetValue(start + index, out int live))
            {
                // Every register in the root prefix must be initialized on every path.
                BigInteger prefix = (BigInteger.One << live) - 1;
                after &= prefix;
                if (after != prefix)
                {
                    return false;
                }
            }

            if (conditionalWriteFailures[index] is int failure)
            {
                MergeInitialized(failure, after);
            }

            foreach (int register in writes[index])
            {
                int translated = register < xCount ? register : registers[register - xCount]!.Value;
                after |= BigInteger.One << translated;
            }

            foreach (int next in successors[index])
            {
                if (next >= 0)
                {
                    MergeInitialized(next, after);
                }
            }
        }

        return true;

        void MergeInitialized(int target, BigInteger incoming)
        {
            BigInteger intersection = (initialized[target] ?? incoming) & incoming;
            if (initialized[target] != intersection)
            {
                initialized[target] = intersection;
                work.Enqueue(target);
            }
        }
    }

    private static int LeafRegisterIndex(BeamOperand operand) => operand.Kind switch
    {
        BeamOperandParameters.X => (int)operand.Value,
        BeamOperandParameters.Y => BeamOperandParameters.MaximumRegister + (int)operand.Value,
        _ => -1
    };

    private static bool LeafRegisterIsLive(BigInteger live, int register) =>
        !(live & (BigInteger.One << register)).IsZero;
}
