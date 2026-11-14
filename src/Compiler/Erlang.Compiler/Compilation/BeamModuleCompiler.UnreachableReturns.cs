using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static void CollectUnreachableBlocks(
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Label)> labelOperands,
        Dictionary<int, int> redirects,
        List<CodeEdit> edits
    )
    {
        var referenced = labelOperands.Select(operand => operand.Label).ToHashSet();
        for (int index = 1; index + 1 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Label, Operands: [var entry] }
                || !EndsBeamBlock(body[index - 1].Definition.Opcode)
                || referenced.Contains((int)entry.Value))
            {
                continue;
            }

            int end = index + 1;
            // Keep source locations and the bootstrap label preceding func_info.
            while (body[end].Definition.Opcode is not (Label or FunctionInfo or EndCode or Line)
                && body[end].Definition.Name is not ("debug_line" or "executable_line"))
            {
                end++;
            }

            if (body[end].Definition.Opcode is not (Label or EndCode))
            {
                continue;
            }

            redirects.Add((int)entry.Value, 0);
            edits.Add(new(body[index].Offset, body[end].Offset, LabelTag, 0));
            index = end - 1;
        }
    }

    private static void CollectUnusedFallthroughLabels(
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Label)> labelOperands,
        Dictionary<int, int> redirects,
        List<CodeEdit> edits
    )
    {
        var referenced = labelOperands.Select(operand => operand.Label).ToHashSet();
        int following = body.Count - 1;
        for (int index = body.Count - 2; index > 0; index--)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            if (instruction is { Definition.Opcode: Label, Operands: [var entry] }
                && body[following].Definition.Opcode != FunctionInfo
                && body[index - 1].Definition.Opcode != FunctionInfo
                && !EndsBeamBlock(body[index - 1].Definition.Opcode)
                && !referenced.Contains((int)entry.Value))
            {
                redirects.Add((int)entry.Value, 0);
                edits.Add(new(instruction.Offset, body[index + 1].Offset, LabelTag, 0));
            }

            following = index;
        }
    }
}
