namespace Erlang;

public sealed partial class ProcessRuntime
{
    public RuntimeExternalTermReservation ReserveExternalTerm(Term term) => ReserveTerm591(term, hostAtomics586: true);

    // VM-owned store roots must retain deterministic guest custody, not rely
    // on a CLR host wrapper or public external-reservation lifetime.
    internal RuntimeExternalTermReservation ReserveStoredTerm591(Term term) => ReserveTerm591(term, hostAtomics586: false);

    private RuntimeExternalTermReservation ReserveTerm591(Term term, bool hostAtomics586)
    {
        ArgumentNullException.ThrowIfNull(term);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, nameof(ProcessRuntime));
            RequireExternalTermDomain(term);
            KnownNodes.Observe(term);

            return new(this, term, hostAtomics586);
        }
    }

    internal void ObserveIncomingTerm(Term term)
    {
        if (term is Atom or Pid or ReferenceTerm or PortTerm or Cons or TupleTerm or MapTerm or NativeRecordTerm or HamtBitmapTerm429 ||
            term is FunctionTerm)
            KnownNodes.Observe(term);
    }

    private void RequireExternalTermDomain(Term root)
    {
        var pending = new Stack<Term>();
        var visited = new HashSet<Term>(ReferenceEqualityComparer.Instance);

        void Enqueue(Term value)
        {
            if (visited.Add(value))
                pending.Push(value);
        }
        Enqueue(root);
        while (pending.TryPop(out var term))
        {
            LocalNodeIdentity? owner = term switch
            {
                Pid pid => pid.LocalIdentity,
                ReferenceTerm reference => reference.LocalIdentity,
                PortTerm port => port.LocalIdentity,
                _ => null
            };
            if (owner is not null && !ReferenceEquals(owner, LocalIdentity))
                throw new ArgumentException(RuntimeExternalTermDiagnostics.ForeignIdentity, nameof(root));
            switch (term)
            {
                case Cons list:
                    Enqueue(list.Head);
                    Enqueue(list.Tail);
                    break;
                case TupleTerm tuple:
                    foreach (Term item in tuple.Items)
                        Enqueue(item);
                    break;
                case HamtBitmapTerm429 node:
                    if (hamtIndex429 is null || !ReferenceEquals(hamtIndex429.Domain, node.Domain))
                        throw new ArgumentException(HamtIndexNames429.Foreign, nameof(root));
                    foreach (Term child in node.Children) Enqueue(child);
                    break;
                case MapTerm map:
                    foreach (var entry in map.Entries)
                    {
                        Enqueue(entry.Key);
                        Enqueue(entry.Value);
                    }
                    break;
                case NativeRecordTerm record:
                    foreach (Term item in record.Values)
                        Enqueue(item);
                    break;
                case FunctionTerm function:
                    foreach (Term item in function.Environment)
                        Enqueue(item);
                    break;
            }
        }
    }
}
