using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static void CollectUnreachableReturns(
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Label)> labelOperands,
        Dictionary<int, int> redirects,
        List<CodeEdit> edits
    )
    {
        var referenced = labelOperands.Select(operand => operand.Label).ToHashSet();
        for (int index = 1; index + 2 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Label, Operands: [var entry] }
                || !EndsBeamBlock(body[index - 1].Definition.Opcode)
                || referenced.Contains((int)entry.Value))
            {
                continue;
            }

            int terminal = index + (body[index + 1].Definition.Opcode == Deallocate ? 2 : 1);
            if (terminal + 1 >= body.Count || body[terminal].Definition.Opcode != Return)
            {
                continue;
            }

            // Entry labels follow func_info, and every other incoming edge is a label operand.
            redirects.Add((int)entry.Value, 0);
            edits.Add(new(body[index].Offset, body[terminal + 1].Offset, LabelTag, 0));
        }
    }
}
