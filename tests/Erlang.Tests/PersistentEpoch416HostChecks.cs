using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class PersistentEpoch416HostChecks
{
    internal static Task PidKey() => Check(PersistentEpoch416HostNames.Pid);

    internal static Task PortKey() => Check(PersistentEpoch416HostNames.Port);

    internal static Task ReferenceKey() => Check(PersistentEpoch416HostNames.Reference);

    private static async Task Check(string kind)
    {
        await using var runtime = new ProcessRuntime(1);
        object store = PersistentArena416HostAccess.Create(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        string node = Node(kind);
        object? held = null;
        try
        {
            var staged = await Stage(actor, store, kind);
            _ = await actor.Invoke(_ => true);
            GuestStoredFunction407Access.Collected(staged.Wrapper);
            RequireEpoch(runtime, node, true);
            WeakReference<object> fresh = await CheckFresh(
                actor,
                store,
                kind,
                staged.Hash
            );
            _ = await actor.Invoke(_ => true);
            GuestStoredFunction407Access.Collected(fresh);
            held = PersistentArena416HostAccess.Snapshot(store);
            PersistentArena416HostAccess.Require(await Erase(actor, store, kind));
            _ = await actor.Invoke(_ => true);
            IdentifierLifetimeHostAccess.Collect();
            RequireEpoch(runtime, node, true);
            PersistentArena416HostAccess.Dispose(held);
            held = null;
            IdentifierLifetimeHostAccess.Collect();
            RequireEpoch(runtime, node, false);
        }
        finally
        {
            try
            {
                if (held is not null) PersistentArena416HostAccess.Dispose(held);
            }
            finally
            {
                await PersistentArena416HostAccess.Reclaim(store);
            }
        }
        PersistentArena416HostAccess.Empty(store);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<(WeakReference<object> Wrapper, ulong Hash)> Stage(RuntimeNodeFoundationHostActor actor, object store, string kind)
        => actor.InvokeAsync(async context =>
        {
            Term key = Key(kind);
            await PersistentArena416HostAccess.Put(
                store,
                context,
                key,
                Term.I(PersistentEpoch416HostNames.Value)
            );
            ulong hash = (ulong)PersistentArena416HostAccess.Call(store, PersistentArena416HostNames.Hash, key)!;

            return (new WeakReference<object>(key), hash);
        });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<WeakReference<object>> CheckFresh(
        RuntimeNodeFoundationHostActor actor,
        object store,
        string kind,
        ulong hash
    )
        => actor.Invoke(context =>
        {
            Term key = Key(kind);
            // A map-hash scope sweeps unretained epochs before looking this up.
            PersistentArena416HostAccess.Require((ulong)PersistentArena416HostAccess.Call(store, PersistentArena416HostNames.Hash, key)! == hash);
            PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Get(store, context, key).Equals(Term.I(PersistentEpoch416HostNames.Value)));

            return new WeakReference<object>(key);
        });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<bool> Erase(RuntimeNodeFoundationHostActor actor, object store, string kind)
        => actor.InvokeAsync(context => PersistentArena416HostAccess.Erase(store, context, Key(kind)));

    private static Term Key(string kind) => kind switch
    {
        PersistentEpoch416HostNames.Pid => new Pid(Node(kind), 29, PersistentEpoch416HostNames.Creation),
        PersistentEpoch416HostNames.Port => new PortTerm(Node(kind), 31, PersistentEpoch416HostNames.Creation),
        _ => new ReferenceTerm(Node(kind), new uint[] { 1, 4, 16 }, PersistentEpoch416HostNames.Creation)
    };

    private static string Node(string kind) => kind switch
    {
        PersistentEpoch416HostNames.Pid => PersistentEpoch416HostNames.PidNode,
        PersistentEpoch416HostNames.Port => PersistentEpoch416HostNames.PortNode,
        _ => PersistentEpoch416HostNames.ReferenceNode
    };

    private static void RequireEpoch(ProcessRuntime runtime, string node, bool expected)
    {
        object known = IdentifierLifetimeHostAccess.Known(runtime);
        bool retained = (bool)PersistentArena416HostAccess.Call(
            known,
            PersistentEpoch416HostNames.HasEpoch,
            node,
            PersistentEpoch416HostNames.Creation
        )!;
        string[] snapshot = ((IReadOnlyList<string>)PersistentArena416HostAccess.Call(known, PersistentEpoch416HostNames.Snapshot)!).ToArray();
        PersistentArena416HostAccess.Require(retained == expected && snapshot.Contains(node, StringComparer.Ordinal) == expected);
    }

    internal static async Task CreationReuse()
    {
        await using var fixture = await IdentifierLifetimeHostFixture.Create();
        object store = PersistentArena416HostAccess.Create(fixture.Runtime);
        try
        {
            WeakReference<object> original = await StageCreation(fixture, store);
            _ = await fixture.Reader.Invoke(_ => true);
            GuestStoredFunction407Access.Collected(original);
            await fixture.StopKernel();
            // Actual controller adoption and stop select the retained noname DistEntry.
            PersistentArena416HostAccess.Require((await fixture.ReadCreation()).Equals(new Integer(IdentifierLifetimeHostNames.RemoteCreation)));
        }
        finally
        {
            await PersistentArena416HostAccess.Reclaim(store);
        }
        PersistentArena416HostAccess.Empty(store);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference<object>> StageCreation(IdentifierLifetimeHostFixture fixture, object store)
    {
        Term value = await fixture.AdoptAndImport(IdentifierLifetimeHostNames.Pid);
        await fixture.Reader.InvokeAsync(async context =>
        {
            await PersistentArena416HostAccess.Put(
                store,
                context,
                Term.A(PersistentArena416HostNames.Key),
                value
            );

            return true;
        });

        return new(value);
    }
}
