using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveCoveredCallFrameInitializations(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var nilSlots = new HashSet<int>();
        var edits = new List<CodeEdit>();
        int frameSize = -1;
        bool crossedCall = false;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Allocate or AllocateHeap)
            {
                frameSize = (int)instruction.Operands[0].Value;
                nilSlots.Clear();
                crossedCall = false;
                continue;
            }

            if (frameSize < 0 || opcode == Line)
            {
                continue;
            }

            if (opcode is LocalCall or ExternalCall)
            {
                crossedCall = true;
                continue;
            }

            if (opcode == InitYRegisters && instruction.Operands.Count > 1
                && instruction.Operands.Skip(1).All(slot =>
                    slot is { Kind: BeamOperandParameters.Y, TypeIndex: null } && (uint)slot.Value < (uint)frameSize))
            {
                var slots = instruction.Operands.Skip(1).Select(slot => (int)slot.Value).ToArray();
                int next = index + 1;
                while (next < body.Count && body[next].Definition.Opcode == Line)
                {
                    next++;
                }

                if (crossedCall && next < body.Count && body[next].Definition.Opcode is (LocalCall or ExternalCall)
                    && slots.All(nilSlots.Contains))
                {
                    edits.Add(new CodeEdit(
                        instruction.Offset,
                        body[index + 1].Offset,
                        0,
                        0,
                        IsRemoval: true
                    ));
                }

                nilSlots.UnionWith(slots);
                continue;
            }

            if (opcode is Move or GetTupleElement or GetList or GetHead or GetTail)
            {
                ForgetWrittenSlot(instruction.Operands[StackWriteOperand(instruction)]);
                if (opcode == GetList)
                {
                    ForgetWrittenSlot(instruction.Operands[2]);
                }

                continue;
            }

            // A label, trim, handler or unmodeled instruction ends the single-entry proof.
            frameSize = -1;
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        void ForgetWrittenSlot(BeamOperand destination)
        {
            if (destination.Kind == BeamOperandParameters.Y)
            {
                nilSlots.Remove((int)destination.Value);
            }
        }
    }
}
