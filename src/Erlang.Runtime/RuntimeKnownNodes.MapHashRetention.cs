namespace Erlang;

internal sealed partial class RuntimeKnownNodes
{
    internal void RetainMapHashIdentifierIdentity(Term value, LocalNodeSnapshot snapshot)
    {
        bool external = value switch
        {
            Pid pid => pid.LocalIdentity is null,
            PortTerm port => port.LocalIdentity is null,
            ReferenceTerm reference => reference.LocalIdentity is null,
            _ => false
        };
        if (!external)
            return;

        lock (gate)
        {
            if (observedIdentitiesClosed || observed.TryGetValue(value, out _))
                return;
        }

        // Runtime ownership resolution must never run under the KnownNodes gate.
        object? retention = resolveDistributionEntry(Term.A(snapshot.Node));
        lock (gate)
        {
            if (observedIdentitiesClosed)
                return;

            ObjectDisposedException.ThrowIf(retention is null, this);
            if (!observed.TryGetValue(value, out _))
                observed.Add(value, retention!);
        }
    }
}
