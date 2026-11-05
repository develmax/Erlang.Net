using System.Collections;
using System.Reflection;
using Erlang;

internal static class SysBeamImports
{
    internal static string[] Read(ModuleRegistry registry, byte[] bytes)
    {
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type reader = typeof(ProcessRuntime).Assembly.GetType(SysBeamReflectionNames.StructureReader, true)!;
        object decoding = typeof(ModuleRegistry).GetMethod(SysBeamReflectionNames.PreparationDecodeOptions, members)!.Invoke(registry, null)!;
        object structure = reader.GetMethod(SysBeamReflectionNames.Read, members)!.Invoke(null, [(ReadOnlyMemory<byte>)bytes, decoding])!;
        var atoms = (IReadOnlyList<string?>)structure.GetType().GetProperty(SysBeamReflectionNames.Atoms, members)!.GetValue(structure)!;
        var imports = (IEnumerable)structure.GetType().GetProperty(SysBeamReflectionNames.Imports, members)!.GetValue(structure)!;
        var modules = new HashSet<string>(StringComparer.Ordinal);
        foreach (object import in imports)
        {
            int index = (int)import.GetType().GetProperty(SysBeamReflectionNames.ModuleAtomIndex, members)!.GetValue(import)!;
            modules.Add(atoms[index]!);
        }

        return modules.Order(StringComparer.Ordinal).ToArray();
    }
}
