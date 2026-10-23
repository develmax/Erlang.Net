namespace Erlang;

internal sealed class PersistentIdentifierRoot416 : IDisposable
{
    private IDisposable? epoch;
    private readonly LocalNodeIdentity? local;
    private readonly RuntimeDistributionEntryIdentity? external;
    private readonly WeakReference<RuntimeKnownNodes> observations;

    private PersistentIdentifierRoot416(LocalNodeIdentity? local, RuntimeDistributionEntryIdentity? external, RuntimeKnownNodes observations)
    {
        this.local = local;
        this.external = external;
        this.observations = new(observations);
    }

    internal static PersistentIdentifierRoot416 Capture(ProcessRuntime owner, Term value)
    {
        // This admission verifies foreign opaque domains; its temporary Term root is not retained.
        using var admission = owner.ReserveExternalTerm(value);
        LocalNodeIdentity? local = value switch
        {
            Pid pid => pid.LocalIdentity,
            PortTerm port => port.LocalIdentity,
            ReferenceTerm reference => reference.LocalIdentity,
            _ => throw new InvalidDataException(PersistentArenaNames416.Invalid)
        };
        string node = value switch
        {
            Pid pid => pid.Node,
            PortTerm port => port.Node,
            ReferenceTerm reference => reference.Node,
            _ => throw new InvalidDataException(PersistentArenaNames416.Invalid)
        };
        var result = new PersistentIdentifierRoot416(local,
            local is null ? owner.GetDistributionEntryIdentity(Term.A(node)) : null, owner.KnownNodes);
        if (local is null)
        {
            uint creation = value switch
            {
                Pid pid => pid.Creation,
                PortTerm port => port.Creation,
                ReferenceTerm reference => reference.Creation,
                _ => throw new InvalidDataException(PersistentArenaNames416.Invalid)
            };
            result.epoch = owner.KnownNodes.ReservePersistentEpoch416(node, creation);
        }

        return result;
    }

    public void Dispose() => Interlocked.Exchange(ref epoch, null)?.Dispose();

    internal Term Materialize(byte[] bytes)
    {
        Term value = PersistentIdentifier416.Decode(bytes, local);
        if (observations.TryGetTarget(out var known)) known.Observe(value);
        // Keep the genuine external ErlNode/DistEntry root alive through ephemeron publication.
        GC.KeepAlive(external);

        return value;
    }
}
