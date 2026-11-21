using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReallocateLeafFrameRegisters(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        bool afterCanonicalization = false,
        Func<BeamInstruction, bool>? isArithmetic = null,
        Func<BeamInstruction, bool>? canDiscardExternalTailFrame = null,
        bool ownedBranchesOnly = false,
        Func<BeamInstruction, bool>? isBodyHead = null
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

            Dictionary<int, int>? heapRoots = null;
            int? returningCall = afterCanonicalization && !ownedBranchesOnly && !ModuleOptimizationDisabled
                ? FindSingleFrameOnlyCall(body, start, end) : null;
            int?[]? registers = ownedBranchesOnly ? null : PlanLeafFrameRegisters(
                body,
                start,
                end,
                arity,
                afterCanonicalization,
                isArithmetic,
                out heapRoots,
                canDiscardExternalTailFrame: canDiscardExternalTailFrame,
                returningCall: returningCall,
                isBodyHead: isBodyHead
            );
            if (registers is not null)
            {
                WritePlan(
                    start,
                    end,
                    registers,
                    heapRoots,
                    keepCallFrame: returningCall.HasValue
                );
            }
            else if (ownedBranchesOnly)
            {
                int entryAllocation = FindEntryFrameAllocation(body, start, end);
                (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
                for (int allocation = start + 2; allocation + 2 < end; allocation++)
                {
                    BeamInstruction frame = body[allocation];
                    if (allocation == entryAllocation || frame.Definition.Opcode is not (Allocate or AllocateHeap)
                        || frame.Operands[0].Value.IsZero)
                    {
                        continue;
                    }

                    int branchEnd = ReadLinearLeafFrameTail(body, allocation, end);
                    if (branchEnd < 0
                        || (ownership ??= ReadDisjointFrameOwnership(body, start, end)) is not { } branchOwnership
                        || branchOwnership.Owners[allocation - start] != UnframedOwner
                        || !branchOwnership.Uses.ContainsKey(allocation))
                    {
                        continue;
                    }

                    int live = (int)frame.Operands[^1].Value;
                    if (PlanLeafFrameRegisters(
                        body,
                        allocation,
                        branchEnd,
                        live,
                        afterCanonicalization,
                        isArithmetic,
                        out var branchRoots,
                        canDiscardExternalTailFrame: canDiscardExternalTailFrame,
                        allocationAtStart: true
                    ) is { } branchRegisters)
                    {
                        WritePlan(
                            allocation,
                            branchEnd,
                            branchRegisters,
                            branchRoots
                        );
                        allocation = branchEnd - 1;
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

        void WritePlan(
            int first,
            int limit,
            int?[] slots,
            Dictionary<int, int>? roots,
            bool keepCallFrame = false
        )
        {
            for (int index = first; index < limit; index++)
            {
                BeamInstruction instruction = body[index];
                int next = body[index + 1].Offset;
                while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                {
                    operand++;
                }

                byte opcode = instruction.Definition.Opcode;
                if (keepCallFrame && opcode is Allocate or AllocateHeap)
                {
                    int frame = (int)instruction.Operands[0].Value;
                    output.Write(code.Span[copied..(instruction.Offset + 1)]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                    copied = instruction.Offset + 1 + CompactRegisterCountWidth(frame);
                    if (opcode == Allocate)
                    {
                        continue;
                    }
                }

                if (keepCallFrame && opcode is Deallocate or TailLocalCallWithFrame or TailExternalCallWithFrame)
                {
                    int frame = (int)instruction.Operands[^1].Value;
                    output.Write(code.Span[copied..(next - CompactRegisterCountWidth(frame))]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                    copied = next;
                    continue;
                }

                if (opcode is Allocate or Deallocate or InitYRegisters)
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

                if (roots is not null && roots.TryGetValue(index, out int live))
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
                        if (!keepCallFrame && instruction.Definition.Opcode == AllocateHeap)
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
                    BeamCompactWriter.Write(output, XRegisterTag, slots[slot]!.Value);
                    copied = to;
                }
            }
        }
    }

    private static int? FindSingleFrameOnlyCall(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        int allocation = FindEntryFrameAllocation(
            body,
            start,
            end,
            allowUnframedProjections: true
        );
        if (allocation < 0)
        {
            return null;
        }

        int failure = EntryFrameFailureLabel(body, start);
        int? call = null;
        bool finished = false;
        for (int index = start + 1; index < end; index++)
        {
            var instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (finished && opcode is Label or Line)
            {
                continue;
            }
            // Unframed entry checks may report function_clause with the original arguments.
            // Their success falls through to the same continuation allocation.
            bool checkedEntry = index < allocation && failure >= 0 && IsTrimTest(opcode)
                && instruction.Operands[0] is { Kind: BeamOperandParameters.Label } rejected
                && rejected.Value == failure;
            if (finished || opcode is Label or Jump or SelectValue or SelectTupleArity
                || IsTrimTest(opcode) && !checkedEntry || instruction.Operands.Any(operand =>
                    operand.Kind == BeamOperandParameters.Y && operand.TypeIndex is not null))
            {
                return null;
            }

            if (opcode is LocalCall or ExternalCall)
            {
                if (call.HasValue)
                {
                    return null;
                }

                call = index;
            }
            else if (call.HasValue)
            {
                if (opcode is not (Line or Move or TestHeap or PutTuple or PutList
                    or Deallocate or Return or TailLocalCallWithFrame or TailExternalCallWithFrame)
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    return null;
                }

                finished = opcode is TailLocalCallWithFrame or TailExternalCallWithFrame
                    || opcode == Return && body[index - 1].Definition.Opcode == Deallocate;
            }
        }

        return finished ? call : null;
    }

    private static int ReadLinearLeafFrameTail(IReadOnlyList<BeamInstruction> body, int allocation, int end)
    {
        int terminal = allocation + 1;
        while (terminal < end && body[terminal].Definition.Opcode is
            Line or InitYRegisters or Move or Swap or GetTupleElement or GetList or GetHead or GetTail or TestHeap or PutTuple or PutList or UpdateRecord)
        {
            terminal++;
        }

        if (terminal >= end || terminal == allocation + 1)
        {
            return -1;
        }

        return body[terminal].Definition.Opcode switch
        {
            Deallocate when terminal + 1 < end && body[terminal + 1].Definition.Opcode == Return => terminal + 2,
            TailLocalCallWithFrame or TailExternalCallWithFrame => terminal + 1,
            _ => -1
        };
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
        int postCallFrameSize = 0,
        bool allocationAtStart = false,
        Func<int, bool>? canUsePostCallRegister = null,
        int? returningCall = null,
        Func<BeamInstruction, bool>? isBodyHead = null
    )
    {
        heapRoots = null;
        const int xCount = BeamOperandParameters.MaximumRegister;
        int allocation = allocationAtStart ? start
            : postCallSlots is null
                ? FindEntryFrameAllocation(
                    body,
                    start,
                    end,
                    allowUnframedProjections: returningCall.HasValue
                )
                : -1;
        if (allocation < 0 && postCallSlots is null)
        {
            return null;
        }

        int slots = postCallSlots is null ? (int)body[allocation].Operands[0].Value : postCallFrameSize;
        // A zero-cell frame can still be an artifact of ordinary-call planning.
        // Reuse the leaf instruction proof and require consistent allocation ownership.
        if (slots >= xCount
            || slots == 0 && (returningCall.HasValue || postCallSlots is not null
                || ReadDisjointFrameOwnership(body, start, end) is null))
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
                && (returningCall.HasValue || isolatedTailBranch || canDiscardExternalTailFrame?.Invoke(instruction) == true);
            bool returning = start + index == returningCall;
            bool arithmetic = opcode == GcBifTwo && isArithmetic?.Invoke(instruction) == true;
            bool guardedBif = IsLeafGuardBif(instruction);
            // This BIF neither allocates nor clobbers X registers. Its failure leaves the leaf by throwing.
            bool bodyHead = !returningCall.HasValue && isBodyHead?.Invoke(instruction) == true;
            bool recordUpdate = allocationAtStart && opcode == UpdateRecord;
            // The preceding heap check owns allocation, and a closed fun only defines its destination.
            bool emptyFrameClosure = slots == 0 && instruction is
            {
                Definition.Opcode: MakeFun,
                Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null },
                { Kind: BeamOperandParameters.Count, Value.IsZero: true }]
            };
            bool callClosure = returningCall.HasValue && opcode == MakeFun && instruction.Operands.Count >= 3
                && instruction.Operands[1].Kind is BeamOperandParameters.X or BeamOperandParameters.Y
                && instruction.Operands[2].Kind == BeamOperandParameters.Count;
            needsCanonicalization |= opcode == PutTuple || arithmetic || bodyHead;
            if ((!returning && !localTail && !externalTail && !recordUpdate && !emptyFrameClosure && !callClosure
                    && !IsFramelessInstruction(instruction))
                || (opcode is Allocate or AllocateHeap && start + index != allocation)
                || (!afterCanonicalization && opcode == PutTuple)
                || opcode is GetMapElements or GcBifOne or GcBifThree
                || (opcode is BifOne or BifTwo or BifThree && !guardedBif && !bodyHead)
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
                _ when returning || localTail || externalTail => (BigInteger.One << (int)instruction.Operands[0].Value) - 1,
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
                    || (emptyFrameClosure || callClosure) && position == 1
                    || recordUpdate && position == StackWriteOperand(instruction)
                    || (guardedBif || bodyHead || opcode is Move or GetHead or GetTail or GetTupleElement or PutList or GcBifTwo)
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

        if (afterCanonicalization && !needsCanonicalization && postCallSlots is null && !allocationAtStart
            && !returningCall.HasValue)
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
                if (start + index != returningCall && instruction.Definition.Opcode != TailLocalCallWithFrame
                    && operand.Kind == BeamOperandParameters.Label)
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
                if (start + index == returningCall)
                {
                    // A returning call supplies only X0, with no former register values preserved here.
                    after = BigInteger.Zero;
                }
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

        if (returningCall is int call && (liveOut[call - start] & ~BigInteger.One) != BigInteger.Zero)
        {
            return null;
        }

        var xMask = (BigInteger.One << xCount) - 1;
        if (isolatedTailBranch || allocationAtStart)
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
                registers[other] == x && Interferes(xCount + other))
                && canUsePostCallRegister?.Invoke(x) != false;

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
            conditionalWriteFailures,
            returningCall
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
        int?[] conditionalWriteFailures,
        int? returningCall
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

            if (start + index == returningCall)
            {
                after = BigInteger.One;
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
