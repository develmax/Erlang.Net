using System.Reflection;

namespace Erlang.Tests;

internal static class DebugDirtyNative424Access
{
    private static readonly MethodInfo Read = typeof(ModuleRegistry).GetMethod(
        "IsNativeEntry",
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        [typeof(string), typeof(string), typeof(int)],
        null
    )!;

    internal static bool IsNativeEntry(
        ModuleRegistry registry,
        string module,
        string function,
        int arity
    )
        => Read.CreateDelegate<Func<string, string, int, bool>>(registry)(module, function, arity);
}
