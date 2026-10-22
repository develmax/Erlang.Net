using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class PersistentArena416HostAccess
{
    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(PersistentArena416HostNames.Failure);
    }

    internal static object Create(ProcessRuntime runtime)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(PersistentArena416HostNames.StoreType, true)!,
            PersistentArena416HostNames.Members,
            null,
            [runtime],
            null
        )!;

    internal static object? Call(object owner, string name, params object?[] arguments)
    {
        try
        {
            return owner.GetType().GetMethod(name, PersistentArena416HostNames.Members)!.Invoke(owner, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    internal static long Number(object value, string property)
        => Convert.ToInt64(value.GetType().GetProperty(property, PersistentArena416HostNames.Members)!.GetValue(value));

    internal static object Info(object store, ProcessContext context) => Call(store, PersistentArena416HostNames.Info, context)!;

    internal static object Allocations(object store) => Call(store, PersistentArena416HostNames.Allocations)!;

    internal static Task Put(
        object store,
        ProcessContext context,
        Term key,
        Term value,
        bool newOnly = false
    )
        => ((ValueTask)Call(
            store,
            PersistentArena416HostNames.Put,
            context,
            key,
            value,
            newOnly
        )!).AsTask();

    internal static Task<bool> Erase(object store, ProcessContext context, Term key)
        => ((ValueTask<bool>)Call(
            store,
            PersistentArena416HostNames.Erase,
            context,
            key
        )!).AsTask();

    internal static Term Get(
        object store,
        ProcessContext context,
        Term key,
        Term? fallback = null
    )
        => (Term)Call(
            store,
            PersistentArena416HostNames.Get,
            context,
            key,
            fallback
        )!;

    internal static Term GetAll(object store, ProcessContext context)
        => (Term)Call(store, PersistentArena416HostNames.GetAll, context)!;

    internal static Task Reclaim(object store) => (Task)Call(store, PersistentArena416HostNames.Reclaim)!;

    internal static Task Close(object store) => (Task)Call(store, PersistentArena416HostNames.Close)!;

    internal static object Snapshot(object store) => Call(store, PersistentArena416HostNames.Snapshot)!;

    internal static TupleTerm[] Pairs(object read)
        => (TupleTerm[])Call(read, PersistentArena416HostNames.Pairs, CancellationToken.None)!;

    internal static void Dispose(object read) => ((IDisposable)read).Dispose();

    internal static void Empty(object store)
    {
        object report = Allocations(store);
        Require(Number(report, PersistentArena416HostNames.AllocatedPairs) == 0
            && Number(report, PersistentArena416HostNames.AllocatedTables) == 0
            && Number(report, PersistentArena416HostNames.AllocatedPairBytes) == 0
            && Number(report, PersistentArena416HostNames.AllocatedTableBytes) == 0);
    }
}
