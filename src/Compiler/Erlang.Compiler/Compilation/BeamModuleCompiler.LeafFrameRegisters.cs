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
        Func<BeamInstruction, bool>? isBodyProjection = null,
        Func<BeamInstruction, bool>? isColdMapErrorTail = null,
        bool ownedMapIteratorsOnly = false
    )
        => ReallocateLeafFrameRegistersCore(
            code,
            body,
            stackOperands,
            afterCanonicalization,
            isArithmetic,
            canDiscardExternalTailFrame,
            ownedBranchesOnly,
            isBodyProjection,
            isColdMapErrorTail,
            ownedMapIteratorsOnly,
            guardedReturningEntries: null
        );


    private byte[]? ReallocateLeafFrameRegistersCore(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        bool afterCanonicalization,
        Func<BeamInstruction, bool>? isArithmetic,
        Func<BeamInstruction, bool>? canDiscardExternalTailFrame,
        bool ownedBranchesOnly,
        Func<BeamInstruction, bool>? isBodyProjection,
        Func<BeamInstruction, bool>? isColdMapErrorTail,
        bool ownedMapIteratorsOnly,
        IReadOnlySet<int>? guardedReturningEntries
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        int start = 0;
        int arity = 0;
        var iteratorAtomNames = ownedMapIteratorsOnly
            ? atoms.Where(pair => MapEmittedAtom(pair.Value) > 0)
                .ToDictionary(pair => MapEmittedAtom(pair.Value), pair => pair.Key)
            : null;
        bool planLeaves = !ownedBranchesOnly && !ownedMapIteratorsOnly;
        BeamProjectionFlow? linearArithmeticFlow = null;
        for (int end = 0; end < body.Count; end++)
        {
            BeamInstruction boundary = body[end];
            if (boundary.Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            Dictionary<int, int>? heapRoots = null;
            bool guardedReturning = guardedReturningEntries is not null
                && start < end && body[start].Definition.Opcode == Label
                && guardedReturningEntries.Contains((int)body[start].Operands[0].Value);
            int? returningCall = guardedReturning
                ? FindGuardedReturningSelector(body, start, end)
                : guardedReturningEntries is null && planLeaves && afterCanonicalization && !ModuleOptimizationDisabled
                    ? FindSingleFrameOnlyCall(body, start, end) ?? FindSingleFrameBranchCall(body, start, end)
                    : null;
            bool tryLeaf = guardedReturningEntries is null ? planLeaves : returningCall.HasValue;
            int?[]? registers = !tryLeaf ? null : PlanLeafFrameRegisters(
                body,
                start,
                end,
                arity,
                afterCanonicalization,
                isArithmetic,
                out heapRoots,
                canDiscardExternalTailFrame: canDiscardExternalTailFrame,
                returningCall: returningCall,
                isBodyProjection: isBodyProjection,
                isColdMapErrorTail: isColdMapErrorTail
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
            else if (iteratorAtomNames is not null && start > 0
                && iteratorAtomNames.TryGetValue((int)body[start - 1].Operands[1].Value, out string? iteratorOwner)
                && PlanMapIteratorBranchFrame(
                    body,
                    start,
                    end,
                    arity,
                    iteratorOwner,
                    isArithmetic,
                    canDiscardExternalTailFrame,
                    isColdMapErrorTail!
                ) is { } iterator)
            {
                WritePlan(
                    start,
                    iterator.Cut,
                    iterator.Registers,
                    iterator.Roots
                );
                copied = WriteMapIteratorBranchFrame(
                    code,
                    body,
                    output,
                    copied,
                    iterator
                );
            }
            else if (ownedBranchesOnly)
            {
                int entryAllocation = FindEntryFrameAllocation(body, start, end);
                (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
                bool checkedOwnership = false;
                for (int allocation = start + 2; allocation + 2 < end; allocation++)
                {
                    BeamInstruction frame = body[allocation];
                    if (allocation == entryAllocation || frame.Definition.Opcode is not (Allocate or AllocateHeap))
                    {
                        continue;
                    }

                    int branchEnd = ReadLinearLeafFrameTail(
                        body,
                        allocation,
                        end,
                        afterCanonicalization ? isArithmetic : null,
                        out int next,
                        out bool hasArithmetic
                    );
                    bool emptyFrame = frame.Operands[0].Value.IsZero;
                    if (emptyFrame && (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
                        || branchEnd < 0 || (frame.Definition.Opcode == Allocate
                            ? !IsOwnedZeroFrameProjectionReturn(body, allocation, branchEnd)
                            : !IsOwnedZeroFrameConstructorReturn(body, allocation, branchEnd))))
                    {
                        continue;
                    }

                    if (branchEnd < 0 && (next >= end || !ContinuesLeafFrame(body[next].Definition.Opcode)))
                    {
                        continue;
                    }

                    if (!checkedOwnership)
                    {
                        ownership = ReadDisjointFrameOwnership(body, start, end);
                        checkedOwnership = true;
                        if (ownership is null && PlanSharedLeafFrameReturn(
                            body,
                            start,
                            end,
                            afterCanonicalization
                        ) is { } shared)
                        {
                            foreach (var leaf in shared.Owners)
                            {
                                WritePlan(
                                    leaf.Allocation,
                                    leaf.Limit,
                                    leaf.Registers,
                                    leaf.HeapRoots
                                );
                            }

                            output.Write(code.Span[copied..body[shared.FirstRestore].Offset]);
                            copied = body[shared.Jump].Offset;
                            break;
                        }
                    }

                    if (ownership is { } branchOwnership)
                    {
                        if (branchOwnership.Owners[allocation - start] != UnframedOwner
                            || !branchOwnership.Uses.ContainsKey(allocation))
                        {
                            continue;
                        }

                        if (branchEnd < 0)
                        {
                            branchEnd = ReadOwnedLeafFrameTail(
                                body,
                                start,
                                end,
                                allocation,
                                branchOwnership.Owners
                            );
                        }
                    }
                    else if (!hasArithmetic || branchEnd < 0 || body[branchEnd - 1].Definition.Opcode != Return
                        || !(linearArithmeticFlow ??= new BeamProjectionFlow(body)).ReachesLinearFrameUnframed(start, allocation))
                    {
                        continue;
                    }

                    if (branchEnd < 0)
                    {
                        continue;
                    }

                    if (emptyFrame)
                    {
                        // The frame owns no values. Keep the heap check with its original X roots.
                        Dictionary<int, int>? roots = frame.Definition.Opcode == AllocateHeap
                            ? new() { [allocation] = (int)frame.Operands[^1].Value }
                            : null;
                        WritePlan(
                            allocation,
                            branchEnd,
                            [],
                            roots
                        );
                        allocation = branchEnd - 1;
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
                if (ownedMapIteratorsOnly && opcode == Move && instruction.Operands is [var source, var destination]
                    && PromotedRegister(source, slots) is int register
                    && PromotedRegister(destination, slots) == register)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = next;
                    continue;
                }

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
                    else if (opcode == PutMapAssoc)
                    {
                        if (instruction.Operands[1].Kind == BeamOperandParameters.Y)
                        {
                            // The base precedes both the destination and the live count.
                            var (from, to, slot) = stackOperands[operand++];
                            output.Write(code.Span[copied..from]);
                            BeamCompactWriter.Write(output, XRegisterTag, slots[slot]!.Value);
                            copied = to;
                        }

                        copied = WriteLeafMapAssociationRoots(
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

    private static int? PromotedRegister(BeamOperand register, IReadOnlyList<int?> slots) =>
        register.TypeIndex is null
            ? register.Kind switch
            {
                BeamOperandParameters.X => (int)register.Value,
                BeamOperandParameters.Y when register.Value < slots.Count => slots[(int)register.Value],
                _ => null
            }
            : null;

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

    private static int? FindSingleFrameBranchCall(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        int allocation = FindEntryFrameAllocation(
            body,
            start,
            end,
            allowUnframedProjections: true
        );
        if (allocation < 0 || body[allocation].Operands[0].Value.IsZero)
        {
            return null;
        }

        int call = -1;
        int failure = EntryFrameFailureLabel(body, start);
        bool checkedResult = false;
        for (int index = start + 1; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is LocalCall or ExternalCall)
            {
                if (call >= 0 || index <= allocation)
                {
                    return null;
                }

                call = index;
                continue;
            }

            if (opcode is Allocate or AllocateHeap && index != allocation
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    && (operand.TypeIndex is not null || call >= 0))
                || (!IsFramelessInstruction(instruction) && opcode is not (TailLocalCallWithFrame or TailExternalCallWithFrame)))
            {
                return null;
            }

            if (call < 0)
            {
                if (opcode is (Label or Jump or SelectValue or SelectTupleArity)
                    || IsTrimTest(opcode) && (index >= allocation
                        || instruction.Operands[0].Kind != BeamOperandParameters.Label
                        || instruction.Operands[0].Value != failure))
                {
                    return null;
                }
            }
            else
            {
                checkedResult |= IsTrimTest(opcode);
            }
        }

        if (!checkedResult || ReadDisjointFrameOwnership(body, start, end) is not { } ownership
            || ownership.Owners[call - start] != allocation)
        {
            return null;
        }

        // The ordinary call needs its continuation frame. The successor graph
        // contains no Y reads or writes, so only values used before the call
        // need registers. The existing planner checks the X clobber and GC roots.
        return call;
    }

    private static bool ContinuesLeafFrame(byte opcode) =>
        opcode is Label or Jump or SelectValue or SelectTupleArity || IsEntryFramePredicate(opcode);

    private static int ReadLinearLeafFrameTail(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int end,
        Func<BeamInstruction, bool>? isArithmetic,
        out int terminal,
        out bool hasArithmetic
    )
    {
        terminal = allocation + 1;
        hasArithmetic = false;
        while (terminal < end)
        {
            BeamInstruction instruction = body[terminal];
            if (instruction.Definition.Opcode is Line or InitYRegisters or Move or Swap or GetTupleElement
                or GetList or GetHead or GetTail or TestHeap or PutTuple or PutList or UpdateRecord)
            {
                terminal++;
                continue;
            }

            if (instruction.Definition.Opcode != GcBifTwo
                || instruction.Operands[0] is not { Kind: BeamOperandParameters.Failure, Value.IsZero: true }
                || isArithmetic?.Invoke(instruction) != true)
            {
                break;
            }

            hasArithmetic = true;
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

    private static bool IsOwnedZeroFrameConstructorReturn(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int limit
    )
    {
        if (limit - allocation < 4
            || body[limit - 2] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsZero: true }] }
            || body[limit - 1].Definition.Opcode != Return)
        {
            return false;
        }

        for (int index = allocation + 1; index < limit - 2; index++)
        {
            if (body[index].Definition.Opcode is not (Line or Move or PutTuple or PutList)
                || body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsOwnedZeroFrameProjectionReturn(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int limit
    )
    {
        if (limit - allocation < 4
            || body[limit - 2] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsZero: true }] }
            || body[limit - 1].Definition.Opcode != Return)
        {
            return false;
        }

        bool projects = false;
        for (int index = allocation + 1; index < limit - 2; index++)
        {
            if (body[index].Definition.Opcode == Line)
            {
                continue;
            }

            if (body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null },
                    { Kind: BeamOperandParameters.Unsigned },
                    { Kind: BeamOperandParameters.X, TypeIndex: null }
                    ]
                })
            {
                return false;
            }

            projects = true;
        }

        return projects;
    }

    private static int ReadOwnedLeafFrameTail(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int allocation,
        IReadOnlyList<int> owners,
        int sharedExit = -1
    )
    {
        int limit = allocation + 1;
        for (int index = limit; index < end; index++)
        {
            if (owners[index - start] == allocation)
            {
                limit = index + 1;
                if (body[index].Definition.Opcode == Deallocate && limit < end && body[limit].Definition.Opcode == Return)
                {
                    limit++;
                }
            }
        }

        var labels = new HashSet<int>();
        bool returns = false;
        bool constructs = false;
        for (int index = allocation + 1; index < limit; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode == Return && body[index - 1].Definition.Opcode == Deallocate
                && owners[index - start] == UnframedOwner && owners[index - start - 1] == allocation)
            {
                returns = true;
                continue;
            }

            if (owners[index - start] != allocation || opcode is Allocate or AllocateHeap
                || opcode != UpdateRecord && (!IsFramelessInstruction(instruction)
                    || IsTrimTest(opcode) && !IsEntryFramePredicate(opcode))
                || opcode == Deallocate && (index + 1 >= limit || body[index + 1].Definition.Opcode != Return))
            {
                return -1;
            }

            if (opcode == Label)
            {
                labels.Add((int)instruction.Operands[0].Value);
            }

            constructs |= opcode is PutTuple or PutList or UpdateRecord;
        }

        // A branch may leave the owner only through an independently proved shared exit.
        if (!returns || !constructs || body[limit - 1].Definition.Opcode is not (Return or BadMatch or BadRecord or CaseEnd or IfEnd))
        {
            return -1;
        }

        for (int index = allocation + 1; index < limit; index++)
        {
            if (body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                && operand.Value != sharedExit && !labels.Contains((int)operand.Value)))
            {
                return -1;
            }
        }

        return limit;
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
        Func<BeamInstruction, bool>? isBodyProjection = null,
        BigInteger? externalBranchInputs = null,
        Func<BeamInstruction, bool>? isColdMapErrorTail = null,
        bool allowStackMapBase = false
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
        bool hasLocalTail = false;
        bool hasExternalTail = false;
        bool hasOtherExternalTail = false;
        bool hasRegisterMapAssociation = false;
        bool hasLeafMapOperation = false;
        bool allowLeafMapOperations = !returningCall.HasValue && postCallSlots is null
            && !allocationAtStart && !isolatedTailBranch;
        bool hasBodyTupleProjection = false;
        for (int index = 0; index < count; index++)
        {
            BeamInstruction instruction = body[start + index];
            byte opcode = instruction.Definition.Opcode;
            bool localTail = opcode == TailLocalCallWithFrame;
            hasLocalTail |= localTail;
            if (opcode == TailExternalCallWithFrame)
            {
                hasExternalTail = true;
                hasOtherExternalTail |= isColdMapErrorTail?.Invoke(instruction) != true;
            }
            bool externalTail = opcode == TailExternalCallWithFrame
                && (returningCall.HasValue || isolatedTailBranch || canDiscardExternalTailFrame?.Invoke(instruction) == true);
            bool returning = start + index == returningCall;
            bool arithmetic = opcode == GcBifTwo && isArithmetic?.Invoke(instruction) == true;
            bool guardedBif = IsLeafGuardBif(instruction)
                || returningCall is int guardedCall && body[guardedCall + 1].Definition.Opcode == IsTuple
                    && opcode == BifTwo && isBodyProjection?.Invoke(instruction) == true;
            bool mapProjection = allowLeafMapOperations && IsSingleLeafMapProjection(instruction);
            bool mapAssociation = allowLeafMapOperations && IsLeafMapAssociation(
                instruction,
                allowRegisterBase: isColdMapErrorTail is not null,
                allowStackBase: allowStackMapBase
            );
            hasRegisterMapAssociation |= mapAssociation
                && instruction.Operands[1].Kind is BeamOperandParameters.X or BeamOperandParameters.Y;
            hasLeafMapOperation |= mapProjection || mapAssociation;
            // This BIF neither allocates nor clobbers X registers. Its failure leaves the leaf by throwing.
            bool bodyProjection = !returningCall.HasValue && isBodyProjection?.Invoke(instruction) == true;
            hasBodyTupleProjection |= bodyProjection && opcode == BifTwo;
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
            needsCanonicalization |= opcode == PutTuple || arithmetic || bodyProjection || mapAssociation;
            if ((!returning && !localTail && !externalTail && !recordUpdate && !emptyFrameClosure && !callClosure && !mapAssociation
                    && !IsFramelessInstruction(instruction))
                || (opcode is Allocate or AllocateHeap && start + index != allocation)
                || (!afterCanonicalization && opcode == PutTuple)
                || (opcode == GetMapElements && !mapProjection) || opcode is GcBifOne or GcBifThree
                || (opcode is BifOne or BifTwo or BifThree && !guardedBif && !bodyProjection)
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
                    || (recordUpdate || mapAssociation) && position == StackAllocationDestination(instruction)
                    || (guardedBif || mapProjection || bodyProjection
                            || opcode is Move or GetHead or GetTail or GetTupleElement or PutList or GcBifTwo)
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

        bool mapTailNeedsFrame = hasLocalTail || hasExternalTail && (!hasRegisterMapAssociation || hasOtherExternalTail);
        if (hasLeafMapOperation && mapTailNeedsFrame || hasBodyTupleProjection && !hasLocalTail
            || afterCanonicalization && !needsCanonicalization && postCallSlots is null && !allocationAtStart
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
                    if (instruction.Definition.Opcode is GcBifTwo or GetMapElements || IsLeafGuardBif(instruction))
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
        BigInteger externalInputs = postCallSlots is null ? externalBranchInputs ?? arguments : BigInteger.Zero;
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

                // Guarded BIFs and one-key projections write only on success.
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
            bool denseReturningRoots = returningCall is int guardedCall
                && body[guardedCall + 1].Definition.Opcode == IsTuple;
            IEnumerable<int> choices = denseReturningRoots ? [] : preferred[slot] ?? [];
            int candidate = choices.Concat(Enumerable.Range(0, xCount))
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
            if (body[start + index].Definition.Opcode is not (TestHeap or AllocateHeap or GcBifTwo or PutMapAssoc))
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

    private static bool IsLeafMapAssociation(
        BeamInstruction instruction,
        bool allowRegisterBase,
        bool allowStackBase
    ) =>
        instruction is
        {
            Definition.Opcode: PutMapAssoc,
            Operands: [
            { Kind: BeamOperandParameters.Failure },
            var source,
            { Kind: BeamOperandParameters.X, TypeIndex: null },
            { Kind: BeamOperandParameters.Unsigned },
            { Kind: BeamOperandParameters.Count }, _, _, ..
            ]
        }
        && (source.Kind == BeamOperandParameters.LiteralValue
            || allowRegisterBase && source is { Kind: BeamOperandParameters.X, TypeIndex: null }
            || allowStackBase && source is { Kind: BeamOperandParameters.Y, TypeIndex: null });

    private static int WriteLeafMapAssociationRoots(
        ReadOnlyMemory<byte> code,
        Stream output,
        int offset,
        int copied,
        int live
    )
    {
        int operands = offset + 1;
        var cursor = new BeamCursor(code[operands..]);
        // The map base is an extended literal or an untyped register.
        // A promoted base has already been written by the caller.
        _ = BeamCompactReader.Read(cursor);
        var source = BeamCompactReader.Read(cursor);
        if (source.WireTag == BeamOperandParameters.ExtendedTag)
        {
            _ = BeamCompactReader.Read(cursor);
        }

        _ = BeamCompactReader.Read(cursor);
        int liveOffset = operands + cursor.Position;
        _ = BeamCompactReader.Read(cursor);
        output.Write(code.Span[copied..liveOffset]);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);

        return operands + cursor.Position;
    }

    private static bool IsSingleLeafMapProjection(BeamInstruction instruction) =>
        instruction is
        {
            Definition.Opcode: GetMapElements,
            Operands: [
            { Kind: BeamOperandParameters.Label, Value.Sign: > 0 }, _,
            { Kind: BeamOperandParameters.Count }, _,
            { Kind: BeamOperandParameters.X or BeamOperandParameters.Y }
            ]
        };

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
