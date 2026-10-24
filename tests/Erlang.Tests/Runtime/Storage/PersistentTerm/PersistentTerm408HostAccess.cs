using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class PersistentTerm408HostAccess
{
    internal static object Store(ProcessRuntime runtime)
        => typeof(ProcessRuntime).GetProperty(PersistentTerm408HostNames.Property, PersistentTerm408HostNames.Members)!.GetValue(runtime)!;

    internal static object? Call(object store, string method, params object?[] arguments)
    {
        MethodInfo selected = store.GetType().GetMethods(PersistentTerm408HostNames.Members)
            .Single(candidate => candidate.Name == method && candidate.GetParameters().Length == arguments.Length);
        try
        {
            return selected.Invoke(store, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static Task<Term> Put(
        object store,
        ProcessContext context,
        Term key,
        Term value,
        bool newOnly = false
    )
        => ((ValueTask<Term>)Call(
            store,
            PersistentTerm408HostNames.Put,
            context,
            key,
            value,
            newOnly
        )!).AsTask();

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(PersistentTerm408HostNames.Failure);
    }
}
