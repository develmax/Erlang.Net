using System.Collections;
using System.Reflection;
using Erlang;

internal static class GenStatemBeamImports
{
    internal static string[] Read(ModuleRegistry registry, byte[] bytes)
    {
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type reader = typeof(ProcessRuntime).Assembly.GetType("Erlang.BeamStructureReader", true)!;
        object decoding = typeof(ModuleRegistry).GetMethod("PreparationDecodeOptions", members)!.Invoke(registry, null)!;
        object structure = reader.GetMethod("Read", members)!.Invoke(null, [(ReadOnlyMemory<byte>)bytes, decoding])!;
        var atoms = (IReadOnlyList<string?>)structure.GetType().GetProperty("Atoms", members)!.GetValue(structure)!;
        var imports = (IEnumerable)structure.GetType().GetProperty("Imports", members)!.GetValue(structure)!;
        var modules = new HashSet<string>(StringComparer.Ordinal);
        foreach (object import in imports)
        {
            int index = (int)import.GetType().GetProperty("ModuleAtomIndex", members)!.GetValue(import)!;
            modules.Add(atoms[index]!);
        }

        return modules.Order(StringComparer.Ordinal).ToArray();
    }
}
