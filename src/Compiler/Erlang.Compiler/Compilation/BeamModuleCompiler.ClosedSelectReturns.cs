using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static void CollectClosedSelectReturnRelays(
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> references,
        Dictionary<int, (int Target, int End)> relays
    )
    {
        Dictionary<int, int>? uses = null;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (!body.Skip(entry).Take(end - entry).Any(instruction => instruction.Definition.Opcode == CatchEnd))
            {
                continue;
            }

            HashSet<int>? unprotected = null;
            for (int select = entry + 1; select + 7 <= end; select++)
            {
                if (body[select] is not
                    {
                        Definition.Opcode: SelectValue,
                        Operands: [_, var fallback, _, var key, var target, ..]
                    } instruction || key.Kind != BeamOperandParameters.Atom || target.Kind != BeamOperandParameters.Label)
                {
                    continue;
                }

                int first = select + 1;
                int second = first + 3;
                if (body[first] is not { Definition.Opcode: Label, Operands: [var left] }
                    || body[first + 1] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                    || body[first + 2].Definition.Opcode != Return
                    || body[second] is not { Definition.Opcode: Label, Operands: [var right] }
                    || body[second + 1] is not { Definition.Opcode: Deallocate, Operands: [var otherWidth] }
                    || body[second + 2].Definition.Opcode != Return
                    || body[second + 3].Definition.Opcode is not (Label or FunctionInfo or EndCode)
                    || width != otherWidth || left.Value == fallback.Value || right.Value == fallback.Value)
                {
                    continue;
                }

                bool selectsLeft = false;
                bool selectsRight = false;
                for (int operand = 4; operand < instruction.Operands.Count; operand += 2)
                {
                    var branch = instruction.Operands[operand];
                    selectsLeft |= branch.Value == left.Value;
                    selectsRight |= branch.Value == right.Value;
                }

                if (!selectsLeft || !selectsRight)
                {
                    continue;
                }

                int leftLabel = (int)left.Value;
                int rightLabel = (int)right.Value;
                uses ??= references.GroupBy(reference => reference.Label).ToDictionary(group => group.Key, group => group.Count());
                if (uses.GetValueOrDefault(leftLabel) != 1 || uses.GetValueOrDefault(rightLabel) != 1)
                {
                    continue;
                }

                unprotected ??= UnprotectedReturnLabels(body, entry, end);
                if (unprotected is null || !unprotected.Contains(leftLabel) || !unprotected.Contains(rightLabel))
                {
                    continue;
                }

                // Both keys enter from this select with the same cleared handler and frame.
                // Share the instruction-only epilogue after the final frame and layout passes.
                relays.Add(first, (rightLabel, second));
            }
        }
    }
}
