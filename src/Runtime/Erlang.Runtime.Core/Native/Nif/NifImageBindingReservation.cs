namespace Erlang;

internal sealed class NifImageBindingReservation : IDisposable
{
    private readonly NifImageBindings owner;
    internal readonly HashSet<(string Name, int Arity)> Eligible;
    private readonly TaskCompletionSource settled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task Settled => settled.Task;

    internal void Complete() => settled.TrySetResult();

    internal NifImageBindingReservation(NifImageBindings owner, ModuleCodeDescriptor descriptor)
    {
        this.owner = owner;
        Eligible = descriptor.Functions.ToHashSet();
        if (descriptor.DeclaredNifs is { } declarations)
            Eligible.IntersectWith(declarations);
    }

    internal void Publish(
        IEnumerable<KeyValuePair<(string Name, int Arity), ErlangFunction>> bindings,
        IAsyncDisposable nativeOwner
    ) => owner.Publish(this, bindings, nativeOwner);

    internal NifPreparedImageBindings544 Prepare(
        IEnumerable<KeyValuePair<(string Name, int Arity), ErlangFunction>> bindings
    ) => owner.Prepare(this, bindings);

    internal void PublishPrepared(
        NifPreparedImageBindings544 prepared,
        IAsyncDisposable nativeOwner
    ) => owner.PublishPrepared(this, prepared, nativeOwner);

    // Failed publication does not transfer nativeOwner. Release that caller-owned
    // resource before disposing the reservation, so retirement observes real cleanup.
    public void Dispose() => owner.Abort(this);
}
