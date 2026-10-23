using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class ReUnicodeCancellation453Access
{
    internal static RuntimeRegexEngine411 Engine(ProcessRuntime runtime)
        => (RuntimeRegexEngine411)Field(runtime, ReUnicodeCancellation453Names.Engine);

    internal static long Work(ProcessContext context)
        => (long)typeof(ProcessContext).GetProperty(ReUnicodeCancellation453Names.Reductions,
            ReUnicodeCancellation453Names.Members)!.GetValue(context)!;

    internal static bool HasOriginalLease(ProcessContext context, object owner)
    {
        lock (Field(context, ReUnicodeCancellation453Names.CodeGate))
        {
            var entries = (Dictionary<ModuleCodeImage, int>)Field(context, ReUnicodeCancellation453Names.ActiveCode);

            return entries.Any(pair => pair.Value > 0 && ReferenceEquals(
                owner,
                typeof(ModuleCodeImage).GetProperty(ReUnicodeCancellation453Names.ExecutionOwner,
                    ReUnicodeCancellation453Names.Members)!.GetValue(pair.Key)
            ));
        }
    }

    private static object Field(object value, string name)
        => value.GetType().GetField(name, ReUnicodeCancellation453Names.Members)!.GetValue(value)!;
}
