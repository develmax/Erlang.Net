namespace Erlang;

// Name visibility is independent of callback execution/physical image custody.
internal sealed class NativeResourceImageNamesRetirement529 : IDisposable
{
    private readonly NativeResourceTypeTable527 table;
    private readonly ModuleCodeImage image;
    private readonly LinkedListNode<NativeResourceType524>[] nodes;
    private readonly List<Exception> failures;
    private bool entered, committed;

    internal NativeResourceImageNamesRetirement529(NativeResourceTypeTable527 table, ModuleCodeImage image)
    {
        this.table = table;
        this.image = image;
        table.EnterNames529();
        entered = true;
        try
        {
            nodes = table.ImageNameNodes529(image);
            failures = new(nodes.Length);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal void Commit529()
    {
        foreach (var node in nodes)
            table.UnlinkName529(node);
        committed = true;
    }

    internal void Finish529()
    {
        if (!committed) return;
        foreach (var node in nodes)
        {
            try
            {
                if (node.Value.MarkImageRetired529(image))
                    table.TryRetireEmpty527(node.Value);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    public void Dispose()
    {
        if (!entered) return;
        entered = false;
        table.LeaveNames529();
    }
}
