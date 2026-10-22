using System.Reflection;
using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class HamtIndex429HostAccess
{
    internal const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(HamtIndex429HostNames.Failure);
    }

    internal static object Property(object value, string name) => value.GetType().GetProperty(name, Flags)!.GetValue(value)!;

    internal static object Owner(ProcessRuntime runtime) => typeof(ProcessRuntime).GetField(HamtIndex429HostNames.Owner, Flags)!.GetValue(runtime)!;

    internal static IDisposable Acquire(ProcessRuntime runtime, MapTerm map)
    {
        try
        {
            return (IDisposable)typeof(ProcessRuntime).GetMethod(HamtIndex429HostNames.Acquire, Flags)!.Invoke(runtime, [map])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static Term Protocol(ProcessRuntime runtime, string method, Term value)
    {
        try
        {
            return (Term)typeof(ProcessRuntime).Assembly.GetType(HamtIndex429HostNames.Protocol)!.GetMethod(method, Flags)!.Invoke(null, [runtime, value])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static int Rows(ProcessRuntime runtime) => (int)Property(Owner(runtime), HamtIndex429HostNames.Rows);

    internal static int ActiveEpochScopes(ProcessRuntime runtime)
    {
        object nodes = typeof(ProcessRuntime).GetProperty("KnownNodes", Flags)!.GetValue(runtime)!;
        var epochs = (System.Collections.IDictionary)nodes.GetType().GetField("mapHashEpochs", Flags)!.GetValue(nodes)!;
        int active = 0;
        foreach (object epoch in epochs.Values) active += (int)Property(epoch, "ActiveScopes");

        return active;
    }

    internal static MapTerm Map(bool external, int count = 256)
        => new(Enumerable.Range(0, count).Select(index => new KeyValuePair<Term, Term>(
            external ? new Pid("hamt429@host", (ulong)index, 1) : Term.I(index), Term.Tuple(Term.I(index), Term.A("payload")))));

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static (WeakReference Map, Term Node) Escape(ProcessRuntime runtime)
    {
        MapTerm map = Map(true);
        using IDisposable read = Acquire(runtime, map);
        Term node = Cons.Items(Protocol(runtime, HamtIndex429HostNames.Children, map))
            .First(value => value.GetType().Name == HamtIndex429HostNames.Node);

        return (new(map), node);
    }

    internal static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static (WeakReference Map, Term Node)[] CreateHolder(ProcessRuntime runtime)
        => [Escape(runtime)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void CheckHeld(ProcessRuntime runtime, (WeakReference Map, Term Node)[] holder)
    {
        Collect();
        Require(!holder[0].Map.IsAlive && Rows(runtime) == 1 && ActiveEpochScopes(runtime) == 1);
        Require(Cons.Items(Protocol(runtime, HamtIndex429HostNames.Children, holder[0].Node)).Any());
        GC.KeepAlive(holder);
    }
}
