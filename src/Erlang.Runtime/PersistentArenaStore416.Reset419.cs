namespace Erlang;

internal sealed partial class PersistentArenaStore416
{
    internal async ValueTask EraseAll419(ProcessContext context)
    {
        runtime.RequirePersistentActor(context);
        await updates.WaitAsync(context.Cancellation).ConfigureAwait(false);
        PersistentTable416? replacement = null;
        PersistentTable416? previous = null;
        PersistentPairArena416[] owners = [];
        bool published = false;
        try
        {
            replacement = PersistentTable416.Create(PersistentTableNames416.InitialCapacity, [], accounting: accounting);
            lock (gate)
            {
                previous = Table();
                owners = previous.AreaOwnersForRetirement().ToArray();
            }
            // Allocate all failure storage before the publication boundary.
            var failures = new List<Exception>(checked(owners.Length + 1));
            runtime.PublishPersistent(context, () =>
            {
                lock (gate)
                {
                    _ = Table();
                    current = replacement;
                    published = true;
                    replacement = null;

                    return true;
                }
            });
            // Existing snapshots keep their actual native blocks alive. They do
            // not prevent publishing the empty table or subsequent writes.
            for (int index = 0; index < owners.Length; index++)
            {
                try
                {
                    owners[index].Retire();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                if ((index + 1) % PersistentTableNames416.RetirementUnits == 0) await Task.Yield();
            }
            try
            {
                previous.Retire();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (failures.Count != 0) throw new AggregateException(failures);
        }
        finally
        {
            try
            {
                replacement?.Retire();
                if (published) previous?.Retire();
            }
            finally
            {
                updates.Release();
            }
        }
    }
}
