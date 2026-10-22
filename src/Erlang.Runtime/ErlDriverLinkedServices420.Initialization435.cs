namespace Erlang;

internal sealed partial class ErlDriverLinkedServices420
{
    internal ErlDriverLinkedInitialization435 BeginInitialization435()
    {
        Monitor.Enter(gate);
        try
        {
            var scope = new ErlDriverLinkedInitialization435(this, entries);
            EnterLinkedVisibility438();

            return scope;
        }
        catch
        {
            Monitor.Exit(gate);
            throw;
        }
    }

    internal void EndInitialization435(HashSet<ErlDriverLinkedRow420> before, bool committed)
    {
        List<Exception>? failures = null;
        try
        {
            if (!committed)
            {
                // Preserve435's exact surviving prior-row boundary, including
                // genuine removals of an old linked row during nested init.
                while (entries.First is { } node && !before.Contains(node.Value))
                {
                    var row = node.Value;
                    try
                    {
                        host.Remove(row.Identity, CurrentScope438());
                    }
                    catch (Exception error)
                    {
                        (failures ??= []).Add(error);
                    }
                    finally
                    {
                        entries.Remove(node);
                        try
                        {
                            row.Lease.Dispose();
                        }
                        catch (Exception error)
                        {
                            (failures ??= []).Add(error);
                        }
                    }
                }
            }
        }
        catch (Exception error)
        {
            (failures ??= []).Add(error);
        }
        finally
        {
            try
            {
                // Only the outermost physical scope ends the parent gate, after
                // all actual descriptor and worker-lease commit/rollback work.
                LeaveLinkedVisibility438(failures is null);
            }
            catch (Exception error)
            {
                (failures ??= []).Add(error);
            }
            finally
            {
                Monitor.Exit(gate);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
