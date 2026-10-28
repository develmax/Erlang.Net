namespace Erlang;

// Explicit output-construction scope only. No ambient actor/native authority.
internal static class AtomicsHostAggregate594
{
    internal static T Publish<T>(Func<AtomicsHostCopyTransaction591, T> build)
    {
        var transaction = new AtomicsHostCopyTransaction591();
        Exception? primary = null;
        try
        {
            T result = build(transaction);
            transaction.CommitWrappers593();

            return result;
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            try
            {
                transaction.Dispose();
            }
            catch (Exception cleanup) when (primary is not null)
            {
                throw new AggregateException(primary, cleanup);
            }
        }
    }
}
