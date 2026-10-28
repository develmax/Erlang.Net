namespace Erlang;

internal sealed class ProcessTermConstruction514
{
    private readonly ProcessTermFrame506 frame;
    private readonly HashSet<Term> admitted = new(ReferenceEqualityComparer.Instance);

    internal ProcessTermConstruction514(ProcessTermFrame506 frame, Term source)
    {
        this.frame = frame;
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

    internal bool Admit(Term value) => admitted.Add(value);

    internal void Forget(Term value) => admitted.Remove(value);

    internal void Publish(Term value) => frame.PublishConstructedNode511(value, this);
}
