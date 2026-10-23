namespace Erlang;

internal sealed class NifNativeAllocationDomainChange : IDisposable
{
    private NifNativeAllocationDomain? domain;
    private readonly long membership;
    private readonly NifNativeAllocationSnapshot nextOwner;
    private readonly NifNativeAllocationSnapshot nextAggregate;

    internal NifNativeAllocationDomainChange(
        NifNativeAllocationDomain domain,
        long membership,
        NifNativeAllocationSnapshot nextOwner,
        NifNativeAllocationSnapshot nextAggregate
    )
    {
        this.domain = domain;
        this.membership = membership;
        this.nextOwner = nextOwner;
        this.nextAggregate = nextAggregate;
    }

    internal void Commit() => domain!.CommitChange(membership, nextOwner, nextAggregate);

    public void Dispose() => Interlocked.Exchange(ref domain, null)?.EndChange();
}
