using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? MoveColdBooleanCaseFailures(ReadOnlyMemory<byte> code, int labelCount, bool standaloneOnly = false)
    {
        if (!code.Span.Contains(SelectValue) || !code.Span.Contains(CaseEnd))
        {
            return null;
        }

        var references = new List<(int Start, int End, int Label)>();
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                recordLabelOperand: (from, to, label) => references.Add((from, to, label))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }
        if (body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine))
        {
            return null;
        }

        string[] atomNames = atoms.OrderBy(atom => atom.Value).Select(atom => atom.Key).ToArray();
        BeamProjectionFlow? flow = null;
        var layouts = new List<(int Start, int End, int[] Order)>();
        var labels = new Dictionary<int, int>();
        int entry = 0;
        for (int boundary = 0; boundary < body.Count; boundary++)
        {
            byte opcode = body[boundary].Definition.Opcode;
            if (opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = boundary + 1;
            int end = boundary;
            if (opcode == FunctionInfo)
            {
                while (end > start && body[end - 1].Definition.Opcode == Line)
                {
                    end--;
                }
                if (end == start || body[end - 1].Definition.Opcode != Label)
                {
                    continue;
                }

                end--;
            }

            if (end <= start + 1 || body[start].Definition.Opcode != Label
                || !EndsBeamBlock(body[end - 1].Definition.Opcode)
                || body.Skip(start).Take(end - start).Any(instruction => instruction.Definition.Opcode is TryEnter or CatchEnter))
            {
                continue;
            }

            int[]? dispatch = standaloneOnly ? PlanDispatchCaseFailures(
                body,
                start,
                end,
                standaloneOnly: true
            )
                : PlanNestedRecordColdCases(body, start, end) ?? PlanDispatchCaseFailures(body, start, end);
            if (dispatch is not int[] order)
            {
                if (standaloneOnly)
                {
                    continue;
                }

                int exceptionSuffix = FindTrailingExceptionBlocks(body, start, end);
                var failures = new SortedSet<int>();
                for (int index = start + 1; index < exceptionSuffix; index++)
                {
                    if (body[index] is not
                        {
                            Definition.Opcode: SelectValue,
                            Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true }, var failure, _,
                            { Kind: BeamOperandParameters.Atom } first, _,
                            { Kind: BeamOperandParameters.Atom } second, _]
                        }
                        || atomNames[(int)first.Value - 1] != ErlangBooleanAtoms.False || atomNames[(int)second.Value - 1] != ErlangBooleanAtoms.True
                        || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(index, (int)failure.Value, out int cold)
                        || cold + 2 >= exceptionSuffix || body[cold + 1].Definition.Opcode != Line
                        || body[cold + 2] is not { Definition.Opcode: CaseEnd, Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true }] }
                        || cold + 3 < end && body[cold + 3].Definition.Opcode != Label)
                    {
                        continue;
                    }

                    failures.Add(cold);
                }
                if (failures.Count == 0)
                {
                    continue;
                }

                // beam_jump relocates a short exception block that cannot be reached by fallthrough.
                var coldInstructions = failures.SelectMany(failure => Enumerable.Range(failure, 3)).ToHashSet();
                order = Enumerable.Range(start + 1, exceptionSuffix - start - 1)
                    .Where(index => !coldInstructions.Contains(index))
                    .Concat(coldInstructions.Order())
                    .Concat(Enumerable.Range(exceptionSuffix, end - exceptionSuffix))
                    .ToArray();
            }

            if (order[0] == start + 1 && order.Zip(order.Skip(1)).All(pair => pair.First + 1 == pair.Second))
            {
                continue;
            }

            layouts.Add((start + 1, end, order));
            // beam_clean numbers the labels in physical block order. Keep func_info and entry labels.
            int[] oldLabels = order.Where(index => body[index].Definition.Opcode == Label)
                .Select(index => (int)body[index].Operands[0].Value).ToArray();
            foreach (var (old, mapped) in oldLabels.Zip(oldLabels.Order()))
            {
                if (old != mapped)
                {
                    labels.Add(old, mapped);
                }
            }
        }
        if (layouts.Count == 0)
        {
            return null;
        }

        var edits = new List<CodeEdit>();
        foreach (var (from, to, label) in references)
        {
            if (labels.TryGetValue(label, out int mapped))
            {
                edits.Add(new(
                    from,
                    to,
                    LabelTag,
                    mapped
                ));
            }
        }
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: Label, Operands: [var operand] }
                && labels.TryGetValue((int)operand.Value, out int mapped))
            {
                edits.Add(new(
                    body[index].Offset + 1,
                    body[index + 1].Offset,
                    BeamWire.UnsignedTag,
                    mapped
                ));
            }
        }

        CodeEdit[] orderedEdits = edits.OrderBy(edit => edit.Start).ToArray();
        using var remapped = new MemoryStream(code.Length);
        int[] offsets = new int[body.Count + 1];
        int change = 0;
        for (int index = 0; index < body.Count; index++)
        {
            offsets[index] = (int)remapped.Length;
            int from = body[index].Offset;
            int to = index + 1 < body.Count ? body[index + 1].Offset : code.Length;
            while (change < orderedEdits.Length && orderedEdits[change].Start < to)
            {
                CodeEdit edit = orderedEdits[change++];
                remapped.Write(code.Span[from..edit.Start]);
                BeamCompactWriter.Write(remapped, edit.Tag, edit.Value);
                from = edit.End;
            }

            remapped.Write(code.Span[from..to]);
        }
        offsets[^1] = (int)remapped.Length;

        using var output = new MemoryStream((int)remapped.Length);
        byte[] bytes = remapped.GetBuffer();
        int emitted = 0;
        foreach (var (start, end, order) in layouts)
        {
            output.Write(bytes.AsSpan(emitted, offsets[start] - emitted));
            foreach (int index in order)
            {
                output.Write(bytes.AsSpan(offsets[index], offsets[index + 1] - offsets[index]));
            }

            emitted = offsets[end];
        }
        output.Write(bytes.AsSpan(emitted, offsets[^1] - emitted));

        return RebuildLineTable(output.ToArray(), labelCount);
    }

    private static int FindTrailingExceptionBlocks(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end
    )
    {
        int boundary = end;
        while (boundary > entry + 2 && body[boundary - 1].Definition.Opcode is
            CaseEnd or IfEnd or TryCaseEnd or BadMatch or BadRecord)
        {
            int label = boundary - 2;
            while (label > entry && body[label].Definition.Opcode == Line)
            {
                label--;
            }

            if (label <= entry || body[label].Definition.Opcode != Label)
            {
                break;
            }

            // Existing short exception blocks already have the pinned suffix order.
            boundary = label;
        }

        return boundary;
    }
}
