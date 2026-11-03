namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    // A pre-created construction capability owns every arbitrary input leaf.
    // These operations allocate only real wrapper cells; they never import,
    // export, resolve maps/functions or acquire foreign/native ownership.
    internal Term CreateSequenceTraceTuple(ProcessTermConstruction514 custody, Term[] children)
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            foreach (Term child in children)
                if (!custody.Contains(child)) throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
            Term value = owner.CreateTuple(children);
            custody.Publish(value);

            return value;
        }
    }

    internal Term CreateSequenceTraceCons(ProcessTermConstruction514 custody, Term head, Term tail)
    {
        lock (owner.Gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!custody.Contains(head) || !custody.Contains(tail))
                throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
            Term value = owner.CreateCons(head, tail);
            custody.Publish(value);

            return value;
        }
    }
}
