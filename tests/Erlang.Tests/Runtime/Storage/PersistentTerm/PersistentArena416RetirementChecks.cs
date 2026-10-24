namespace Erlang.Tests;

internal static class PersistentArena416RetirementChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(1);
        object store = PersistentArena416HostAccess.Create(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object? held = null;
        try
        {
            await actor.InvokeAsync(async context =>
            {
                Term key = Term.A(PersistentArena416HostNames.Key);
                Term old = Term.Tuple(new BitString(new byte[8192]), Term.List(Term.I(1), Term.I(2)));
                await PersistentArena416HostAccess.Put(
                    store,
                    context,
                    key,
                    old
                );
                object before = PersistentArena416HostAccess.Info(store, context);
                held = PersistentArena416HostAccess.Snapshot(store);
                await PersistentArena416HostAccess.Put(
                    store,
                    context,
                    key,
                    Term.I(3)
                );
                object current = PersistentArena416HostAccess.Info(store, context);
                object retained = PersistentArena416HostAccess.Allocations(store);
                PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Number(current, PersistentArena416HostNames.Count) == 1
                    && PersistentArena416HostAccess.Number(retained, PersistentArena416HostNames.AllocatedPairs) == 2
                    && PersistentArena416HostAccess.Number(retained, PersistentArena416HostNames.AllocatedTables) == 2
                    && PersistentArena416HostAccess.Number(retained, PersistentArena416HostNames.AllocatedPairBytes)
                        == PersistentArena416HostAccess.Number(before, PersistentArena416HostNames.PairBytes)
                            + PersistentArena416HostAccess.Number(current, PersistentArena416HostNames.PairBytes)
                    && PersistentArena416HostAccess.Number(current, PersistentArena416HostNames.Memory)
                        == PersistentArena416HostAccess.Number(current, PersistentArena416HostNames.TableBytes)
                            + PersistentArena416HostAccess.Number(current, PersistentArena416HostNames.PairBytes));
                PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Pairs(held!).Single().Items[1].Equals(old));
                PersistentArena416HostAccess.Require(await PersistentArena416HostAccess.Erase(store, context, key));
                object empty = PersistentArena416HostAccess.Info(store, context);
                PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Number(empty, PersistentArena416HostNames.Count) == 0
                    && PersistentArena416HostAccess.Number(empty, PersistentArena416HostNames.PairBytes) == 0
                    && PersistentArena416HostAccess.Number(empty, PersistentArena416HostNames.Memory)
                        == PersistentArena416HostAccess.Number(empty, PersistentArena416HostNames.TableBytes)
                    && PersistentArena416HostAccess.Number(PersistentArena416HostAccess.Allocations(store), PersistentArena416HostNames.AllocatedPairs) == 1);
                PersistentArena416HostAccess.Dispose(held!);
                held = null;
                PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Number(PersistentArena416HostAccess.Allocations(store), PersistentArena416HostNames.AllocatedPairs) == 0);
                await PersistentArena416HostAccess.Put(
                    store,
                    context,
                    key,
                    Term.Tuple(Term.I(7))
                );
                held = PersistentArena416HostAccess.Snapshot(store);
                await PersistentArena416HostAccess.Close(store);
                Task reclamation = PersistentArena416HostAccess.Reclaim(store);
                PersistentArena416HostAccess.Require(!reclamation.IsCompleted);
                object afterClose = PersistentArena416HostAccess.Allocations(store);
                PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Number(afterClose, PersistentArena416HostNames.AllocatedPairs) == 1
                    && PersistentArena416HostAccess.Number(afterClose, PersistentArena416HostNames.AllocatedTables) == 1
                    && PersistentArena416HostAccess.Pairs(held!).Single().Items[1].Equals(Term.Tuple(Term.I(7))));
                bool refused = false;
                try
                {
                    _ = PersistentArena416HostAccess.Get(store, context, key);
                }
                catch (ObjectDisposedException)
                {
                    refused = true;
                }
                PersistentArena416HostAccess.Require(refused);
                PersistentArena416HostAccess.Dispose(held!);
                held = null;
                await reclamation;
                PersistentArena416HostAccess.Empty(store);

                return true;
            });
        }
        finally
        {
            if (held is not null) PersistentArena416HostAccess.Dispose(held);
            await PersistentArena416HostAccess.Close(store);
        }
    }
}
