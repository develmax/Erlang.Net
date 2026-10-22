namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    internal void Link(ProcessContext owner, PortTerm identity, bool priority)
    {
        lock (gate)
        {
            runtime.RequireDriverParentActor422(owner);
            var row = Require(identity);
            bool addedPriority = priority && owner.PriorityPortLinks.Add(identity);
            bool addedOwner = false;
            try
            {
                addedOwner = owner.PortLinks.Add(identity);
                row.Links.Add(owner.Self);
            }
            catch
            {
                if (addedOwner) owner.PortLinks.Remove(identity);
                if (addedPriority) owner.PriorityPortLinks.Remove(identity);
                throw;
            }
            if (!priority) owner.PriorityPortLinks.Remove(identity);
        }
    }

    internal void Unlink(ProcessContext owner, PortTerm identity)
    {
        lock (gate)
        {
            owner.PortLinks.Remove(identity);
            owner.PriorityPortLinks.Remove(identity);
            if (ports.TryGetValue(identity, out var row)) row.Links.Remove(owner.Self);
        }
    }
}
