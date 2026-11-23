using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (
        (int Count, int[] Slots, (int Index, int OriginalRemoved, int Kept)? Trim) Layout,
        int Definition,
        int Read,
        int Slot,
        int Replacement
    )? PlanTerminalProjectionSlotReuse(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end
    )
    {
        if (ReadDisjointFrameOwnership(body, entry, end) is not { } ownership || ownership.Uses.Count != 1)
        {
            return null;
        }

        int frame = ownership.Uses.Keys.Single();
        int width = ownership.Uses[frame].Length;
        int last = end - 1;
        while (last > frame && body[last].Definition.Opcode is Label or Line)
        {
            last--;
        }

        if (last <= frame + 2 || body[last].Definition.Opcode != Return
            || body[last - 1].Definition.Opcode != Deallocate
            || body.Skip(frame + 1).Take(end - frame - 1).Any(instruction =>
                instruction.Definition.Opcode is CatchEnter or TryEnter or TrimFrame or DebugLine or ExecutableLine))
        {
            return null;
        }

        for (int definition = last - 2; definition > frame + 1; definition--)
        {
            BeamInstruction instruction = body[definition];
            byte opcode = instruction.Definition.Opcode;
            if (ownership.Owners[definition - entry] != frame
                || opcode is not (Line or Move or TestHeap or PutTuple or LocalCall or ExternalCall
                    or GcBifOne or GcBifTwo or GcBifThree)
                || opcode != LocalCall && instruction.Operands.Any(operand =>
                    operand.Kind == BeamOperandParameters.Label && !operand.Value.IsZero))
            {
                return null;
            }

            if (body[definition - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || instruction is not
                {
                    Definition.Opcode: Move,
                    Operands: [var value, { Kind: BeamOperandParameters.Y, TypeIndex: null } retired]
                }
                || !IsBareX0(value)
                || !body.Skip(frame + 1).Take(definition - frame - 1).Any(previous =>
                    IsAtomicStackAliasCandidate(previous) && SameProjectionRegister(previous.Operands[0], retired)))
            {
                continue;
            }

            int read = -1;
            var used = new HashSet<int> { (int)retired.Value };
            for (int index = definition + 1; index < last - 1; index++)
            {
                foreach (BeamOperand slot in body[index].Operands.Where(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    used.Add((int)slot.Value);
                    if (SameProjectionRegister(slot, retired))
                    {
                        if (read >= 0 || body[index] is not
                            {
                                Definition.Opcode: Move,
                                Operands: [{ TypeIndex: null } from, { Kind: BeamOperandParameters.X }]
                            } || !SameProjectionRegister(from, retired))
                        {
                            return null;
                        }

                        read = index;
                    }
                }
            }

            if (read < 0)
            {
                return null;
            }

            // The suffix has one entry and no handler. The new definition and
            // its sole read can share an untouched slot without moving either.
            var rewritten = body.ToArray();
            for (int slot = 0; slot < width; slot++)
            {
                if (used.Contains(slot))
                {
                    continue;
                }

                BeamOperand replacement = retired with { Value = slot };
                rewritten[definition] = body[definition] with { Operands = [value, replacement] };
                rewritten[read] = body[read] with { Operands = [replacement, body[read].Operands[1]] };
                if (PlanAtomicProjectionStackSlots(rewritten, entry, end) is { Trim: null } compacted)
                {
                    return (compacted, definition, read, (int)retired.Value, slot);
                }
            }

            return null;
        }

        return null;
    }
}
