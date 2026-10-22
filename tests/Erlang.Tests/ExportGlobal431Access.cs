using System.Reflection;

namespace Erlang.Tests;

internal static class ExportGlobal431Access
{
    internal static bool HasModuleMarker(ProcessRuntime runtime, Term function)
    {
        object markers = typeof(ProcessRuntime).GetField("sharingLiterals425", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        object?[] arguments = [function, null];

        return (bool)markers.GetType().GetMethod("TryGetValue")!.Invoke(markers, arguments)!;
    }
}
