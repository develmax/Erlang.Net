using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CombineAtomMembershipTests(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> labelOperands,
        IReadOnlyList<string>? atomNames = null,
        bool multiKey = false
    )
    {
        // Consecutive exact atom tests share a selector and a common success edge.
        var selections = new List<(int Start, int End, BeamOperand Selector, int Failure, int Success, List<BeamOperand> Keys)>();
        var renumbered = new Dictionary<int, int>();
        Dictionary<int, int>? labelPositions = null;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (!ReadTest(
                body[index],
                ExactNotEqualTest,
                out var success,
                out var selector,
                out var first
            ))
            {
                continue;
            }

            if (index > 0 && ReadTest(
                body[index - 1],
                ExactNotEqualTest,
                out int earlierSuccess,
                out var earlierSelector,
                out _
            ) && earlierSuccess == success && earlierSelector == selector)
            {
                continue;
            }

            var keys = new List<BeamOperand> { first };
            int end = index + 1;
            while (end + 1 < body.Count && ReadTest(
                body[end],
                ExactNotEqualTest,
                out int nextSuccess,
                out var nextSelector,
                out var atom
            ) && nextSuccess == success && nextSelector == selector)
            {
                keys.Add(atom);
                end++;
            }

            if (end + 1 >= body.Count || !ReadTest(
                body[end],
                ExactEqualTest,
                out var failure,
                out var finalSelector,
                out var final
            ) || finalSelector != selector || failure == success
                || body[end + 1] is not { Definition.Opcode: Label, Operands: [var next] }
                || next.Value != success)
            {
                continue;
            }

            keys.Add(final);
            if (multiKey != (keys.Count > 2) || keys.Select(key => key.Value).Distinct().Count() != keys.Count)
            {
                continue;
            }

            var names = atomNames ??= atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
            if (keys.All(key => names[(int)key.Value - 1] is ErlangBooleanAtoms.False or ErlangBooleanAtoms.True))
            {
                continue;
            }

            int directSuccess = end + 1;
            if (success == failure + 1
                && (labelPositions ??= ReadLabelPositions()).TryGetValue(failure, out int otherwise)
                && otherwise > directSuccess + 1 && EndsBeamBlock(body[otherwise - 1].Definition.Opcode)
                && !body.Skip(directSuccess + 1).Take(otherwise - directSuccess - 1)
                    .Any(instruction => instruction.Definition.Opcode is Label or FunctionInfo)
                && !renumbered.ContainsKey(failure) && !renumbered.ContainsKey(success))
            {
                // beam_clean assigns consecutive sibling labels in physical block order.
                renumbered.Add(success, failure);
                renumbered.Add(failure, success);
            }

            keys.Sort((left, right) => TermOrder.Compare(
                new Atom(names[(int)left.Value - 1]),
                new Atom(names[(int)right.Value - 1]),
                exact: true
            ));
            selections.Add((body[index].Offset, body[directSuccess].Offset, selector, failure, success, keys));
            index = end;
        }

        if (selections.Count == 0 || body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine))
        {
            return null;
        }

        var edits = new List<CodeEdit>();
        foreach (var (start, end, label) in labelOperands)
        {
            if (renumbered.TryGetValue(label, out int mapped))
            {
                edits.Add(new(
                    start,
                    end,
                    LabelTag,
                    mapped
                ));
            }
        }
        foreach (var (label, mapped) in renumbered)
        {
            int index = labelPositions![label];
            edits.Add(new(
                body[index].Offset + 1,
                body[index + 1].Offset,
                BeamWire.UnsignedTag,
                mapped
            ));
        }

        CodeEdit[] orderedEdits = edits.OrderBy(edit => edit.Start).ToArray();
        int nextEdit = 0;
        int copied = 0;
        using var output = new MemoryStream(code.Length);
        foreach (var (start, end, selector, failure, success, keys) in selections)
        {
            CopyThrough(start);
            while (nextEdit < orderedEdits.Length && orderedEdits[nextEdit].Start < end)
            {
                nextEdit++;
            }

            output.WriteByte(SelectValue);
            BeamCompactWriter.Write(output, XRegisterTag, selector.Value);
            BeamCompactWriter.Write(output, LabelTag, renumbered.GetValueOrDefault(failure, failure));
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, keys.Count * 2);
            foreach (BeamOperand key in keys)
            {
                BeamCompactWriter.Write(output, AtomTag, key.Value);
                BeamCompactWriter.Write(output, LabelTag, renumbered.GetValueOrDefault(success, success));
            }
            copied = end;
        }
        CopyThrough(code.Length);

        return output.ToArray();

        void CopyThrough(int end)
        {
            while (nextEdit < orderedEdits.Length && orderedEdits[nextEdit].Start < end)
            {
                var edit = orderedEdits[nextEdit++];
                output.Write(code.Span[copied..edit.Start]);
                BeamCompactWriter.Write(output, edit.Tag, edit.Value);
                copied = edit.End;
            }
            output.Write(code.Span[copied..end]);
            copied = end;
        }

        Dictionary<int, int> ReadLabelPositions() => body.Select((instruction, index) => (instruction, index))
            .Where(item => item.instruction.Definition.Opcode == Label)
            .ToDictionary(item => (int)item.instruction.Operands[0].Value, item => item.index);

        static bool ReadTest(
            BeamInstruction instruction,
            byte opcode,
            out int target,
            out BeamOperand selector,
            out BeamOperand atom
        )
        {
            target = 0;
            selector = default!;
            atom = default!;
            if (instruction.Definition.Opcode != opcode || instruction.Operands is not [var label, var left, var right])
            {
                return false;
            }

            if (left.Kind == BeamOperandParameters.Atom)
            {
                (left, right) = (right, left);
            }

            if (left is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                || right.Kind != BeamOperandParameters.Atom)
            {
                return false;
            }

            target = (int)label.Value;
            selector = left;
            atom = right;

            return true;
        }
    }
}
