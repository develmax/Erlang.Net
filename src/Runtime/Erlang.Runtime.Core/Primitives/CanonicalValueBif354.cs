namespace Erlang;

internal sealed class CanonicalValueBif354
{
    private readonly Func<ProcessContext, IReadOnlyList<Term>, Term> evaluate;

    internal CanonicalValueBif354(Func<ProcessContext, IReadOnlyList<Term>, Term> evaluate)
    {
        this.evaluate = evaluate;
        Body = Invoke;
    }

    internal ErlangFunction Body { get; }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term value = evaluate(context, arguments);
        OwnedTermInvocation506? invocation = context.CurrentOwned506;
        if (invocation?.BindingKind507 == OwnedBindingKind507.LocalValueBif354)
        {
            if (invocation.IsDisposed || !invocation.CanBorrow507 || !invocation.OwnsContext507(context)
                || !RuntimeActorExecutionTurn.IsOwnedBy(context))
                throw new InvalidOperationException(OwnedBoundary507Names.PublicationChanged);

            // Return actual input aliases unchanged; publish fresh scalar results
            // into the real frame before either registry/native bridge returns.
            value = invocation.Frame.AdoptGuestInto507(invocation.ResultSlot507, value);
        }

        return ValueTask.FromResult(value);
    }
}
