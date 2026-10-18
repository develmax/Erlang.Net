namespace Erlang;

internal sealed partial class RuntimeKnownNodes
{
    private readonly Dictionary<(string Node, uint Creation), RuntimeMapHashEpoch> mapHashEpochs = new();
    private ulong nextMapHashNodeWord;

    internal RuntimeMapHashNodeLease AcquireMapHashNodeWord(Term term, LocalNodeSnapshot snapshot)
    {
        lock (gate)
        {
            // Reuse the actual node observer's weak-term/epoch ownership, including
            // an explicit captured epoch when a foreign mutable identity changed.
            foreach (var key in mapHashEpochs.Keys.ToArray())
                if (mapHashEpochs[key].ActiveScopes == 0 && !MapHashEpochRetained(key.Node, key.Creation))
                    mapHashEpochs.Remove(key);
            if (!nodes.TryGetValue(snapshot.Node, out var references))
                nodes.Add(snapshot.Node, references = new());
            if (!references.Any(reference => reference.TryGetTarget(out var value) && ReferenceEquals(value, term)))
                references.Add(new(term));
            var epoch = (snapshot.Node, snapshot.Creation);
            if (!mapHashEpochs.TryGetValue(epoch, out var entry))
            {
                if (nextMapHashNodeWord > ulong.MaxValue - RuntimeMapHashNames.PointerAlignment)
                    throw new ErlangException(ErlangErrorReasons.SystemLimit);
                nextMapHashNodeWord += RuntimeMapHashNames.PointerAlignment;
                entry = new(nextMapHashNodeWord);
                mapHashEpochs.Add(epoch, entry);
            }
            if (entry.ActiveScopes == int.MaxValue)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            entry.ActiveScopes++;

            return new(entry.Word, () => ReleaseMapHashNode(entry));
        }
    }

    internal void ObserveMapHashEpochTerm(Term term, LocalNodeSnapshot snapshot)
    {
        lock (gate)
        {
            if (!nodes.TryGetValue(snapshot.Node, out var references))
                nodes.Add(snapshot.Node, references = new());
            if (!references.Any(reference => reference.TryGetTarget(out var value) && ReferenceEquals(value, term)))
                references.Add(new(term));
        }
    }

    private void ReleaseMapHashNode(RuntimeMapHashEpoch entry)
    {
        lock (gate)
        {
            if (entry.ActiveScopes <= 0)
                throw new InvalidOperationException(RuntimeMapHashDiagnostics.UnbalancedScope);
            entry.ActiveScopes--;
        }
    }

    private bool MapHashEpochRetained(string node, uint creation)
    {
        if (reservedEpochs.ContainsKey((node, creation)))
            return true;
        if (!nodes.TryGetValue(node, out var references))
            return false;

        return references.Any(reference => reference.TryGetTarget(out var term) && (term switch
        {
            Pid pid => pid.Node == node && pid.Creation == creation,
            PortTerm port => port.Node == node && port.Creation == creation,
            ReferenceTerm value => value.Node == node && value.Creation == creation,
            _ => false
        }));
    }
}
