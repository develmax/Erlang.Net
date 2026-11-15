using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Count, int[] Slots)? PlanBranchStackSlots(IReadOnlyList<BeamInstruction> body, int entry, int end)
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
            if (instruction.Definition.Opcode != Line
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
        var labels = new Dictionary<int, int>();
        var firstUse = new int[frame];
        int tryEntry = -1;
        int trySlot = -1;
        Array.Fill(firstUse, int.MaxValue);
        for (int index = start; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode == Label)
            {
                labels.Add((int)instruction.Operands[0].Value, index);
            }

            if (instruction.Definition.Opcode == TryEnter)
            {
                if (tryEntry >= 0
                    || instruction.Operands is not [{ Kind: BeamOperandParameters.Y } tag, { Kind: BeamOperandParameters.Label }]
                    || tag.Value >= frame)
                {
                    return null;
                }

                tryEntry = index;
                trySlot = (int)tag.Value;
            }

            if (instruction.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            foreach (BeamOperand operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y && operand.Value < frame)
                {
                    int slot = (int)operand.Value;
                    firstUse[slot] = Math.Min(firstUse[slot], index);
                }
            }
        }

        foreach (int failure in failureLabels)
        {
            if (!labels.TryGetValue(failure, out int target) || !IsFramelessStackAllocationExit(body, target, end))
            {
                return null;
            }
        }

        BigInteger[] masks = Enumerable.Range(0, frame).Select(slot => BigInteger.One << slot).ToArray();
        var effects = new (BigInteger Reads, BigInteger Writes, bool FallsThrough, List<int>? Targets)[end - start];
        var live = new BigInteger[effects.Length + 1];
        var conflicts = new BigInteger[frame];
        bool branched = false;
        bool released = false;
        bool hasBackEdge = false;
        for (int index = end - 1; index >= start; index--)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Allocate or AllocateHeap or TrimFrame or CatchEnter or CatchEnd
                || instruction.Definition.Name == "debug_line")
            {
                return null;
            }

            int frameOperand = LinearFrameSizeOperand(opcode);
            if (frameOperand >= 0)
            {
                if (instruction.Operands[frameOperand].Value != frame)
                {
                    return null;
                }

                released = true;
            }

            bool fallsThrough = !EndsBeamBlock(opcode) && opcode != IfEnd;
            List<int>? targets = null;
            bool call = opcode is LocalCall or TailLocalCall or TailLocalCallWithFrame;
            bool initializes = opcode == InitYRegisters;
            if (opcode is TryEnd or TryCase && (tryEntry < 0
                || instruction.Operands is not [{ Kind: BeamOperandParameters.Y } tag]
                || tag.Value != trySlot))
            {
                return null;
            }

            int destination = opcode switch
            {
                LoopReceive => 1,
                TryEnter => 0,
                WaitTimeout or GetMapElements or BinaryMatch or TryEnd or TryCase or TryCaseEnd or Raise => -1,
                _ => StackAllocationDestination(instruction)
            };
            BigInteger reads = BigInteger.Zero;
            BigInteger writes = BigInteger.Zero;
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (!call && opcode != TryEnter
                    && operand.Kind == BeamOperandParameters.Label && operand.Value != clauseFailure)
                {
                    if (!labels.TryGetValue((int)operand.Value, out int target))
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

                if (operand.Value >= frame || frameOperand >= 0
                    || !initializes && destination == int.MinValue
                        && opcode is not (SelectValue or SelectTupleArity) && !IsEntryFramePredicate(opcode))
                {
                    return null;
                }

                int slot = (int)operand.Value;
                if (initializes && firstUse[slot] == int.MaxValue)
                {
                    continue;
                }

                if (opcode is TryEnd or TryCase)
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

        var exceptional = tryEntry >= 0 ? new HashSet<int>() : null;
        if (exceptional is not null && !AddTryEdges(exceptional) || !branched || !released)
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
        Array.Fill(slots, -1);
        foreach (int slot in Enumerable.Range(0, frame)
            .Where(slot => firstUse[slot] != int.MaxValue)
            .OrderBy(slot => slot == trySlot ? int.MinValue : firstUse[slot]))
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

        return (groups.Count, slots);

        bool AddTryEdges(HashSet<int> protectedInstructions)
        {
            if (!labels.TryGetValue((int)body[tryEntry].Operands[1].Value, out int handler))
            {
                return false;
            }

            int entry = tryEntry - start;
            int target = handler - start;
            int handle = handler;
            while (handle < end && body[handle].Definition.Opcode is Label or Line)
            {
                handle++;
            }

            if (handle == end || body[handle].Definition.Opcode != TryCase)
            {
                return false;
            }

            const int NoHandler = 0;
            const int Installed = 1;
            const int HandlingException = 2;
            // An ordinary path cannot enter try_case or merge with an exception
            // before the handler is removed.
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
                bool exits = opcode is TryEnd or TryCase;
                if (enters && handlerState != NoHandler
                    || opcode == TryEnd && handlerState != Installed
                    || opcode == TryCase && (handlerState != HandlingException || start + index != handle)
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

    private static bool IsFramelessStackAllocationExit(IReadOnlyList<BeamInstruction> body, int target, int end)
    {
        int index = target + 1;
        while (index < end && body[index].Definition.Opcode is Label or Line)
        {
            index++;
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
