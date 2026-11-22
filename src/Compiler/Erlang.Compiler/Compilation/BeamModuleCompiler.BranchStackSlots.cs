using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Count, int[] Slots, (int Index, int OriginalRemoved, int Kept)? Trim)? PlanBranchStackSlots(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        int linearAllocation = -1,
        bool allowGcBifExceptionExit = false,
        bool allowAtomicProjectionAliasing = false,
        IReadOnlyList<int>? trimEpochOwners = null,
        int? firstOwnedInstruction = null,
        bool allowSerialTryHandlers = false,
        bool allowNestedTryHandlers = false
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
        int allocation = linearAllocation >= 0 ? linearAllocation : entry + 1;
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

        int start = firstOwnedInstruction ?? allocation + 1;
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
        var serialEntries = allowSerialTryHandlers ? new Dictionary<int, (int Slot, int Label)>() : null;
        var serialSlots = allowSerialTryHandlers ? new HashSet<int>() : null;
        var tagDepths = allowNestedTryHandlers ? new Dictionary<int, int>() : null;
        if (tagDepths is not null && (serialSlots is null || failureLabels.Count != 0 || Enumerable.Range(start, end - start)
            .Any(index => body[index].Definition.Opcode is LoopReceive or LoopReceiveEnd or Wait or WaitTimeout
                or ReceiveTimeout or RemoveMessage or ReceiveMarkerBind or ReceiveMarkerClear
                or ReceiveMarkerReserve or ReceiveMarkerUse or DebugLine or ExecutableLine
                || body[index].Definition.Name is "recv_mark" or "recv_set")))
        {
            return null;
        }
        if (serialEntries is not null && trim != end)
        {
            return null;
        }

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
                    || instruction.Operands is not
                    [
                    { Kind: BeamOperandParameters.Y } tag,
                    { Kind: BeamOperandParameters.Label } target
                    ]
                    || tag.Value >= frame)
                {
                    return null;
                }

                if (serialEntries is null)
                {
                    handlerEntry = index;
                    handlerSlot = (int)tag.Value;
                    isCatch = instruction.Definition.Opcode == CatchEnter;
                }
                else if (instruction.Definition.Opcode == TryEnter)
                {
                    serialEntries.Add(index - start, ((int)tag.Value, (int)target.Value));
                    serialSlots!.Add((int)tag.Value);
                }
                else
                {
                    return null;
                }
            }

            int shift = InTrimmedEpoch(index) ? removed : 0;
            foreach (BeamOperand operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y && operand.Value < frame - shift)
                {
                    int slot = (int)operand.Value + shift;
                    neededAfterTrim[slot] |= InTrimmedEpoch(index);
                    if (instruction.Definition.Opcode != InitYRegisters)
                    {
                        firstUse[slot] = Math.Min(firstUse[slot], index);
                    }
                }
            }
        }

        foreach (int failure in failureLabels)
        {
            if (!labels.TryGetValue(failure, out int target) || !IsFramelessStackAllocationExit(
                body,
                target,
                end,
                allowReturn: true
            ))
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
            int shift = InTrimmedEpoch(index) ? removed : 0;
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
            if (opcode is TryEnd or TryCase or CatchEnd)
            {
                if (instruction.Operands is not [{ Kind: BeamOperandParameters.Y } tag]
                    || (serialSlots is null
                        ? handlerEntry < 0 || (opcode == CatchEnd) != isCatch || tag.Value != handlerSlot
                        : opcode == CatchEnd || !serialSlots.Contains((int)tag.Value)))
                {
                    return null;
                }
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
                bool exceptionExit = operand.Value.IsZero
                    && ((allowGcBifExceptionExit && handlerEntry < 0 || serialEntries is not null)
                        && opcode is GcBifOne or GcBifTwo or GcBifThree
                        || serialEntries is not null && opcode is BifOne or BifTwo or BifThree);
                if (!call && !exceptionExit && opcode is not (TryEnter or CatchEnter)
                    && operand.Kind == BeamOperandParameters.Label && operand.Value != clauseFailure)
                {
                    if (!labels.TryGetValue((int)operand.Value, out int target)
                        || InTrimmedEpoch(target) != InTrimmedEpoch(index) && !IsFramelessStackAllocationExit(body, target, end))
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
                if (serialSlots?.Contains(slot) == true
                    && opcode is not (TryEnter or TryEnd or TryCase or InitYRegisters))
                {
                    // A reserved try register is never an ordinary Erlang value.
                    return null;
                }

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

        var exceptional = handlerEntry >= 0 || serialEntries is not null ? new HashSet<int>() : null;
        bool[]? handlerFreeInstructions = allowAtomicProjectionAliasing && handlerEntry >= 0 && linearAllocation < 0
            && FindEntryFrameAllocation(body, entry, end) == allocation
                ? new bool[effects.Length]
                : null;
        bool validHandlers = serialEntries is not null
            ? AddSerialTryEdges(serialEntries, exceptional!)
            : exceptional is null || AddHandlerEdges(exceptional);
        if (!validHandlers || linearAllocation < 0 && !branched && trim == end || !released)
        {
            return null;
        }

        if (serialSlots is not null)
        {
            // beam_ssa_pre_codegen:reserve_try_tags uses the same reserved
            // register for disjoint handlers at the same depth.
            BigInteger tokenMask = serialSlots.Aggregate(BigInteger.Zero, (mask, slot) => mask | masks[slot]);
            BigInteger valueMask = ((BigInteger.One << frame) - 1) & ~tokenMask;
            AddStackInterference(tokenMask, valueMask, conflicts);
            AddStackInterference(valueMask, tokenMask, conflicts);
        }

        if (tagDepths is not null)
        {
            // OTP reserves dedicated registers for lexical try depth, even
            // when two different depths are not simultaneously live here.
            BigInteger outer = BigInteger.Zero;
            BigInteger inner = BigInteger.Zero;
            foreach (var (slot, depth) in tagDepths)
            {
                if (depth == 0)
                {
                    outer |= masks[slot];
                }
                else
                {
                    inner |= masks[slot];
                }
            }

            AddStackInterference(outer, inner, conflicts);
            AddStackInterference(inner, outer, conflicts);
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
                byte opcode = body[start + index].Definition.Opcode;
                bool initializes = opcode == InitYRegisters;
                // A source whose last use is here can share a result slot when the opcode
                // reads all inputs first. The write mask still keeps independent outputs apart.
                // Leave move interference intact because moves also carry branch joins.
                BeamInstruction instruction = body[start + index];
                // A handler tag has a reserved register role. Keep its slot outside
                // the projection alias proof, even when that role has been closed.
                bool readsBeforeWrite = opcode is GetHead or GetTail
                    or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree
                    || allowAtomicProjectionAliasing && trim == end && (handlerEntry < 0
                        ? IsAtomicStackProjection(instruction)
                        : handlerFreeInstructions?[index] == true && IsAtomicStackAliasCandidate(instruction)
                            && !instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                                && operand.Value == handlerSlot));
                BigInteger occupied = next | (readsBeforeWrite ? BigInteger.Zero : reads)
                    | (initializes ? BigInteger.Zero : writes);
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
            .OrderByDescending(slot => slot == handlerSlot || serialSlots?.Contains(slot) == true || neededAfterTrim[slot])
            .ThenBy(slot => tagDepths?.GetValueOrDefault(slot) ?? 0)
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

        if (groups.Count == 0 || groups.Count >= frame
            || tagDepths is not null && tagDepths.GroupBy(slot => slot.Value)
                .Any(level => level.Select(slot => slots[slot.Key]).Distinct().Count() != 1))
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

        bool InTrimmedEpoch(int index) => trimEpochOwners is null
            ? index > trim
            : trimEpochOwners[index - entry] == trim;

        bool AddSerialTryEdges(Dictionary<int, (int Slot, int Label)> entries, HashSet<int> protectedInstructions)
        {
            var targets = new Dictionary<int, (int Target, int Landing, int Slot)>();
            foreach (var (at, (slot, label)) in entries)
            {
                if (!labels.TryGetValue(label, out int target))
                {
                    return false;
                }

                int landing = target;
                while (landing < end && body[landing].Definition.Opcode is Label or Line)
                {
                    landing++;
                }

                if (landing == end || body[landing] is not { Definition.Opcode: TryCase, Operands: [var tag] }
                    || tag.Value != slot)
                {
                    return false;
                }

                targets.Add(at + 1, (target - start, landing - start, slot));
            }

            var parentByTry = tagDepths is not null ? new int[effects.Length + 1] : null;
            if (tagDepths is not null && targets.Values.Select(target => target.Target).Distinct().Count() != targets.Count)
            {
                return false;
            }

            var reached = new int?[effects.Length];
            var pending = new Stack<(int Index, int Owner)>();
            pending.Push((0, 0));
            while (pending.TryPop(out var state))
            {
                var (index, active) = state;
                if (index < 0 || index >= effects.Length)
                {
                    return false;
                }

                if (reached[index] is int prior)
                {
                    if (prior != active)
                    {
                        return false;
                    }

                    continue;
                }

                reached[index] = active;
                BeamInstruction instruction = body[start + index];
                byte opcode = instruction.Definition.Opcode;
                int next = active;
                if (tagDepths is not null && active < 0 && opcode is not (Label or Line or TryCase))
                {
                    return false;
                }

                if (opcode == TryEnter)
                {
                    next = index + 1;
                    int slot = targets[next].Slot;
                    if (active != 0 && (parentByTry is null || active < 0
                        || parentByTry[active] != 0 || targets[active].Slot == slot))
                    {
                        return false;
                    }

                    if (tagDepths is not null)
                    {
                        int depth = active == 0 ? 0 : 1;
                        if (tagDepths.TryGetValue(slot, out int previousDepth) && previousDepth != depth)
                        {
                            return false;
                        }

                        // Each reachable installation has one incoming state.
                        // Its recorded parent makes the signed top identity describe the full stack.
                        tagDepths[slot] = depth;
                        parentByTry![next] = active;
                    }

                    pending.Push((targets[next].Target, -next));
                }
                else if (opcode == TryEnd)
                {
                    if (active <= 0 || instruction.Operands[0].Value != targets[active].Slot)
                    {
                        return false;
                    }

                    next = parentByTry?[active] ?? 0;
                }
                else if (opcode == TryCase)
                {
                    if (active >= 0 || index != targets[-active].Landing)
                    {
                        return false;
                    }

                    next = parentByTry?[-active] ?? 0;
                }
                else if (active != 0 && (opcode is Return or TailLocalCall or TailExternalCall || LinearFrameSizeOperand(opcode) >= 0))
                {
                    return false;
                }

                var effect = effects[index];
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

                if (active > 0 && opcode != TryEnd)
                {
                    int failure = targets[active].Target;
                    protectedInstructions.Add(index);
                    (effect.Targets ??= []).Add(failure);
                    effects[index] = effect;
                    pending.Push((failure, -active));
                    hasBackEdge |= failure <= index;
                    branched = true;
                }
            }

            return tagDepths is null
                ? entries.Keys.All(index => reached[index] == 0)
                : tagDepths.Values.Contains(1) && entries.Keys.All(index => reached[index] is >= 0);
        }

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
                if (handlerFreeInstructions is not null)
                {
                    handlerFreeInstructions[index] = handlerState == NoHandler;
                }

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
            while (index < end && body[index].Definition.Opcode is (Line or Move or Swap or TestHeap or PutTuple or PutList)
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

    private static (int Count, int[] Slots, (int Index, int OriginalRemoved, int Kept)? Trim)? PlanAtomicProjectionStackSlots(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        int linearAllocation = -1,
        bool allowGcBifExceptionExit = false
    )
    {
        int start = linearAllocation >= 0 ? linearAllocation + 1 : entry;
        if (start >= end || !Enumerable.Range(start, end - start).Any(index => IsAtomicStackAliasCandidate(body[index]))
            || PlanBranchStackSlots(
                body,
                entry,
                end,
                linearAllocation,
                allowGcBifExceptionExit,
                allowAtomicProjectionAliasing: true
            ) is not { Trim: null } candidate)
        {
            return null;
        }

        var original = PlanBranchStackSlots(
            body,
            entry,
            end,
            linearAllocation,
            allowGcBifExceptionExit
        );

        return original is null || candidate.Count < original.Value.Count ? candidate : null;
    }

    private static bool IsAtomicStackAliasCandidate(BeamInstruction instruction) =>
        IsAtomicStackProjection(instruction)
        && instruction.Operands[0] is { Kind: BeamOperandParameters.Y } source
        && instruction.Operands.Skip(1).Any(operand => operand.Kind == BeamOperandParameters.Y
            && !SameProjectionRegister(source, operand));

    private static bool IsAtomicStackProjection(BeamInstruction instruction) =>
        instruction.Definition.Opcode == GetTupleElement
        || instruction is { Definition.Opcode: GetList, Operands: [_, var head, var tail] }
            && !SameProjectionRegister(head, tail);

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
