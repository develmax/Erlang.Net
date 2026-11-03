namespace Erlang;

internal sealed class WritableBinaryCollection : IDisposable
{
    private readonly List<(OrdinaryBinaryBacking Owner, WritableBinaryBinding Binding, OrdinaryBinaryMutation Mutation)> items = new();

    internal void Add(OrdinaryBinaryBacking owner, WritableBinaryBinding binding, OrdinaryBinaryMutation mutation)
    {
        try
        {
            items.Add((owner, binding, mutation));
        }
        catch
        {
            mutation.Dispose();
            throw;
        }
    }

    internal void Commit()
    {
        foreach (var item in items) item.Owner.CommitMutation(item.Mutation, item.Binding);
    }

    public void Dispose()
    {
        List<Exception>? errors = null;
        foreach (var item in items)
            try
            {
                item.Mutation.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new()).Add(error);
            }
        items.Clear();
        if (errors is not null) throw new AggregateException(errors);
    }
}
