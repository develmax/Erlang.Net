using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private int[]? emittedAtoms;
    private int emittedAtomCount;

    private int MapEmittedAtom(int atom) => emittedAtoms?[atom] ?? atom;

    private ReadOnlyMemory<byte> CompactFinalAtoms(ReadOnlyMemory<byte> code, int labelCount)
    {
        var present = new bool[atoms.Count + 1];
        var references = new List<(int Start, int End, int Atom)>();
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            BeamInstructionReader.ReadWithAtomOperands(
                header,
                present.Length,
                literalBytes.Count,
                (start, end, atom) =>
                {
                    present[atom] = true;
                    references.Add((start, end, atom));
                }
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return code;
        }

        present[1] = true;
        foreach (var (module, function, _) in imports.Keys)
        {
            present[module] = true;
            present[function] = true;
        }
        foreach (var (atom, _, _) in exports.Concat(locals))
        {
            present[atom] = true;
        }
        foreach (var closure in closures)
        {
            present[atoms[closure.Name]] = true;
        }

        int firstUnused = Array.IndexOf(present, false, 1);
        if (firstUnused < 0)
        {
            return code;
        }

        // The final assembler interns only atoms that survive instruction selection.
        // Keep the assigned relative order of all live atoms, including table roots.
        var map = new int[present.Length];
        for (int atom = 1; atom < firstUnused; atom++)
        {
            map[atom] = atom;
        }

        int next = firstUnused;
        for (int atom = firstUnused + 1; atom < present.Length; atom++)
        {
            if (present[atom])
            {
                map[atom] = next++;
            }
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        foreach (var (start, end, atom) in references)
        {
            if (map[atom] == atom)
            {
                continue;
            }

            output.Write(code.Span[copied..start]);
            BeamCompactWriter.Write(output, AtomTag, map[atom]);
            copied = end;
        }
        output.Write(code.Span[copied..]);
        emittedAtoms = map;
        emittedAtomCount = next - 1;

        return output.ToArray();
    }
}
