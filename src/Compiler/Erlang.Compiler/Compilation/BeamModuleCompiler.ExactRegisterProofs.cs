using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static void CollectExactRegisterEdits(IReadOnlyList<BeamInstruction> body, List<CodeEdit> edits)
    {
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands:
                [
                    _,
                { Kind: BeamOperandParameters.X or BeamOperandParameters.Y } left,
                {
                    Kind: BeamOperandParameters.X or BeamOperandParameters.Y or BeamOperandParameters.Atom
                    or BeamOperandParameters.Integer or BeamOperandParameters.Nil
                } right
                ]
                })
            {
                continue;
            }

            int removed = index;
            if (left != right)
            {
                if (index + 2 >= body.Count || body[index + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var source, var destination]
                    } || right != source || left != destination)
                {
                    continue;
                }

                // A successful exact test has already established the copied value.
                removed++;
            }

            edits.Add(new CodeEdit(
                body[removed].Offset,
                body[removed + 1].Offset,
                0,
                0,
                IsRemoval: true
            ));
            index = removed;
        }
    }
}
