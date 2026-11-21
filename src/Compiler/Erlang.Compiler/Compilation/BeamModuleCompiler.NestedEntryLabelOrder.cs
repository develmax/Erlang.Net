using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly HashSet<(string Name, int Arity)> nestedEntryLabelOrder = [];

    private (byte[] Code, int[] Labels, int RemovedLabels)? OrderNestedEntryLabels(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> references,
        IReadOnlyList<string> atomNames,
        int labelCount
    )
    {
        if (nestedEntryLabelOrder.Count == 0)
        {
            return null;
        }

        var edits = new List<CodeEdit>();
        int[] labels = Enumerable.Range(0, labelCount).ToArray();
        int[] functions = Enumerable.Range(0, body.Count)
            .Where(index => body[index].Definition.Opcode == FunctionInfo).ToArray();
        for (int index = 0; index < functions.Length; index++)
        {
            int info = functions[index];
            var operands = body[info].Operands;
            if (!nestedEntryLabelOrder.Contains((atomNames[(int)operands[1].Value - 1], (int)operands[2].Value)))
            {
                continue;
            }

            int begin = info + 2;
            int end = index + 1 < functions.Length ? functions[index + 1] - 1 : body.Count - 1;
            while (index + 1 < functions.Length && end > info && body[end].Definition.Opcode != Label)
            {
                end--;
            }

            var definitions = new List<(int Index, int Label)>();
            for (int offset = begin; offset < end; offset++)
            {
                if (body[offset] is { Definition.Opcode: Label, Operands: [var label] })
                {
                    definitions.Add((offset, (int)label.Value));
                }
            }

            int[] ordered = definitions.Select(definition => definition.Label).Order().ToArray();
            for (int offset = 0; offset < ordered.Length; offset++)
            {
                var (instruction, original) = definitions[offset];
                int mapped = ordered[offset];
                if (original == mapped)
                {
                    continue;
                }

                labels[original] = mapped;
                edits.Add(new CodeEdit(
                    body[instruction].Offset + 1,
                    body[instruction + 1].Offset,
                    BeamWire.UnsignedTag,
                    mapped
                ));
            }
        }

        if (edits.Count == 0)
        {
            return null;
        }

        return (RewriteLabelOperands(
            code,
            references,
            labels,
            edits
        ), labels, 0);
    }

    private static byte[] RewriteLabelOperands(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<(int Start, int End, int Label)> references,
        IReadOnlyList<int> labels,
        List<CodeEdit> edits
    )
    {
        var removals = edits.Where(edit => edit.IsRemoval).OrderBy(edit => edit.Start).ToArray();
        int currentRemoval = 0;
        foreach (var (start, end, label) in references)
        {
            while (currentRemoval < removals.Length && removals[currentRemoval].End <= start)
            {
                currentRemoval++;
            }

            if (currentRemoval < removals.Length && removals[currentRemoval].Start <= start)
            {
                continue;
            }

            if (labels[label] != label)
            {
                edits.Add(new CodeEdit(
                    start,
                    end,
                    LabelTag,
                    labels[label]
                ));
            }
        }

        using var rewritten = new MemoryStream(code.Length);
        int copied = 0;
        foreach (CodeEdit edit in edits.OrderBy(edit => edit.Start))
        {
            rewritten.Write(code.Span[copied..edit.Start]);
            if (!edit.IsRemoval)
            {
                BeamCompactWriter.Write(rewritten, edit.Tag, edit.Value);
            }
            copied = edit.End;
        }

        rewritten.Write(code.Span[copied..]);

        return rewritten.ToArray();
    }
}
