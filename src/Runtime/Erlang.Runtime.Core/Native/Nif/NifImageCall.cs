namespace Erlang;

internal sealed class NifImageCall : IDisposable
{
    private readonly NifImageBindings owner;
    private readonly ErlangFunction body;
    private readonly int arity;
    private int state;

    internal bool GuestNative535 { get; }

    internal bool CanRetainGuestResult535(ModuleCodeImage image)
        => GuestNative535 && ReferenceEquals(owner.Image, image)
            && Volatile.Read(ref state) == NifImageCallStates.Running;

    internal NifImageCall(NifImageBindings owner, ErlangFunction body, int arity)
        : this(
            owner,
            body,
            arity,
            false
        )
    {
    }

    internal NifImageCall(
        NifImageBindings owner,
        ErlangFunction body,
        int arity,
        bool guestNative
    )
    {
        this.owner = owner;
        this.body = body;
        this.arity = arity;
        GuestNative535 = guestNative;
    }

    internal async ValueTask<Term> InvokeAsync(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count != arity)
            throw new ArgumentException(NifImageBindingNames.InvalidFunction, nameof(arguments));
        if (Interlocked.CompareExchange(ref state, NifImageCallStates.Running, NifImageCallStates.Reserved) != NifImageCallStates.Reserved)
            throw new ObjectDisposedException(nameof(NifImageCall));
        NifImagePhysicalScope521 physical;
        try
        {
            // Both delegates and the scope are allocated before dispatch/queueing.
            physical = new NifImagePhysicalScope521(ReleasePhysical521, RetainPhysicalFailure521);
        }
        catch
        {
            Interlocked.Exchange(ref state, NifImageCallStates.Released);
            owner.ReleaseCall();
            throw;
        }
        using (physical)
        {
            try
            {
                return await body(context, arguments).ConfigureAwait(false);
            }
            finally
            {
                physical.BodyExited521();
            }
        }
    }

    private void ReleasePhysical521(Exception? failure)
    {
        // ReleaseCall may fail closed on bookkeeping OOM; change state only
        // after successful release, so Dispose cannot release a retained lease.
        owner.ReleaseCall(failure);
        Interlocked.Exchange(ref state, NifImageCallStates.Released);
    }

    private void RetainPhysicalFailure521(Exception failure)
        => owner.RecordPhysicalRetention521(failure);

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref state, NifImageCallStates.Released, NifImageCallStates.Reserved) == NifImageCallStates.Reserved)
            owner.ReleaseCall();
    }
}
