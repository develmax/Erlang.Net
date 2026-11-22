using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimUnusedOwnedFrameSuffixes(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            bool checkedOwnership = false;
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int allocation = start; allocation + 2 < end; allocation++)
            {
                BeamInstruction frame = body[allocation];
                if (frame.Definition.Opcode is not (Allocate or AllocateHeap)
                    || frame.Operands[0].Value < 2
                    || ReadOwnedFrameSuffixEnd(body, allocation, end) is not int stop
                    || PlanLinearStackSlots(
                        body,
                        allocation,
                        stop,
                        includeUnchanged: true
                    ) is not { } local)
                {
                    continue;
                }

                int retained = Array.FindLastIndex(local.Uses, use => use.First >= 0) + 1;
                if (retained == 0 || retained >= frame.Operands[0].Value)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.ReachesLinearFrameUnframed(start, allocation))
                {
                    if (!checkedOwnership)
                    {
                        ownership = ReadDisjointFrameOwnership(body, start, end);
                        checkedOwnership = true;
                    }

                    if (ownership is not { } owned || owned.Owners[allocation - start] != UnframedOwner
                        || !owned.Uses.ContainsKey(allocation))
                    {
                        continue;
                    }
                }

                // The closed frame never accesses its trailing slots except to
                // initialize them. Keep every lower slot in its old position.
                for (int index = allocation; index < stop; index++)
                {
                    BeamInstruction instruction = body[index];
                    int width = LinearFrameSizeOperand(instruction.Definition.Opcode);
                    if (width >= 0)
                    {
                        copied = WritePostCallProjectionOperand(
                            output,
                            code,
                            instruction.Offset,
                            copied,
                            width,
                            retained
                        );
                    }
                    else if (instruction.Definition.Opcode == InitYRegisters)
                    {
                        int[] slots = instruction.Operands.Skip(1).Select(slot => (int)slot.Value)
                            .Where(slot => slot < retained).ToArray();
                        if (slots.Length == instruction.Operands.Count - 1)
                        {
                            continue;
                        }

                        output.Write(code.Span[copied..instruction.Offset]);
                        if (slots.Length > 0)
                        {
                            WriteFrameInitialization(output, slots);
                        }

                        copied = body[index + 1].Offset;
                    }
                }

                allocation = stop - 1;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int? ReadOwnedFrameSuffixEnd(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int end
    )
    {
        bool initializes = false;
        for (int index = allocation + 1; index < end; index++)
        {
            byte opcode = body[index].Definition.Opcode;
            if (opcode == Deallocate)
            {
                return initializes && index + 1 < end && body[index + 1].Definition.Opcode == Return
                    ? index + 2 : null;
            }

            if (opcode is TailLocalCallWithFrame or TailExternalCallWithFrame or TailDynamicApply)
            {
                return initializes ? index + 1 : null;
            }

            if (opcode is Label or FunctionInfo or Allocate or AllocateHeap or TrimFrame or DebugLine
                or TryEnter or CatchEnter or TryEnd or CatchEnd || EndsBeamBlock(opcode))
            {
                return null;
            }

            initializes |= opcode == InitYRegisters;
        }

        return null;
    }
}
