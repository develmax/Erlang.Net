namespace Erlang;

internal sealed partial class NativeResourceTypeTable527
{
    private readonly object namesGate529 = new();

    internal void EnterNames529() => Monitor.Enter(namesGate529);

    internal void LeaveNames529() => Monitor.Exit(namesGate529);

    internal NativeResourceImageNamesRetirement529 PrepareImageNamesRetirement529(ModuleCodeImage image)
        => new(this, image);

    internal LinkedListNode<NativeResourceType524>[] ImageNameNodes529(ModuleCodeImage image)
    {
        var nodes = new List<LinkedListNode<NativeResourceType524>>();
        for (var node = committed.First; node is not null; node = node.Next)
            if (ReferenceEquals(node.Value.CurrentImageForNames529, image))
                nodes.Add(node);

        return nodes.ToArray();
    }

    internal void UnlinkName529(LinkedListNode<NativeResourceType524> node)
        => committed.Remove(node);

    // Publication may replace callback versions, so the callback writer is
    // acquired first. Logical unlink needs only the name writer and never
    // waits for a user C callback or disposes an image under the binding gate.
    internal void EnterPublication529()
    {
        callbackGate.EnterWriteLock();
        try
        {
            Monitor.Enter(namesGate529);
        }
        catch
        {
            callbackGate.ExitWriteLock();
            throw;
        }
    }

    internal void LeavePublication529()
    {
        Monitor.Exit(namesGate529);
        callbackGate.ExitWriteLock();
    }
}
