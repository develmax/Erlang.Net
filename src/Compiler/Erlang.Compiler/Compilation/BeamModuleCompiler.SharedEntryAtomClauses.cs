using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? OrderSharedEntryAtomClauses(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Label)> labelOperands,
        string[] atoms
    )
    {
        using var output = new MemoryStream();
        Dictionary<int, int>? referenceCounts = null;
        int copied = 0;
        for (int info = 0; info + 3 < body.Count; info++)
        {
            int entry = info + 1;
            int selection = entry + 1;
            if (body[info].Definition.Opcode != FunctionInfo
                || body[entry].Definition.Opcode != Label
                || body[selection] is not
                {
                    Definition.Opcode: SelectValue,
                    Operands: [{ Kind: BeamOperandParameters.X }, var failure, _, ..] operands
                }
                || failure.Value != EntryFrameFailureLabel(body, entry)
                || operands.Count < 9)
            {
                continue;
            }

            var arms = new List<(Atom Key, int Target)>();
            for (int operand = 3; operand + 1 < operands.Count; operand += 2)
            {
                if (operands[operand].Kind == BeamOperandParameters.Atom
                    && operands[operand + 1].Kind == BeamOperandParameters.Label)
                {
                    arms.Add((Term.A(atoms[(int)operands[operand].Value - 1]), (int)operands[operand + 1].Value));
                }
            }

            if (arms.Count * 2 != operands.Count - 3)
            {
                continue;
            }

            // beam_ssa_share visits a shared block once. Reverse postorder
            // places terminal arms in reverse of their first ascending edge.
            int[] order = arms.OrderBy(arm => arm.Key, Comparer<Atom>.Create(
                    (left, right) => TermOrder.Compare(left, right, exact: true)
                ))
                .Select(arm => arm.Target).Distinct().Reverse().ToArray();
            if (order.Length < 2 || order.Length == arms.Count)
            {
                continue;
            }

            int first = selection + 1;
            int end = first;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            if (body[end].Definition.Opcode == FunctionInfo)
            {
                end--;
                while (end > first && body[end].Definition.Opcode == Line)
                {
                    end--;
                }
            }

            var blocks = new List<(int Label, int First, int End)>();
            for (int index = first; index < end;)
            {
                if (body[index] is not { Definition.Opcode: Label, Operands: [var label] })
                {
                    break;
                }

                int start = index++;
                while (index < end && body[index].Definition.Opcode is
                    Line or Allocate or AllocateHeap or InitYRegisters or Move or Swap or TestHeap
                    or PutTuple or PutList or LocalCall or ExternalCall or Deallocate)
                {
                    index++;
                }

                if (index >= end || body[index].Definition.Opcode is not
                    (Return or TailLocalCall or TailLocalCallWithFrame or TailExternalCall or TailExternalCallWithFrame))
                {
                    break;
                }

                blocks.Add(((int)label.Value, start, ++index));
            }

            if (blocks.Count != order.Length || blocks[^1].End != end)
            {
                continue;
            }

            int[] old = blocks.Select(block => block.Label).ToArray();
            if (!old.ToHashSet().SetEquals(order) || old.SequenceEqual(order))
            {
                continue;
            }

            referenceCounts ??= labelOperands.GroupBy(operand => operand.Label)
                .ToDictionary(group => group.Key, group => group.Count());
            if (arms.GroupBy(arm => arm.Target).Any(group => referenceCounts.GetValueOrDefault(group.Key) != group.Count()))
            {
                continue;
            }

            var regions = blocks.ToDictionary(block => block.Label);
            var labels = order.Zip(old).ToDictionary(pair => pair.First, pair => pair.Second);
            int firstOffset = body[first].Offset;
            foreach (var operand in labelOperands.Where(operand =>
                operand.Start >= body[selection].Offset && operand.End <= firstOffset))
            {
                if (labels.TryGetValue(operand.Label, out int label) && label != operand.Label)
                {
                    output.Write(code.Span[copied..operand.Start]);
                    BeamCompactWriter.Write(output, LabelTag, label);
                    copied = operand.End;
                }
            }

            output.Write(code.Span[copied..firstOffset]);
            foreach (int original in order)
            {
                var block = regions[original];
                output.WriteByte(Label);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, labels[original]);
                output.Write(code.Span[body[block.First + 1].Offset..body[block.End].Offset]);
            }

            copied = body[end].Offset;
            info = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
