using Erlang;
using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class MapHashRetentionHostChecks
{
    internal static async Task ScalarsAndEpochs()
    {
        await using var runtime = new ProcessRuntime();
        Term[] keys =
        [
            new Pid(MapHashRetentionHostNames.Remote, 1, 1),
            new PortTerm(MapHashRetentionHostNames.Remote, 2, 1),
            new ReferenceTerm(MapHashRetentionHostNames.Remote, new uint[] { 1, 2, 3 }, 1),
            new Pid(MapHashRetentionHostNames.Remote, 3, 2)
        ];
        using var scope = new ProplistOwnHashScope(runtime);
        object? identity = null;
        foreach (Term key in keys)
        {
            MapHashRetentionHostAccess.Check(IdentifierLifetimeHostAccess.Observed(runtime, key) is null);
            ulong first = scope.Hash(key);
            MapHashRetentionHostAccess.Check(first == scope.Hash(key));
            object actual = MapHashRetentionHostAccess.Identity(runtime, key);
            identity ??= actual;
            MapHashRetentionHostAccess.Check(ReferenceEquals(identity, actual));
        }
        object[] epochs = MapHashRetentionHostAccess.Epochs(runtime);
        MapHashRetentionHostAccess.Check(epochs.Length == 2 && epochs.All(epoch => MapHashRetentionHostAccess.Active(epoch) == 1));
        scope.Dispose();
        MapHashRetentionHostAccess.Check(epochs.All(epoch => MapHashRetentionHostAccess.Active(epoch) == 0));
        GC.KeepAlive(keys);
    }

    internal static async Task NormalOrderAndNestedEnvironment()
    {
        await using var runtime = new ProcessRuntime();
        Term first = new Pid(MapHashRetentionHostNames.Remote, 1, 1);
        IdentifierLifetimeHostAccess.Observe(runtime, first);
        object identity = MapHashRetentionHostAccess.Identity(runtime, first);
        using var scope = new ProplistOwnHashScope(runtime);
        _ = scope.Hash(first);
        MapHashRetentionHostAccess.Check(ReferenceEquals(identity, MapHashRetentionHostAccess.Identity(runtime, first)));
        Term second = new PortTerm(MapHashRetentionHostNames.Remote, 2, 1);
        _ = scope.Hash(second);
        IdentifierLifetimeHostAccess.Observe(runtime, second);
        MapHashRetentionHostAccess.Check(ReferenceEquals(identity, MapHashRetentionHostAccess.Identity(runtime, second)));
        Term reference = new ReferenceTerm(MapHashRetentionHostNames.Remote, new uint[] { 1, 2, 3 }, 1);
        int invoked = 0;
        FunctionTerm carrier = MapHashRetentionHostAccess.Carrier(reference, () => Interlocked.Increment(ref invoked));
        MapHashRetentionHostAccess.Check(IdentifierLifetimeHostAccess.Observed(runtime, reference) is null);
        _ = scope.Hash(carrier);
        MapHashRetentionHostAccess.Check(Volatile.Read(ref invoked) == 0);
        MapHashRetentionHostAccess.Check(ReferenceEquals(identity, MapHashRetentionHostAccess.Identity(runtime, reference)));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context => await carrier.Invoke(context, Array.Empty<Term>()));
        MapHashRetentionHostAccess.Check(Volatile.Read(ref invoked) == 1 && result.Equals(carrier.Environment[0]));
        GC.KeepAlive(carrier);
    }

    internal static async Task LocalForeignAndCapturedSnapshot()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await using var kernel = new RuntimeNodeFoundationHostActor(foreign);
        await using var actor = new RuntimeNodeFoundationHostActor(foreign);
        Pid foreignPid = (await actor.Ready).Self;
        using var captured = new ProplistOwnHashScope(runtime);
        ulong before = captured.Hash(foreignPid);
        await kernel.InvokeAsync(async context =>
        {
            foreign.Register(RuntimeNodeControlNames.Kernel, context.Self);

            return await foreign.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(MapHashRetentionHostNames.Foreign),
                new Integer(MapHashRetentionHostNames.LocalCreation)
            );
        });
        MapHashRetentionHostAccess.Check(before == captured.Hash(foreignPid));
        var resource = foreign.ReferenceResources.CreateBinary(new byte[] { 1 });
        Term port = await actor.InvokeAsync(async context => await foreign.Modules.Call(
            context,
            ErlangModuleNames.Module,
            PortNames.Open,
            Term.Tuple(Term.A(PortDescriptorNames.Kind), Term.I(0), Term.I(1)),
            Term.List(Term.A(PortNames.WriteOnlyOption))
        ));
        try
        {
            using var current = new ProplistOwnHashScope(runtime);
            Term[] values = [runtime.InitializationPid, runtime.MakeReference(), foreignPid, foreign.MakeReference(), resource, port];
            foreach (Term value in values)
            {
                _ = current.Hash(value);
                MapHashRetentionHostAccess.Check(IdentifierLifetimeHostAccess.Observed(runtime, value) is null);
            }
            using var lease = foreign.ReferenceResources.AcquireBinary(resource);
            MapHashRetentionHostAccess.Check(lease is not null);
        }
        finally
        {
            await actor.InvokeAsync(async context => await foreign.Modules.Call(
                context,
                ErlangModuleNames.Module,
                PortNames.Close,
                port
            ));
        }
    }

    internal static async Task CloseAndRelease()
    {
        await using var runtime = new ProcessRuntime();
        Term key = new Pid(MapHashRetentionHostNames.Remote, 1, 1);
        using var scope = new ProplistOwnHashScope(runtime);
        _ = scope.Hash(key);
        object epoch = MapHashRetentionHostAccess.Epochs(runtime).Single();
        MapHashRetentionHostAccess.Check(MapHashRetentionHostAccess.Active(epoch) == 1);
        await runtime.DisposeAsync();
        MapHashRetentionHostAccess.Check(MapHashRetentionHostAccess.Active(epoch) == 1);
        MapHashRetentionHostAccess.Closed(() => scope.Hash(new Pid(MapHashRetentionHostNames.Remote, 2, 1)));
        MapHashRetentionHostAccess.Closed(() =>
 {
     using var rejected = new ProplistOwnHashScope(runtime);
 });
        MapHashRetentionHostAccess.Check(IdentifierLifetimeHostAccess.Observed(runtime, key) is null);
        scope.Dispose();
        scope.Dispose();
        MapHashRetentionHostAccess.Check(MapHashRetentionHostAccess.Active(epoch) == 0);
        MapHashRetentionHostAccess.Check(IdentifierLifetimeHostAccess.Current(runtime) is null && IdentifierLifetimeHostAccess.IdentityCount(runtime) == 0);
        GC.KeepAlive(key);
    }

    internal static async Task WeakKeysAndChurn()
    {
        await using var runtime = new ProcessRuntime();
        var weak = HashTemporary(runtime);
        IdentifierLifetimeHostAccess.Collect();
        MapHashRetentionHostAccess.Check(!weak.Key.TryGetTarget(out _) && !weak.Identity.TryGetTarget(out _));
        Churn(runtime, 0, MapHashRetentionHostNames.FirstChurn);
        IdentifierLifetimeHostAccess.Collect();
        Churn(runtime, MapHashRetentionHostNames.FirstChurn, MapHashRetentionHostNames.SecondChurn);
        MapHashRetentionHostAccess.Check(IdentifierLifetimeHostAccess.IdentityCount(runtime) <= MapHashRetentionHostNames.ChurnBound);
        MapHashRetentionHostAccess.Check(runtime.InitializeDistributionPending(1).MaximumNodes == 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference<Term> Key, WeakReference<object> Identity) HashTemporary(ProcessRuntime runtime)
    {
        Term key = new Pid(MapHashRetentionHostNames.Remote, 1, 1);
        using var scope = new ProplistOwnHashScope(runtime);
        _ = scope.Hash(key);

        return (new(key), new(MapHashRetentionHostAccess.Identity(runtime, key)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Churn(ProcessRuntime runtime, int start, int count)
    {
        for (int index = start; index < start + count; index++)
        {
            using var scope = new ProplistOwnHashScope(runtime);
            _ = scope.Hash(new Pid(MapHashRetentionHostNames.ChurnPrefix + index + MapHashRetentionHostNames.ChurnSuffix, 1, 1));
        }
    }

    internal static async Task ConcurrentScopes()
    {
        await using var runtime = new ProcessRuntime();
        Term same = new Pid(MapHashRetentionHostNames.Remote, 1, 1);
        Term[] keys = Enumerable.Range(1, MapHashRetentionHostNames.ConcurrentCount)
            .Select(index => (Term)new Pid(MapHashRetentionHostNames.Remote, (uint)index, 1)).ToArray();
        await Task.WhenAll(keys.Select(key => Task.Run(() =>
        {
            using var scope = new ProplistOwnHashScope(runtime);
            _ = scope.Hash(same);
            _ = scope.Hash(key);
        })));
        object identity = MapHashRetentionHostAccess.Identity(runtime, same);
        MapHashRetentionHostAccess.Check(keys.All(key => ReferenceEquals(identity, MapHashRetentionHostAccess.Identity(runtime, key))));
        MapHashRetentionHostAccess.Check(MapHashRetentionHostAccess.Epochs(runtime).All(epoch => MapHashRetentionHostAccess.Active(epoch) == 0));
        GC.KeepAlive(same);
        GC.KeepAlive(keys);
    }

    internal static Task HeldPid() => Held(IdentifierLifetimeHostNames.Pid);

    internal static Task HeldPort() => Held(IdentifierLifetimeHostNames.Port);

    internal static Task HeldReference() => Held(IdentifierLifetimeHostNames.Reference);

    internal static Task HeldFunction() => Held(IdentifierLifetimeHostNames.Function);

    private static async Task Held(string kind)
    {
        await using var fixture = await MapHashRetentionHostFixture.Create();
        Term root = await fixture.AdoptAndHash(kind);
        IdentifierLifetimeHostAccess.Collect();
        await fixture.StopKernel();
        MapHashRetentionHostAccess.Check((await fixture.ReadCreation()).Equals(new Integer(MapHashRetentionHostNames.RemoteCreation)));
        Term key = IdentifierLifetimeHostFixture.Key(root);
        MapHashRetentionHostAccess.Check(ReferenceEquals(MapHashRetentionHostAccess.Identity(fixture.Runtime, key), IdentifierLifetimeHostAccess.Current(fixture.Runtime)));
        GC.KeepAlive(root);
    }
}
