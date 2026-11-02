namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool TryGetReadonlyAliasPriority817(PendingAliasAdmission817 admission, ProcessContext owner, out bool priority)
    {
        priority = false;
        if (!System.Threading.Monitor.IsEntered(gate) || !ReferenceEquals(admission.Runtime, this)
            || !ReferenceEquals(admission.Owner, owner)
            || !aliases.TryGetValue(admission.Reference, out var current)
            || !current.Owner.Equals(owner.Self) || current.Reply) return false;
        priority = admission.Priority && current.Priority;

        return true;
    }
}
