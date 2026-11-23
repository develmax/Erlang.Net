using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record OwnedProjectionCopy(int Projection, int Branch, int Allocation);

    private byte[]? ForwardOwnedBranchProjectionCopies(ReadOnlyMemory<byte> code, int labelCount) =>
        ModuleOptimizationDisabled || !SsaTypeInferenceEnabled ? null
            : ForwardOwnedBranchProjectionCopiesCore(code, labelCount);

    private byte[]? ForwardOwnedBranchProjectionCopiesCore(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (!code.Span.Contains(GetTupleElement) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var selected = new Dictionary<int, int>();
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        var body = decoded.Body;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (ReadOwnedProjectionCopy(body, start, end) is not { } plan)
            {
                continue;
            }

            var temporary = body[plan.Projection].Operands[^1];
            int copy = plan.Projection + 1;
            int registerOffset = body[copy].Offset - CompactRegisterCountWidth((int)temporary.Value);
            output.Write(code.Span[copied..registerOffset]);
            BeamCompactWriter.Write(output, XRegisterTag, body[copy].Operands[1].Value);
            WriteFrameInitialization(output, [(int)temporary.Value]);
            copied = body[copy + 1].Offset;
            selected.Add((int)body[plan.Branch].Operands[0].Value, plan.Allocation - plan.Branch);
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return CompactSelectedBranchFrames(output.ToArray(), labelCount, selected);
    }

    private byte[]? CompactSelectedBranchFrames(
        byte[] rewritten,
        int labelCount,
        IReadOnlyDictionary<int, int> selected,
        bool reuseValueLifetimes = false
    )
    {
        if (DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return null;
        }

        // Each selected suffix belongs to a different function. Applying later
        // edits first preserves the saved offsets of earlier functions.
        int entry = updated.Body.Count - 1;
        for (int start = entry - 1; start >= 0; start--)
        {
            BeamInstruction instruction = updated.Body[start];
            if (instruction.Definition.Opcode == FunctionInfo)
            {
                entry = start;
            }
            else if (instruction is { Definition.Opcode: Label, Operands: [var label] }
                && selected.TryGetValue((int)label.Value, out int allocation))
            {
                var region = updated.Body.Skip(start).Take(entry - start + 1).ToArray();
                if (RewriteDisjointFrameLayouts(
                    rewritten,
                    region,
                    updated.StackOperands,
                    atomicOnly: false,
                    completeTrimsOnly: true,
                    entriesToCompact: null,
                    reuseOccupiedEntrySlots: false,
                    onlyAllocation: allocation,
                    reuseSelectedLifetimes: reuseValueLifetimes
                ) is not { } compacted)
                {
                    return null;
                }

                rewritten = compacted;
            }
        }

        return rewritten;
    }

    private static OwnedProjectionCopy? ReadOwnedProjectionCopy(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        if (end - start < 5 || Enumerable.Range(start, end - start)
            .Count(index => body[index].Definition.Opcode is Allocate or AllocateHeap) < 2
            || Enumerable.Range(start, end - start).Any(index =>
                body[index].Definition.Name is "debug_line" or "executable_line"))
        {
            return null;
        }

        int branch = -1;
        int allocation = -1;
        int examinedAllocation = -1;
        HashSet<int>? owned = null;
        for (int index = start; index + 2 < end; index++)
        {
            if (body[index].Definition.Opcode == Label)
            {
                branch = index;
                allocation = -1;
                continue;
            }

            if (body[index].Definition.Opcode is Allocate or AllocateHeap)
            {
                allocation = index;
                continue;
            }

            if (branch <= start || allocation < 0 || body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [_, _, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                } || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null }]
                } || source != slot)
            {
                continue;
            }

            if (examinedAllocation != allocation)
            {
                examinedAllocation = allocation;
                owned = ReadExclusiveBranchFrame(
                    body,
                    start,
                    branch,
                    allocation,
                    end
                );
            }

            if (owned is null || !owned.Contains(index) || !owned.Contains(index + 1))
            {
                continue;
            }

            if (owned.Where(at => body[at].Definition.Opcode != InitYRegisters)
                .SelectMany(at => body[at].Operands).Count(operand => SameProjectionRegister(operand, slot)) == 2)
            {
                return new OwnedProjectionCopy(index, branch, allocation);
            }
        }

        return null;
    }

    private static HashSet<int>? ReadExclusiveBranchFrame(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int branch,
        int allocation,
        int end
    )
    {
        if (EntryFrameFailureLabel(body, branch) >= 0
            || ReadDisjointFrameOwnership(
                body,
                branch,
                end,
                allowCompleteTrims: true
            ) is not { } frame)
        {
            return null;
        }

        var owned = Enumerable.Range(branch, end - branch)
            .Where(at => frame.Owners[at - branch] == allocation).ToHashSet();
        var labels = owned.Where(at => body[at].Definition.Opcode == Label)
            .Select(at => body[at].Operands[0].Value).ToHashSet();

        return owned.Any(at => body[at].Definition.Opcode == TrimFrame
                || at != allocation + 1 && !owned.Contains(at - 1) && !EndsBeamBlock(body[at - 1].Definition.Opcode))
            || Enumerable.Range(start, end - start).Any(at => !owned.Contains(at)
                && body[at].Operands.Any(operand =>
                    operand.Kind == BeamOperandParameters.Label && labels.Contains(operand.Value)))
            ? null : owned;
    }
}
