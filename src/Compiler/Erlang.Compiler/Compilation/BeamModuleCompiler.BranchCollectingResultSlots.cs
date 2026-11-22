using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseBranchCollectingResultSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifOne)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, _) = decoded;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var plans = new Dictionary<int, (int Count, int[] Slots)>();
        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int frame = FindLinearFrameSuffix(body, start, end);
            if (frame < 0 || frame + 5 >= end || body[start].Definition.Opcode != Label
                || body[frame].Definition.Opcode != Allocate
                || body[frame + 1] is not
                {
                    Definition.Opcode: InitYRegisters,
                    Operands: [_, { Kind: BeamOperandParameters.Y, TypeIndex: null } scratch]
                })
            {
                continue;
            }

            for (int spill = frame + 3; spill + 4 < end; spill++)
            {
                if (body[spill - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || !IsMove(spill, x0, scratch)
                    || body[spill + 1] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [var rejected, var tested, { Kind: BeamOperandParameters.Atom }]
                    } || tested != x0)
                {
                    continue;
                }

                int producer = spill + 2;
                while (producer < end && body[producer].Definition.Opcode == Line)
                {
                    producer++;
                }

                if (producer + 2 >= end || body[producer] is not
                    {
                        Definition.Opcode: GcBifOne,
                        Operands: [{ Value.IsZero: true }, { Value.IsZero: true }, _,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } retired, var result]
                    } bif || retired == scratch || result != x0 || !IsMove(producer + 1, x0, scratch)
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                        spill + 1,
                        (int)rejected.Value,
                        out int retry
                    ) || retry <= producer + 1
                    || ReadDisjointFrameOwnership(body, start, end) is not { } owner
                    || owner.Uses.Count != 1 || owner.Owners[producer - start] != frame
                    || !HasClosedAlternate(
                        retry,
                        end,
                        scratch,
                        retired
                    )
                    || body.Skip(frame + 2).Take(spill - frame - 2).Any(instruction =>
                        instruction.Definition.Opcode is not (Move or Line or LocalCall or ExternalCall)
                        || instruction.Operands.Any(operand => SameProjectionRegister(operand, scratch))))
                {
                    continue;
                }

                int move = body[producer + 1].Offset;
                if (code.Span[move - 1] != XRegisterTag || code.Span[move + 1] != XRegisterTag)
                {
                    continue;
                }

                // The selected edge takes an atom into the collecting instruction. The
                // rejected edge only copies outgoing arguments before releasing the frame.
                // The atom save and initial nil are therefore dead on both edges.
                var proposed = body.Take(end).ToList();
                proposed[producer] = bif with { Operands = [.. bif.Operands.Take(4), scratch] };
                proposed[producer + 1] = proposed[producer + 1] with { Operands = [scratch, x0] };
                proposed.RemoveAt(spill);
                proposed.RemoveAt(frame + 1);
                if (PlanBranchStackSlots(
                    proposed,
                    start,
                    proposed.Count,
                    linearAllocation: frame,
                    allowGcBifExceptionExit: true
                ) is not { Trim: null } plan
                    || plan.Count != plan.Slots.Length - 1
                    || plan.Slots[(int)scratch.Value] != plan.Slots[(int)retired.Value]
                    || plan.Slots.Where((_, index) => scratch.Value != index).Distinct().Count() != plan.Count
                    || plan.Slots.Any(slot => slot < 0))
                {
                    continue;
                }

                // Keep the order of the surviving entry captures. Only the shared color
                // changes its physical position, because its later copy loses a slot.
                int scratchSlot = (int)scratch.Value;
                int survivor = (int)retired.Value;
                int[] layout = Enumerable.Range(0, plan.Slots.Length).Select(slot => PhysicalSlot(
                    slot == scratchSlot ? survivor : slot
                )).ToArray();
                plans.Add((int)body[start].Operands[0].Value, (plan.Count, layout));
                edits.Add(new(
                    body[frame + 1].Offset,
                    body[frame + 2].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));
                edits.Add(new(
                    body[spill].Offset,
                    body[spill + 1].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));

                int moveEnd = body[producer + 2].Offset;
                edits.Add(new(
                    move - 1,
                    move,
                    YRegisterTag,
                    scratchSlot
                ));
                edits.Add(new(
                    move + 1,
                    move + 2,
                    YRegisterTag,
                    scratchSlot
                ));
                edits.Add(new(
                    move + 2,
                    moveEnd,
                    XRegisterTag,
                    0
                ));
                break;

                int PhysicalSlot(int slot) => slot > scratchSlot ? slot - 1 : slot;
            }
        }

        if (plans.Count == 0 || ApplyDuplicatedFrameReadEdits(code, edits) is not { } rewritten
            || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } compacted)
        {
            return null;
        }

        var compactedEntries = plans.Keys.ToHashSet();
        byte[]? allocated = RewriteReusedStackSlots(
            rewritten,
            compacted.Body,
            compacted.StackOperands,
            useValueLiveness: false,
            closureEntries: null,
            atomicOnly: false,
            entriesToCompact: compactedEntries,
            provenBranchLayouts: plans
        );
        if (allocated is null)
        {
            return null;
        }

        return ScheduleCompactedCallArgumentRoots(allocated, labelCount, compactedEntries) ?? allocated;

        bool IsMove(int index, BeamOperand source, BeamOperand target) => body[index] is
        { Definition.Opcode: Move, Operands: [var input, var output] } && input == source && output == target;

        bool HasClosedAlternate(
            int begin,
            int end,
            BeamOperand scratch,
            BeamOperand retired
        )
        {
            int index = begin + 1;
            for (; index < end; index++)
            {
                var instruction = body[index];
                if (instruction.Definition.Opcode is Label or Line)
                {
                    continue;
                }

                if (instruction is
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y or BeamOperandParameters.X } source,
                        { Kind: BeamOperandParameters.X }]
                    } && !SameProjectionRegister(source, scratch) && !SameProjectionRegister(source, retired))
                {
                    continue;
                }

                return instruction.Definition.Opcode is TailLocalCallWithFrame or TailExternalCallWithFrame;
            }

            return false;
        }
    }
}
