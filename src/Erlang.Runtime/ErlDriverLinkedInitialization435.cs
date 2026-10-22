namespace Erlang;

internal sealed class ErlDriverLinkedInitialization435 : IDisposable
{
    private ErlDriverLinkedServices420? owner;
    private readonly HashSet<ErlDriverLinkedRow420> before;
    private bool committed;

    internal ErlDriverLinkedInitialization435(ErlDriverLinkedServices420 owner, IEnumerable<ErlDriverLinkedRow420> existing)
    {
        this.owner = owner;
        before = new(existing, ReferenceEqualityComparer.Instance);
    }

    internal void Commit() => committed = true;

    internal void Fail438() => owner?.FailLinkedVisibility438();

    public void Dispose()
    {
        ErlDriverLinkedServices420? held = owner;
        if (held is null) return;
        owner = null;
        held.EndInitialization435(before, committed);
    }
}
