using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static ((int Size, int[] Slots)?[] Layouts, IReadOnlySet<int> RedundantClears)?
        PlanOccupiedEntryFrameAfterRedundantClears(
            IReadOnlyList<BeamInstruction> body,
            int start,
            int end
        )
    {
        if (ReadDisjointFrameOwnership(body, start, end) is not { } frame
            || frame.Uses.Count != 1 || !frame.Uses.First().Value.All(used => used)
            || Enumerable.Range(start, end - start).Any(index => body[index].Definition.Opcode is
                DebugLine or ExecutableLine or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout
                or RemoveMessage or ReceiveTimeout))
        {
            return null;
        }

        int allocation = frame.Uses.First().Key;
        if (frame.Owners[allocation - start] == UnseenFrameOwner)
        {
            return null;
        }

        var knownNil = new HashSet<int>();
        var redundant = new HashSet<int>();
        for (int index = allocation + 1; index < end && frame.Owners[index - start] == allocation; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            // The prefix has one incoming path. Leave any region beyond its
            // first entry label, control transfer or explicit failure edge alone.
            if (opcode is Label or BinaryMatch or Deallocate || EndsBeamBlock(opcode)
                || opcode != LocalCall && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label))
            {
                break;
            }

            if (opcode == InitYRegisters)
            {
                int[] slots = instruction.Operands.Skip(1).Select(operand => (int)operand.Value).ToArray();
                if (slots.Length > 0 && slots.All(knownNil.Contains))
                {
                    redundant.Add(index);
                }

                knownNil.UnionWith(slots);
            }
            else
            {
                // Keeping only slots with no intervening operand is enough to
                // prove the nil root survives ordinary calls and heap checks.
                foreach (BeamOperand operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.Y)
                    {
                        knownNil.Remove((int)operand.Value);
                    }
                }
            }
        }

        if (redundant.Count == 0)
        {
            return null;
        }

        int info = start - 2;
        while (info >= 0 && body[info].Definition.Opcode == Line)
        {
            info--;
        }

        int prefix = Math.Max(0, info);
        var filtered = new List<BeamInstruction>(end - prefix - redundant.Count);
        for (int index = prefix; index < end; index++)
        {
            if (!redundant.Contains(index))
            {
                filtered.Add(body[index]);
            }
        }

        if (PlanBranchStackSlots(
            filtered,
            start - prefix,
            filtered.Count,
            linearAllocation: allocation - prefix,
            allowGcBifExceptionExit: true
        ) is not { Trim: null } compact)
        {
            return null;
        }

        var layouts = new (int Size, int[] Slots)?[end - start];
        for (int index = 0; index < layouts.Length; index++)
        {
            if (frame.Owners[index] == allocation || index + start == allocation)
            {
                layouts[index] = (compact.Count, compact.Slots);
            }
        }

        return (layouts, redundant);
    }
}
