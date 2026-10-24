namespace Erlang.Tests;

internal static class PersistentArena416SlotsChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(1);
        object store = PersistentArena416HostAccess.Create(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            await actor.InvokeAsync(async context =>
            {
                var colliding = new List<Term>();
                for (int value = 0; value < PersistentArena416HostNames.SlotSearchLimit && colliding.Count < 3; value++)
                {
                    Term key = Term.I(value);
                    ulong hash = (ulong)PersistentArena416HostAccess.Call(store, PersistentArena416HostNames.Hash, key)!;
                    if ((hash & (PersistentArena416HostNames.InitialCapacity - 1)) == PersistentArena416HostNames.WrapSlot)
                        colliding.Add(key);
                }
                PersistentArena416HostAccess.Require(colliding.Count == 3);
                for (int index = 0; index < colliding.Count; index++)
                    await PersistentArena416HostAccess.Put(
                        store,
                        context,
                        colliding[index],
                        Term.I(index)
                    );

                Term[] Keys() => Cons.Items(PersistentArena416HostAccess.GetAll(store, context))
                    .Select(pair => ((TupleTerm)pair).Items[0]).ToArray();
                PersistentArena416HostAccess.Require(Keys().SequenceEqual([colliding[0], colliding[2], colliding[1]]));
                await PersistentArena416HostAccess.Put(
                    store,
                    context,
                    colliding[1],
                    Term.I(416)
                );
                PersistentArena416HostAccess.Require(Keys().SequenceEqual([colliding[0], colliding[2], colliding[1]]));
                PersistentArena416HostAccess.Require(await PersistentArena416HostAccess.Erase(store, context, colliding[0]));
                PersistentArena416HostAccess.Require(Keys().SequenceEqual([colliding[1], colliding[2]]));
                for (int index = 0; index < PersistentArena416HostNames.AddedKeys; index++)
                    await PersistentArena416HostAccess.Put(
                        store,
                        context,
                        Term.I(PersistentArena416HostNames.AddedKeyBase + index),
                        Term.I(index)
                    );
                object grown = PersistentArena416HostAccess.Info(store, context);
                PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Number(grown, PersistentArena416HostNames.Capacity)
                    == PersistentArena416HostNames.GrownCapacity);
                for (int index = 0; index < PersistentArena416HostNames.AddedKeys; index++)
                    PersistentArena416HostAccess.Require(await PersistentArena416HostAccess.Erase(store, context,
                        Term.I(PersistentArena416HostNames.AddedKeyBase + index)));
                object final = PersistentArena416HostAccess.Info(store, context);
                object allocation = PersistentArena416HostAccess.Allocations(store);
                PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Number(final, PersistentArena416HostNames.Capacity)
                        == PersistentArena416HostNames.InitialCapacity
                    && PersistentArena416HostAccess.Number(final, PersistentArena416HostNames.Count) == 2
                    && PersistentArena416HostAccess.Number(allocation, PersistentArena416HostNames.AllocatedPairs) == 2
                    && PersistentArena416HostAccess.Number(allocation, PersistentArena416HostNames.AllocatedTables) == 1
                    && PersistentArena416HostAccess.Get(store, context, colliding[1]).Equals(Term.I(416))
                    && PersistentArena416HostAccess.Get(store, context, colliding[2]).Equals(Term.I(2)));

                return true;
            });
        }
        finally
        {
            await PersistentArena416HostAccess.Close(store);
        }
        PersistentArena416HostAccess.Empty(store);
    }
}
