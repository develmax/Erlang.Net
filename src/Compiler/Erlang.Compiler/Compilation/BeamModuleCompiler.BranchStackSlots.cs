using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Count, int[] Slots, (int Index, int OriginalRemoved, int Kept)? Trim)? PlanBranchStackSlots(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end
    )
    {
        if (entry + 1 >= end || body[entry].Definition.Opcode != Label)
        {
            return null;
        }

        int infoLabel = entry - 2;
        while (infoLabel >= 0 && body[infoLabel].Definition.Opcode == Line)
        {
            infoLabel--;
        }

        int clauseFailure = infoLabel >= 0 && body[infoLabel].Definition.Opcode == Label
            ? (int)body[infoLabel].Operands[0].Value : -1;
        var failureLabels = new HashSet<int>();
        int allocation = entry + 1;
        while (allocation < end && body[allocation].Definition.Opcode is not (Allocate or AllocateHeap))
        {
            var instruction = body[allocation];
            if (instruction.Definition.Opcode != Line && !IsUnframedEntryProjection(instruction)
                && (!IsEntryFramePredicate(instruction.Definition.Opcode)
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y)))
            {
                return null;
            }

            foreach (BeamOperand operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Label && operand.Value != clauseFailure)
                {
                    failureLabels.Add((int)operand.Value);
                }
            }

            allocation++;
        }

        if (allocation == end)
        {
            return null;
        }

        int frame = (int)body[allocation].Operands[0].Value;
        if (frame < 2 || frame > BeamOperandParameters.MaximumRegister)
        {
            return null;
        }

        int start = allocation + 1;
        int trim = end;
        int removed = 0;
        for (int index = start; index < end; index++)
        {
            if (body[index].Definition.Opcode != TrimFrame)
            {
                continue;
            }

            int drop = (int)body[index].Operands[0].Value;
            int kept = (int)body[index].Operands[1].Value;
            if (trim != end || drop <= 0 || kept != frame - drop)
            {
                return null;
            }

            trim = index;
            removed = drop;
        }

        var labels = new Dictionary<int, int>();
        var firstUse = new int[frame];
        var neededAfterTrim = new bool[frame];
        int handlerEntry = -1;
        int handlerSlot = -1;
        bool isCatch = false;
        Array.Fill(firstUse, int.MaxValue);
        for (int index = start; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode == Label)
            {
                labels.Add((int)instruction.Operands[0].Value, index);
            }

            if (instruction.Definition.Opcode is TryEnter or CatchEnter)
            {
                if (trim != end || handlerEntry >= 0
                    || instruction.Operands is not [{ Kind: BeamOperandParameters.Y } tag, { Kind: BeamOperandParameters.Label }]
                    || tag.Value >= frame)
                {
                    return null;
                }

                handlerEntry = index;
                handlerSlot = (int)tag.Value;
                isCatch = instruction.Definition.Opcode == CatchEnter;
            }

            int shift = index > trim ? removed : 0;
            foreach (BeamOperand operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y && operand.Value < frame - shift)
                {
                    int slot = (int)operand.Value + shift;
                    neededAfterTrim[slot] |= index > trim;
                    if (instruction.Definition.Opcode != InitYRegisters)
                    {
                        firstUse[slot] = Math.Min(firstUse[slot], index);
                    }
                }
            }
        }

        foreach (int failure in failureLabels)
        {
            if (!labels.TryGetValue(failure, out int target) || !IsFramelessStackAllocationExit(body, target, end, allowReturn: true))
            {
                return null;
            }
        }

        BigInteger[] masks = Enumerable.Range(0, frame).Select(slot => BigInteger.One << slot).ToArray();
        var effects = new (BigInteger Reads, BigInteger Writes, bool FallsThrough, List<int>? Targets)[end - start];
        var live = new BigInteger[effects.Length + 1];
        var conflicts = new BigInteger[frame];
        bool branched = failureLabels.Count != 0;
        bool released = false;
        bool hasBackEdge = false;
        for (int index = end - 1; index >= start; index--)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Allocate or AllocateHeap
                || trim != end && opcode is (RemoveMessage or ReceiveTimeout or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout)
                || instruction.Definition.Name == "debug_line")
            {
                return null;
            }

            int frameOperand = LinearFrameSizeOperand(opcode);
            int shift = index > trim ? removed : 0;
            if (frameOperand >= 0)
            {
                if (instruction.Operands[frameOperand].Value != frame - shift)
                {
                    return null;
                }

                released = true;
            }

            bool fallsThrough = !EndsBeamBlock(opcode) && opcode != IfEnd;
            List<int>? targets = null;
            bool call = opcode is LocalCall or TailLocalCall or TailLocalCallWithFrame;
            bool initializes = opcode == InitYRegisters;
            if (opcode is TryEnd or TryCase or CatchEnd && (handlerEntry < 0
                || (opcode == CatchEnd) != isCatch
                || instruction.Operands is not [{ Kind: BeamOperandParameters.Y } tag]
                || tag.Value != handlerSlot))
            {
                return null;
            }

            int destination = opcode switch
            {
                LoopReceive => 1,
                TryEnter or CatchEnter => 0,
                WaitTimeout or GetMapElements or BinaryMatch or TryEnd or TryCase or CatchEnd or TryCaseEnd or Raise => -1,
                _ => StackAllocationDestination(instruction)
            };
            BigInteger reads = BigInteger.Zero;
            BigInteger writes = BigInteger.Zero;
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (!call && opcode is not (TryEnter or CatchEnter)
                    && operand.Kind == BeamOperandParameters.Label && operand.Value != clauseFailure)
                {
                    if (!labels.TryGetValue((int)operand.Value, out int target)
                        || (target > trim) != (index > trim) && !IsFramelessStackAllocationExit(body, target, end))
                    {
                        return null;
                    }

                    (targets ??= []).Add(target - start);
                    hasBackEdge |= target <= index;
                    branched = true;
                }

                if (operand.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                if (operand.Value >= frame - shift || frameOperand >= 0
                    || !initializes && destination == int.MinValue
                        && opcode is not (SelectValue or SelectTupleArity) && !IsEntryFramePredicate(opcode))
                {
                    return null;
                }

                int slot = (int)operand.Value + shift;
                if (initializes && firstUse[slot] == int.MaxValue)
                {
                    continue;
                }

                if (opcode is TryEnd or TryCase or CatchEnd)
                {
                    // Removing the handler reads its tag and clears the slot.
                    reads |= masks[slot];
                    writes |= masks[slot];
                    continue;
                }

                // In bs_match, the context precedes the command list. Only destinations
                // in that list can be registers.
                if (initializes || position == destination || opcode == GetList && position == 2
                    || opcode == GetMapElements && position >= 4 && position % 2 == 0
                    || opcode == BinaryMatch && position >= BeamBinaryNames.MatchPrefix)
                {
                    writes |= masks[slot];
                }
                else
                {
                    reads |= masks[slot];
                }
            }

            effects[index - start] = (reads, writes, fallsThrough, targets);
        }

        var exceptional = handlerEntry >= 0 ? new HashSet<int>() : null;
        if (exceptional is not null && !AddHandlerEdges(exceptional) || !branched && trim == end || !released)
        {
            return null;
        }

        bool changed;
        do
        {
            changed = false;
            for (int index = effects.Length - 1; index >= 0; index--)
            {
                var (reads, writes, fallsThrough, targets) = effects[index];
                BigInteger next = fallsThrough ? live[index + 1] : BigInteger.Zero;
                // A failing instruction defines its destination on the fallthrough edge only.
                BigInteger before = reads | (next & ~writes);
                if (targets is not null)
                {
                    foreach (int target in targets)
                    {
                        before |= live[target];
                    }
                }

                changed |= before != live[index];
                live[index] = before;
            }
        } while (hasBackEdge && changed);

        for (int index = 0; index < effects.Length; index++)
        {
            var (reads, writes, fallsThrough, targets) = effects[index];
            BigInteger before = live[index];
            AddStackInterference(before, before, conflicts);
            if (!writes.IsZero)
            {
                BigInteger next = fallsThrough ? live[index + 1] : BigInteger.Zero;
                bool initializes = body[start + index].Definition.Opcode == InitYRegisters;
                BigInteger occupied = next | reads | (initializes ? BigInteger.Zero : writes);
                if (targets is not null && (exceptional?.Contains(index) == true
                    || body[start + index].Definition.Opcode is GetMapElements or BinaryMatch))
                {
                    // A failed match can write before transferring control. An
                    // installed handler likewise needs its live slots preserved.
                    foreach (int target in targets)
                    {
                        occupied |= live[target];
                    }
                }

                AddStackInterference(writes, occupied, conflicts);
                AddStackInterference(occupied, writes, conflicts);
            }
        }

        var groups = new List<BigInteger>();
        var slots = new int[frame];
        int keptSlots = 0;
        Array.Fill(slots, -1);
        foreach (int slot in Enumerable.Range(0, frame)
            .Where(slot => firstUse[slot] != int.MaxValue)
            .OrderByDescending(slot => slot == handlerSlot || neededAfterTrim[slot])
            .ThenBy(slot => firstUse[slot]))
        {
            int register = groups.FindIndex(group => (group & conflicts[slot]).IsZero);
            if (register >= 0)
            {
                groups[register] |= masks[slot];
            }
            else
            {
                register = groups.Count;
                groups.Add(masks[slot]);
            }

            slots[slot] = register;
            if (neededAfterTrim[slot])
            {
                // The groups needed after trim occupy the high end of the frame.
                keptSlots = groups.Count;
            }
        }

        if (groups.Count == 0 || groups.Count >= frame)
        {
            return null;
        }

        for (int slot = 0; slot < frame; slot++)
        {
            if (slots[slot] >= 0)
            {
                slots[slot] = groups.Count - slots[slot] - 1;
            }
        }

        return (groups.Count, slots, trim < end ? (trim, removed, keptSlots) : null);

        bool AddHandlerEdges(HashSet<int> protectedInstructions)
        {
            if (!labels.TryGetValue((int)body[handlerEntry].Operands[1].Value, out int handler))
            {
                return false;
            }

            int entry = handlerEntry - start;
            int target = handler - start;
            int handle = handler;
            while (handle < end && body[handle].Definition.Opcode is Label or Line)
            {
                handle++;
            }

            byte exit = isCatch ? CatchEnd : TryCase;
            if (handle == end || body[handle].Definition.Opcode != exit)
            {
                return false;
            }

            const int NoHandler = 0;
            const int Installed = 1;
            const int HandlingException = 2;
            var seen = new int?[effects.Length];
            var pending = new Stack<(int Index, int HandlerState)>();
            pending.Push((0, NoHandler));
            while (pending.TryPop(out var state))
            {
                var (index, handlerState) = state;
                if (index < 0 || index >= effects.Length)
                {
                    return false;
                }

                // Both paths may enter a catch_end through its labels and lines.
                // The cleanup clears the same Y tag before any ordinary code.
                if (isCatch && handlerState == Installed && index >= target && start + index <= handle)
                {
                    handlerState = HandlingException;
                }

                if (seen[index] is int previous)
                {
                    if (previous != handlerState)
                    {
                        return false;
                    }

                    continue;
                }

                seen[index] = handlerState;
                byte opcode = body[start + index].Definition.Opcode;
                bool enters = index == entry;
                bool exits = opcode is TryEnd or TryCase or CatchEnd;
                if (enters && handlerState != NoHandler
                    || opcode == TryEnd && handlerState != Installed
                    || opcode == exit && (handlerState != HandlingException || start + index != handle)
                    || handlerState != NoHandler && (opcode == Return || LinearFrameSizeOperand(opcode) >= 0))
                {
                    return false;
                }

                var effect = effects[index];
                int next = enters ? Installed : exits ? NoHandler : handlerState;
                if (effect.FallsThrough)
                {
                    pending.Push((index + 1, next));
                }

                if (effect.Targets is not null)
                {
                    foreach (int successor in effect.Targets)
                    {
                        pending.Push((successor, next));
                    }
                }

                if (enters)
                {
                    pending.Push((target, HandlingException));
                }
                else if (handlerState == Installed && !exits)
                {
                    protectedInstructions.Add(index);
                    (effect.Targets ??= []).Add(target);
                    effects[index] = effect;
                    hasBackEdge |= target <= index;
                    branched = true;
                }
            }

            return seen[entry] == NoHandler;
        }
    }

    private static bool IsFramelessStackAllocationExit(
        IReadOnlyList<BeamInstruction> body,
        int target,
        int end,
        bool allowReturn = false
    )
    {
        int index = target + 1;
        while (index < end && body[index].Definition.Opcode is Label or Line)
        {
            index++;
        }

        if (allowReturn)
        {
            while (index < end && body[index].Definition.Opcode is (Move or Swap or TestHeap or PutTuple or PutList)
                && body[index].Operands.All(operand => operand.Kind != BeamOperandParameters.Y))
            {
                index++;
            }

            if (index < end && body[index].Definition.Opcode == Return)
            {
                return true;
            }
        }

        return index < end && body[index].Definition.Opcode is BadMatch or BadRecord or CaseEnd or IfEnd
            && body[index].Operands.All(operand => operand.Kind != BeamOperandParameters.Y);
    }

    private static void AddStackInterference(BigInteger slots, BigInteger other, BigInteger[] conflicts)
    {
        while (!slots.IsZero)
        {
            int slot = (int)BigInteger.TrailingZeroCount(slots);
            conflicts[slot] |= other;
            slots &= slots - BigInteger.One;
        }
    }
}
