using Erlang;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class IdentifierLifetimeHostAccess
{
    internal static object Known(ProcessRuntime runtime)
        => typeof(ProcessRuntime).GetProperty(IdentifierLifetimeHostNames.KnownNodes,
            IdentifierLifetimeHostNames.InstanceFlags)!.GetValue(runtime)!;

    internal static object? Observed(ProcessRuntime runtime, Term key)
    {
        object known = Known(runtime);
        object table = known.GetType().GetField(IdentifierLifetimeHostNames.Observed,
            IdentifierLifetimeHostNames.InstanceFlags)!.GetValue(known)!;
        var args = new object?[] { key, null };
        bool found = (bool)table.GetType().GetMethod(IdentifierLifetimeHostNames.TryGetValue)!.Invoke(table, args)!;

        return found ? args[1] : null;
    }

    internal static void Observe(ProcessRuntime runtime, Term term)
    {
        object known = Known(runtime);
        known.GetType().GetMethod(IdentifierLifetimeHostNames.Observe,
            IdentifierLifetimeHostNames.InstanceFlags)!.Invoke(known, new object[] { term });
    }

    internal static object? Current(ProcessRuntime runtime)
        => typeof(ProcessRuntime).GetField(IdentifierLifetimeHostNames.Current,
            IdentifierLifetimeHostNames.InstanceFlags)!.GetValue(runtime);

    internal static int IdentityCount(ProcessRuntime runtime)
        => ((ICollection)typeof(ProcessRuntime).GetField(IdentifierLifetimeHostNames.Identities,
            IdentifierLifetimeHostNames.InstanceFlags)!.GetValue(runtime)!).Count;

    internal static bool IsIdentity(object? value)
        => value?.GetType().FullName == IdentifierLifetimeHostNames.IdentityType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    internal static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(IdentifierLifetimeHostNames.Failure);
    }
}
