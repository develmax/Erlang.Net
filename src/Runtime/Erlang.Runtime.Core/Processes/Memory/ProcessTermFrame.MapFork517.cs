namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    internal ProcessTermConstruction514 BeginMapFork517(ProcessTermConstruction514 parent, IReadOnlyList<Term> children)
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed || parent.Frame517.disposed, this);
            if (!ReferenceEquals(owner, parent.Frame517.owner) || constructionCustody514 is not null)
                throw new InvalidOperationException(ProcessTermConstruction514Names.InvalidCustody);
            foreach (Term child in children)
                if (!parent.Contains(child))
                    throw new InvalidOperationException(ProcessTermConstruction514Names.UnadmittedChild);
            var fork = parent.Fork517(this);
            // Parent's complete input lease already owns every binary backing.
            // These are physical guest roots, not repeated recursive leases.
            foreach (Term child in children)
                values.Add(Reserve(), child);
            constructionCustody514 = fork;

            return fork;
        }
    }
}
