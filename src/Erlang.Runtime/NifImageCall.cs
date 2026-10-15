namespace Erlang;

internal sealed class NifImageCall : IDisposable
{
    private readonly NifImageBindings owner;
    private readonly ErlangFunction body;
    private readonly int arity;
    private int state;

    internal NifImageCall(NifImageBindings owner, ErlangFunction body, int arity)
    {
        this.owner = owner;
        this.body = body;
        this.arity = arity;
    }

    internal async ValueTask<Term> InvokeAsync(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count != arity)
            throw new ArgumentException(NifImageBindingNames.InvalidFunction, nameof(arguments));
        if (Interlocked.CompareExchange(ref state, NifImageCallStates.Running, NifImageCallStates.Reserved) != NifImageCallStates.Reserved)
            throw new ObjectDisposedException(nameof(NifImageCall));
        try
        {
            return await body(context, arguments).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref state, NifImageCallStates.Released);
            owner.ReleaseCall();
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref state, NifImageCallStates.Released, NifImageCallStates.Reserved) == NifImageCallStates.Reserved)
            owner.ReleaseCall();
    }
}
