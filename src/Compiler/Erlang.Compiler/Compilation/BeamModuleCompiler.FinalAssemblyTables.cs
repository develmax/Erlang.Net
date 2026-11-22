using static Erlang.Compiler.BeamEmissionFormat;
using WireOperand = Erlang.BeamOperandParameters;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    // beam_asm registers each function and assigns indices while visiting its final instructions.
    private ReadOnlyMemory<byte> OrderFinalAssemblyTables(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (sourceModule?.LegacyBeamTables == true)
        {
            return code;
        }

        code = ReorderFinalLineLocations(code, labelCount) ?? code;
        var atomReferences = new List<CodeEdit>();
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
                atoms.Count + 1,
                literalBytes.Count,
                (start, end, atom) => atomReferences.Add(new CodeEdit(
                    start,
                    end,
                    AtomTag,
                    atom
                ))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return code;
        }

        if (body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine)
            || ReadPhysicalFunctionOrder(body) is not { } functionOrder
            || ReadFinalAssemblyClosureOrder(body) is not { } closureOrder)
        {
            return code;
        }

        var currentImports = imports.OrderBy(entry => emittedImports?[entry.Value] ?? entry.Value)
            .Select(entry => entry.Key).ToArray();
        var atomOrder = new Dictionary<int, int> { [1] = 1 };
        var importOrder = new Dictionary<int, int>();
        var literalOrder = new Dictionary<int, int>();
        var edits = new List<CodeEdit>();
        int atomReference = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            int end = body[index + 1].Offset;
            if (HasAssemblyLambdaReference(instruction) && closureOrder.Length != 0
                && (int)instruction.Operands[0].Value is var previous && closureOrder[previous] != previous)
            {
                int start = instruction.Offset + 1;
                var cursor = new BeamCursor(code[start..end]);
                _ = BeamCompactReader.Read(cursor);
                edits.Add(new CodeEdit(
                    start,
                    start + cursor.Position,
                    BeamWire.UnsignedTag,
                    closureOrder[previous]
                ));
            }

            foreach (var reference in ReadAssemblyIndexReferences(code[instruction.Offset..end], instruction))
            {
                CollectAtoms(reference.Start);
                if (reference.IsImport)
                {
                    if ((uint)reference.Index >= currentImports.Length)
                    {
                        return code;
                    }

                    var (module, function, _) = currentImports[reference.Index];
                    AssignAtom(MapEmittedAtom(module));
                    AssignAtom(MapEmittedAtom(function));
                }
                var order = reference.IsImport ? importOrder : literalOrder;
                order.TryAdd(reference.Index, order.Count);
                if (order[reference.Index] != reference.Index)
                {
                    edits.Add(new CodeEdit(
                        reference.Start,
                        reference.End,
                        BeamWire.UnsignedTag,
                        order[reference.Index]
                    ));
                }
            }

            CollectAtoms(end);
        }

        int[] finalAtoms = new int[atoms.Count + 1];
        foreach (int atom in atoms.Values)
        {
            finalAtoms[atom] = atomOrder.GetValueOrDefault(MapEmittedAtom(atom));
        }

        var finalLiterals = literalOrder.OrderBy(entry => entry.Value).Select(entry => literalBytes[entry.Key]).ToArray();
        literalBytes.Clear();
        literalBytes.AddRange(finalLiterals);
        imports.Clear();
        foreach (var entry in importOrder)
        {
            imports.Add(currentImports[entry.Key], entry.Value);
        }

        if (closureOrder.Where((index, oldIndex) => index != oldIndex).Any())
        {
            // The assembler reserves a lambda when its MakeFun is first emitted.
            // Publish the same permutation in Code and the closure table.
            var orderedClosures = new LiftedClosure[closures.Count];
            for (int index = 0; index < closures.Count; index++)
            {
                orderedClosures[closureOrder[index]] = closures[index];
            }

            closures.Clear();
            closures.AddRange(orderedClosures);
        }

        emittedImports = null;
        emittedAtoms = finalAtoms;
        emittedAtomCount = atomOrder.Count;
        exports.Sort((left, right) => CompareEntry(left.Entry, right.Entry));
        locals.Sort((left, right) => CompareEntry(left.Entry, right.Entry));
        if (edits.Count == 0)
        {
            return OrderTypeOperandsByFirstUse(code, labelCount, discardUnused: true);
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (CodeEdit edit in edits)
        {
            output.Write(code.Span[copied..edit.Start]);
            BeamCompactWriter.Write(output, edit.Tag, edit.Value);
            copied = edit.End;
        }
        output.Write(code.Span[copied..]);

        return OrderTypeOperandsByFirstUse(output.ToArray(), labelCount, discardUnused: true);

        int CompareEntry(int left, int right) =>
            functionOrder[MapEmittedLabel(left)].CompareTo(functionOrder[MapEmittedLabel(right)]);

        void AssignAtom(int value) => atomOrder.TryAdd(value, atomOrder.Count + 1);

        void CollectAtoms(int before)
        {
            while (atomReference < atomReferences.Count && atomReferences[atomReference].Start < before)
            {
                CodeEdit reference = atomReferences[atomReference++];
                AssignAtom(reference.Value);
                if (atomOrder[reference.Value] != reference.Value)
                {
                    edits.Add(reference with { Value = atomOrder[reference.Value] });
                }
            }
        }
    }

    private int[]? ReadFinalAssemblyClosureOrder(IReadOnlyList<BeamInstruction> body)
    {
        if (closures.Count == 0)
        {
            return [];
        }

        var functions = exports.Concat(locals)
            .Select(entry => (entry.Atom, entry.Arity, Label: MapEmittedLabel(entry.Entry))).ToHashSet();
        var labels = new HashSet<int>();
        foreach (LiftedClosure closure in closures)
        {
            if (!atoms.TryGetValue(closure.Name, out int name) || closure.CaptureCount > closure.Arity
                || closure.Entry < 0 && !deferredEntries.ContainsKey(closure.Entry))
            {
                return null;
            }

            int label = MapClosureEntryLabel(closure.Entry);
            if (!labels.Add(label) || !functions.Contains((name, closure.Arity, label)))
            {
                return null;
            }
        }

        return ReadClosureRegistrations(body, closures.Select(closure => closure.CaptureCount).ToArray());
    }

    private static int[]? ReadClosureRegistrations(IReadOnlyList<BeamInstruction> body, IReadOnlyList<int> captures)
    {
        var ordinals = new int[captures.Count];
        Array.Fill(ordinals, -1);
        int rooted = 0;
        foreach (BeamInstruction instruction in body)
        {
            byte opcode = instruction.Definition.Opcode;
            if (opcode is LegacyMakeFunWithOldUnique or LegacyMakeFun
                || opcode == CallFunTyped && instruction.Operands[0].Kind is not (WireOperand.Atom or WireOperand.Unsigned))
            {
                return null;
            }

            if (!HasAssemblyLambdaReference(instruction))
            {
                continue;
            }

            if (instruction.Operands[0] is not { Kind: WireOperand.Unsigned } index
                || index.Value.Sign < 0 || index.Value >= captures.Count)
            {
                return null;
            }

            int previous = (int)index.Value;
            int captureCount = captures[previous];
            if (opcode == MakeFun && (instruction.Operands[2] is not { Kind: WireOperand.Count } count
                || count.Value != captureCount || instruction.Operands.Count != 3 + captureCount))
            {
                return null;
            }

            if (ordinals[previous] < 0)
            {
                ordinals[previous] = rooted++;
            }
        }

        return rooted == captures.Count ? ordinals : null;
    }

    private static bool HasAssemblyLambdaReference(BeamInstruction instruction) =>
        instruction.Definition.Opcode == MakeFun
            || instruction.Definition.Opcode == CallFunTyped && instruction.Operands[0].Kind == WireOperand.Unsigned;

    private Dictionary<int, int>? ReadPhysicalFunctionOrder(IReadOnlyList<BeamInstruction> body)
    {
        var entries = new Dictionary<int, int>();
        bool needsEntry = false;
        foreach (BeamInstruction instruction in body)
        {
            if (instruction.Definition.Opcode == FunctionInfo)
            {
                needsEntry = true;
            }
            else if (needsEntry && instruction.Definition.Opcode == Label)
            {
                if (!entries.TryAdd((int)instruction.Operands[0].Value, entries.Count))
                {
                    return null;
                }

                needsEntry = false;
            }
        }

        return !needsEntry && entries.Count == exports.Count + locals.Count
            && exports.Concat(locals).All(entry => entries.ContainsKey(MapEmittedLabel(entry.Entry))) ? entries : null;
    }

    private static IEnumerable<(int Start, int End, int Index, bool IsImport)> ReadAssemblyIndexReferences(
        ReadOnlyMemory<byte> instructionBytes,
        BeamInstruction instruction
    )
    {
        int import = ReadAssemblyImportPosition(instruction.Definition.Opcode);
        if (import < 0 && !instruction.Operands.Any(operand => operand.Kind == WireOperand.LiteralValue))
        {
            yield break;
        }

        var cursor = new BeamCursor(instructionBytes[1..]);
        int offset = instruction.Offset + 1;
        for (int index = 0; index < instruction.Operands.Count; index++)
        {
            int start = cursor.Position;
            BeamCompactValue operand = BeamCompactReader.Read(cursor);
            if (index == import)
            {
                yield return (offset + start, offset + cursor.Position, (int)operand.Value, true);
            }

            if (operand.WireTag != WireOperand.ExtendedTag)
            {
                continue;
            }

            switch ((int)operand.Value)
            {
                case WireOperand.Literal:
                    start = cursor.Position;
                    var value = BeamCompactReader.Read(cursor);
                    yield return (offset + start, offset + cursor.Position, (int)value.Value, false);
                    break;
                case WireOperand.ArgumentList:
                case WireOperand.FloatRegister:
                    _ = BeamCompactReader.Read(cursor);
                    break;
                case WireOperand.TypedRegister:
                    _ = BeamCompactReader.Read(cursor);
                    _ = BeamCompactReader.Read(cursor);
                    break;
                case WireOperand.AllocationList:
                    int count = (int)BeamCompactReader.Read(cursor).Value;
                    for (int part = 0; part < count; part++)
                    {
                        _ = BeamCompactReader.Read(cursor);
                        _ = BeamCompactReader.Read(cursor);
                    }
                    break;
            }
        }
    }

    private static int ReadAssemblyImportPosition(byte opcode) => opcode switch
    {
        ExternalCall or TailExternalCall or TailExternalCallWithFrame => 1,
        BifZero => 0,
        BifOne or BifTwo or BifThree => 1,
        GcBifOne or GcBifTwo or GcBifThree => 2,
        _ => -1
    };
}
