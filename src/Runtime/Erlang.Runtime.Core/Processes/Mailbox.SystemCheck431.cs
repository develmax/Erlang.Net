namespace Erlang;

public sealed partial class Mailbox
{
    // Called only after the owning auxiliary batch has physically completed.
    // Unlike general Remove, do not re-admit control signals on a dead caller.
    internal void RemoveSystemCheckReferences431(ReferenceTerm reference)
    {
        List<Exception>? failures = null;
        lock (gate)
        {
            for (var node = messages.First; node is not null;)
            {
                var next = node.Next;
                if (!node.Value.Control && node.Value.Admission is null
                    && node.Value.Value is ReferenceTerm id && id.Equals(reference))
                {
                    try
                    {
                        RemoveNode(node);
                    }
                    catch (Exception error)
                    {
                        (failures ??= new()).Add(error);
                    }
                }
                node = next;
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
