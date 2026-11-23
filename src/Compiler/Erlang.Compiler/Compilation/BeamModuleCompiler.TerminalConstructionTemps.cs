using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PromoteTerminalConstructionTemps(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !code.Span.Contains(TestHeap) || !code.Span.Contains(Move)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        var edits = new List<CodeEdit>();
        var entries = new HashSet<int>();
        BeamProjectionFlow? flow = null;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int first = start;
            start = end + 1;
            if (first >= end || body[first] is not { Definition.Opcode: Label, Operands: [var entry] }
                || ReadTerminalConstructionTemp(
                    body,
                    first,
                    end,
                    ref flow
                ) is not { } plan)
            {
                continue;
            }

            entries.Add((int)entry.Value);
            bool omitMove = body[plan.Save].Operands[0].Value == plan.Register;
            if (omitMove)
            {
                edits.Add(new CodeEdit(
                    body[plan.Save].Offset,
                    body[plan.Save + 1].Offset,
                    0,
                    0,
                    IsRemoval: true
                ));
            }

            int operand = FirstStackOperandAtOrAfter(stackOperands, body[plan.Save + (omitMove ? 1 : 0)].Offset);
            int stop = body[plan.LastRead + 1].Offset;
            while (operand < stackOperands.Count && stackOperands[operand].Start < stop)
            {
                var (from, to, slot) = stackOperands[operand++];
                if (slot == plan.Slot)
                {
                    edits.Add(new CodeEdit(
                        from,
                        to,
                        XRegisterTag,
                        plan.Register
                    ));
                }
            }
        }

        if (edits.Count == 0 || ApplyDuplicatedFrameReadEdits(code, edits) is not { } rewritten
            || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return null;
        }

        return RewriteDisjointFrames(
            rewritten,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );
    }

    private static (int Slot, int Save, int LastRead, int Register)? ReadTerminalConstructionTemp(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        int allocation = -1;
        int maximumX = 0;
        var uses = new Dictionary<int, List<(int Instruction, bool Writes)>>();
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is CatchEnter or TryEnter or TrimFrame or DebugLine or ExecutableLine)
            {
                return null;
            }

            if (opcode is Allocate or AllocateHeap)
            {
                if (allocation >= 0 || instruction.Operands[0].Value.IsZero)
                {
                    return null;
                }

                allocation = index;
            }

            if (opcode == InitYRegisters)
            {
                continue;
            }

            int write = StackAllocationDestination(instruction);
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand value = instruction.Operands[position];
                if (value.Kind == BeamOperandParameters.X)
                {
                    maximumX = Math.Max(maximumX, (int)value.Value);
                }
                else if (value.Kind == BeamOperandParameters.Y)
                {
                    int slot = (int)value.Value;
                    if (!uses.TryGetValue(slot, out var references))
                    {
                        uses.Add(slot, references = []);
                    }

                    references.Add((index, position == write || opcode == GetList && position == 2));
                }
            }
        }

        if (allocation < 0 || uses.Count == 0)
        {
            return null;
        }

        int frame = (int)body[allocation].Operands[0].Value;
        foreach (var (slot, references) in uses.OrderBy(item => item.Value[0].Instruction))
        {
            if (references is not [(int save, true), ..] || references.Count < 2 || save <= allocation
                || references.Skip(1).Any(reference => reference.Writes)
                || body[save] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X } input, _]
                })
            {
                continue;
            }

            int heap = save - 1;
            while (heap > allocation && IsTerminalConstructionStep(body[heap]))
            {
                heap--;
            }

            int tail = save + 1;
            while (tail < end && IsTerminalConstructionStep(body[tail]))
            {
                tail++;
            }

            int lastRead = references[^1].Instruction;
            if (body[heap].Definition.Opcode != TestHeap || lastRead >= tail || tail >= end
                || !IsTerminalFrameExit(
                    body,
                    tail,
                    end,
                    frame
                )
                || ReadDisjointFrameOwnership(body, start, end) is not { } layout
                || layout.Uses.Count != 1 || layout.Owners[save - start] != allocation
                || layout.Owners[tail - start] != allocation)
            {
                continue;
            }

            // The reserved heap contains a straight-line constructor region.
            // A value defined inside it cannot survive a register-clobbering operation.
            if (IsUnwritten(input, save + 1, lastRead))
            {
                return (slot, save, lastRead, (int)input.Value);
            }

            flow ??= new BeamProjectionFlow(body);
            int limit = Math.Min(maximumX + 1, BeamOperandParameters.MaximumRegister - 1);
            for (int register = 0; register <= limit; register++)
            {
                var scratch = new BeamOperand(BeamOperandParameters.X, register);
                if (IsUnwritten(scratch, save + 1, lastRead) && flow.IsBranchTemporaryDead(save, scratch))
                {
                    return (slot, save, lastRead, register);
                }
            }
        }

        return null;

        bool IsUnwritten(BeamOperand value, int first, int last)
        {
            for (int index = first; index < last; index++)
            {
                BeamInstruction instruction = body[index];
                int destination = StackAllocationDestination(instruction);
                if (destination >= 0 && SameProjectionRegister(instruction.Operands[destination], value))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private static bool IsTerminalConstructionStep(BeamInstruction instruction) =>
        instruction.Definition.Opcode is Move or GetTupleElement or PutList or PutTuple or Line;

    private static bool IsTerminalFrameExit(
        IReadOnlyList<BeamInstruction> body,
        int tail,
        int end,
        int frame
    ) =>
        body[tail] is { Definition.Opcode: TailLocalCallWithFrame or TailExternalCallWithFrame, Operands: [_, _, var size] }
            && size.Value == frame
        || tail + 1 < end && body[tail] is { Definition.Opcode: Deallocate, Operands: [var width] }
            && width.Value == frame && body[tail + 1].Definition.Opcode == Return;
}
