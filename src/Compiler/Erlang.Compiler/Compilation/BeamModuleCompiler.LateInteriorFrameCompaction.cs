using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CompactLateUnusedInteriorSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        var entries = new HashSet<int>();
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            if (start < end && body[start] is { Definition.Opcode: Label, Operands: [var entry] }
                && HasUnusedInteriorSlot(body, start, end))
            {
                entries.Add((int)entry.Value);
            }

            start = end + 1;
        }

        return entries.Count == 0 ? null : RewriteDisjointFrames(
            code,
            body,
            stackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );
    }

    private static bool HasUnusedInteriorSlot(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        int frame = -1;
        int largest = -1;
        var used = new HashSet<int>();
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is TrimFrame or CatchEnter or TryEnter or DebugLine or ExecutableLine)
            {
                return false;
            }

            if (opcode is Allocate or AllocateHeap)
            {
                if (frame >= 0)
                {
                    return false;
                }

                frame = (int)instruction.Operands[0].Value;
            }
            else if (opcode != InitYRegisters && instruction is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.Y }]
            })
            {
                foreach (var operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.Y)
                    {
                        int register = (int)operand.Value;
                        used.Add(register);
                        largest = Math.Max(register, largest);
                    }
                }
            }
        }

        // A late per-value rewrite can remove the last read of a slot while
        // leaving the other frame slots and instruction order in place.
        return largest < frame && used.Count < largest + 1;
    }
}
