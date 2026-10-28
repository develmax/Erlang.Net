using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.DynamicApplyBoundary520Names;

namespace Erlang.Tests;

internal static class DynamicApplyBoundary520Imports
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static string[] Read(byte[] image)
    {
        Type reader = typeof(ProcessRuntime).Assembly.GetType(ReaderType, true)!;
        object structure;
        try
        {
            structure = reader.GetMethod(ReadMethod, Members)!.Invoke(null, new object?[] { (ReadOnlyMemory<byte>)image, null })!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
        var atoms = (IReadOnlyList<string?>)structure.GetType().GetProperty(AtomsProperty, Members)!.GetValue(structure)!;
        var imports = (IEnumerable)structure.GetType().GetProperty(ImportsProperty, Members)!.GetValue(structure)!;
        var modules = new HashSet<string>(StringComparer.Ordinal);
        foreach (object import in imports)
        {
            int index = (int)import.GetType().GetProperty(ModuleAtomIndexProperty, Members)!.GetValue(import)!;
            modules.Add(atoms[index] ?? throw new InvalidDataException(Failure));
        }

        return modules.Order(StringComparer.Ordinal).ToArray();
    }
}
