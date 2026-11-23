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

    private byte[]? RemoveGuardProvenLiteralMoves(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(Move)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction test = body[index];
            if (test.Definition.Opcode is not (IsNil or ExactEqualTest) || test.Operands[0].Value.Sign <= 0)
            {
                continue;
            }

            int move = index + 1;
            while (move + 1 < body.Count && body[move].Definition.Opcode == Line)
            {
                move++;
            }

            if (move + 1 >= body.Count || body[move] is not
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Nil or BeamOperandParameters.Atom or BeamOperandParameters.Integer } source,
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y } destination
                    ]
                }
                || !ProvesLiteralMove(test, source, destination))
            {
                continue;
            }

            // No branch can enter between the successful test and its move.
            output.Write(code.Span[copied..body[move].Offset]);
            copied = body[move + 1].Offset;
            index = move;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ProvesLiteralMove(BeamInstruction test, BeamOperand source, BeamOperand destination) =>
        test.Operands switch
        {
            [_, var tested] => source.Kind == BeamOperandParameters.Nil && SameProjectionRegister(tested, destination),
            [_, var left, var right] => (source == right && SameProjectionRegister(left, destination))
                || (source == left && SameProjectionRegister(right, destination)),
            _ => false
        };
}
