using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private int[]? emittedImports;

    private ReadOnlyMemory<byte>? OrderRelocatedEntryTables(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (closures.Count != 0)
        {
            return null;
        }

        int[] atomMap = new int[atoms.Count + 1];
        int[] importMap = Enumerable.Repeat(-1, imports.Count).ToArray();
        int[] lineMap = new int[lines.Count + 1];
        var atomReferences = new List<CodeEdit>();
        var indexReferences = new List<CodeEdit>();
        int nextAtom = 1;
        int nextImport = 0;
        int nextLine = 1;
        int lineCount = 0;
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
            body = BeamInstructionReader.ReadWithAtomOperands(
                header,
                atomMap.Length,
                literalBytes.Count,
                (start, end, value) => atomReferences.Add(new CodeEdit(
                    start,
                    end,
                    AtomTag,
                    value
                ))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var entries = imports.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();
        var locations = lines.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();
        atomMap[1] = nextAtom++;
        int nextUse = 0;
        for (int index = 0; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            int end = index + 1 < body.Count ? body[index + 1].Offset : code.Length;
            int importOperand = instruction.Definition.Opcode switch
            {
                ExternalCall or TailExternalCall or TailExternalCallWithFrame => 1,
                BifZero => 0,
                BifOne or BifTwo or BifThree => 1,
                GcBifOne or GcBifTwo or GcBifThree => 2,
                _ => -1
            };
            bool hasLine = instruction.Definition.Opcode == Line;
            if (importOperand >= 0 || hasLine)
            {
                var cursor = new BeamCursor(code[(instruction.Offset + 1)..end]);
                int position = hasLine ? 0 : importOperand;
                for (int skipped = 0; skipped < position; skipped++)
                {
                    _ = BeamCompactReader.Read(cursor);
                }

                int first = instruction.Offset + 1 + cursor.Position;
                BeamCompactValue operand = BeamCompactReader.Read(cursor);
                int last = instruction.Offset + 1 + cursor.Position;
                if (operand.WireTag != BeamWire.UnsignedTag || operand.Value < 0)
                {
                    return null;
                }

                CollectAtoms(first);
                if (hasLine)
                {
                    lineCount++;
                    if (operand.Value != 0 && operand.Value >= lineMap.Length)
                    {
                        return null;
                    }

                    int line = (int)operand.Value;
                    if (line != 0 && lineMap[line] == 0)
                    {
                        lineMap[line] = nextLine++;
                    }
                    if (line != lineMap[line])
                    {
                        indexReferences.Add(new CodeEdit(
                            first,
                            last,
                            BeamWire.UnsignedTag,
                            lineMap[line]
                        ));
                    }
                }
                else
                {
                    if (operand.Value >= importMap.Length)
                    {
                        return null;
                    }

                    int import = (int)operand.Value;
                    AssignImport(import);
                    if (import != importMap[import])
                    {
                        indexReferences.Add(new CodeEdit(
                            first,
                            last,
                            BeamWire.UnsignedTag,
                            importMap[import]
                        ));
                    }
                }
            }

            CollectAtoms(end);
        }
        for (int index = 0; index < importMap.Length; index++)
        {
            AssignImport(index);
        }
        foreach (var (atom, _, _) in exports.Concat(locals))
        {
            AssignAtom(atom);
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (CodeEdit edit in indexReferences.OrderBy(edit => edit.Start))
        {
            output.Write(code.Span[copied..edit.Start]);
            BeamCompactWriter.Write(output, edit.Tag, edit.Value);
            copied = edit.End;
        }
        output.Write(code.Span[copied..]);

        emittedAtoms = atomMap;
        emittedAtomCount = nextAtom - 1;
        emittedImports = importMap;
        lineInstructions = lineCount;
        lines.Clear();
        filenames.Clear();
        foreach (var line in locations.Select((location, index) => (Location: location, Order: lineMap[index + 1]))
            .Where(line => line.Order != 0).OrderBy(line => line.Order))
        {
            lines.Add(line.Location, line.Order);
            if (line.Location.File is { } filename)
            {
                filenames.TryAdd(filename, filenames.Count + 1);
            }
        }

        return output.ToArray();

        void AssignAtom(int index)
        {
            if (atomMap[index] == 0)
            {
                atomMap[index] = nextAtom++;
            }
        }

        void AssignImport(int index)
        {
            if (importMap[index] < 0)
            {
                importMap[index] = nextImport++;
                AssignAtom(entries[index].Module);
                AssignAtom(entries[index].Function);
            }
        }

        void CollectAtoms(int stop)
        {
            while (nextUse < atomReferences.Count && atomReferences[nextUse].Start < stop)
            {
                CodeEdit atom = atomReferences[nextUse++];
                AssignAtom(atom.Value);
                if (atom.Value != atomMap[atom.Value])
                {
                    indexReferences.Add(atom with { Value = atomMap[atom.Value] });
                }
            }
        }
    }
}
