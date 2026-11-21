using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ShareStackCopyTails(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> references,
        int sharedLabel
    )
    {
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            for (int test = start; test + 3 < end; test++)
            {
                byte inverse = body[test].Definition.Opcode switch
                {
                    ExactEqualTest => ExactNotEqualTest,
                    ExactNotEqualTest => ExactEqualTest,
                    _ => 0
                };
                if (inverse == 0
                    || body[test].Operands is not [{ Kind: BeamOperandParameters.Label } failure, _, _]
                    || body[test + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } source,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } destination
                        ]
                    }
                    || source == destination
                    || body[test + 2] is not
                    {
                        Definition.Opcode: Jump,
                        Operands: [{ Kind: BeamOperandParameters.Label } join]
                    }
                    || failure == join
                    || body[test + 3] is not { Definition.Opcode: Label, Operands: [var next] }
                    || next.Value != failure.Value
                    || FindLaterStackCopyTail(
                        body,
                        test + 4,
                        end,
                        body[test + 1],
                        join
                    ) is not { } tail
                    || !HasUnprotectedCheckedFailureRegion(body, test, tail.Join)
                    || ReadDisjointFrameOwnership(body, start, end) is not { } frame
                    || frame.Uses.Count != 1)
                {
                    continue;
                }

                int owner = frame.Owners[test + 1 - start];
                if (owner < start || frame.Owners[test - start] != owner
                    || frame.Owners[test + 3 - start] != owner
                    || frame.Owners[tail.Copy - start] != owner
                    || frame.Owners[tail.Join - start] != owner)
                {
                    continue;
                }

                BeamInstruction predicate = body[test];
                var reference = references.FirstOrDefault(reference => reference.Start == predicate.Offset + 1);
                if (reference.Start == 0 || reference.Label != (int)failure.Value)
                {
                    continue;
                }

                // Only the original copy and jump are removed. The new edge skips the
                // other arm's source markers and enters its identical copy directly.
                using var output = new MemoryStream(code.Length);
                output.Write(code.Span[..predicate.Offset]);
                output.WriteByte(inverse);
                BeamCompactWriter.Write(output, LabelTag, sharedLabel);
                output.Write(code.Span[reference.End..body[test + 1].Offset]);
                output.Write(code.Span[body[test + 3].Offset..body[tail.Copy].Offset]);
                output.WriteByte(Label);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, sharedLabel);
                output.Write(code.Span[body[tail.Copy].Offset..]);

                return output.ToArray();
            }
        }

        return null;
    }

    private static (int Copy, int Join)? FindLaterStackCopyTail(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        BeamInstruction copy,
        BeamOperand join
    )
    {
        int retained = -1;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is { Definition.Opcode: Label, Operands: [var label] } && label.Value == join.Value)
            {
                return retained < 0 ? null : (retained, index);
            }

            if (index + 1 < end && instruction.Definition.Opcode == Move
                && body[index - 1].Definition.Opcode is (IsNil or ExactEqualTest or ExactNotEqualTest
                    or IsLess or IsGreaterOrEqual)
                && instruction.Operands.SequenceEqual(copy.Operands)
                && body[index + 1] is { Definition.Opcode: Jump, Operands: [var target] } && target == join)
            {
                retained = index;
            }
        }

        return null;
    }
}
