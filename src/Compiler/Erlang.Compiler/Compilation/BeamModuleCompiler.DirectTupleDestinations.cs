using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static void CollectDirectConstructionDestinationEdits(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<CodeEdit> edits
    )
    {
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction construction = body[index];
            BeamInstruction copy = body[index + 1];
            byte opcode = construction.Definition.Opcode;
            if (opcode is not (PutTuple or PutList)
                || construction.Operands[opcode == PutTuple ? 0 : 2] is not
                { Kind: BeamOperandParameters.X, TypeIndex: null } temporary
                || copy is not
                {
                    Definition.Opcode: Move,
                    Operands: [var source, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                }
                || source != temporary || destination == temporary
                || !DiscardsCopiedConstructionTemporary(body, index + 2, temporary))
            {
                continue;
            }

            // Both constructors read their inputs before writing the destination.
            int start = opcode == PutTuple
                ? construction.Offset + 1
                : copy.Offset - CompactRegisterCountWidth(checked((int)temporary.Value));
            var cursor = new BeamCursor(code[start..copy.Offset]);
            BeamCompactValue operand = BeamCompactReader.Read(cursor);
            if (operand.WireTag != XRegisterTag || operand.Value != temporary.Value)
            {
                continue;
            }

            edits.Add(new CodeEdit(
                start,
                start + cursor.Position,
                XRegisterTag,
                checked((int)destination.Value)
            ));
            edits.Add(new CodeEdit(
                copy.Offset,
                body[index + 2].Offset,
                0,
                0,
                IsRemoval: true
            ));
        }
    }

    private static bool DiscardsCopiedConstructionTemporary(
        IReadOnlyList<BeamInstruction> body,
        int start,
        BeamOperand temporary
    )
    {
        for (int index = start; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case Line:
                    continue;
                case Move:
                    if (SameProjectionRegister(instruction.Operands[0], temporary))
                    {
                        return false;
                    }

                    if (SameProjectionRegister(instruction.Operands[1], temporary))
                    {
                        return true;
                    }

                    continue;
                case Deallocate:
                    return !temporary.Value.IsZero && index + 1 < body.Count && body[index + 1].Definition.Opcode == Return;
                case Return:
                    return !temporary.Value.IsZero;
                case TailLocalCall:
                case TailLocalCallWithFrame:
                case TailExternalCall:
                case TailExternalCallWithFrame:
                    return instruction.Operands[0].Value <= temporary.Value;
                default:
                    return false;
            }
        }

        return false;
    }
}
