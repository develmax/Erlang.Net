namespace Erlang;

internal sealed partial class ProcessTermConstruction514
{
    private readonly ProcessTermFrame506 frame;
    private readonly HashSet<Term> admitted;

    internal ProcessTermConstruction514(ProcessTermFrame506 frame, Term source)
    {
        this.frame = frame;
        admitted = new(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Term>();
        pending.Push(source);
        while (pending.TryPop(out Term? current))
        {
            if (!admitted.Add(current))
                continue;
            foreach (Term child in NativeMapOwnedRoots510.Children(current))
                pending.Push(child);
        }
    }

    internal bool Contains(Term value) => admitted.Contains(value);

    internal bool Admit(Term value)
    {
        if (forkAdds517 is { } added)
            added.EnsureCapacity(checked(added.Count + 1));
        if (!admitted.Add(value))
            return false;
        forkAdds517?.Add(value);

        return true;
    }

    internal void Forget(Term value) => admitted.Remove(value);

    internal void Publish(Term value) => frame.PublishConstructedNode511(value, this);
}
