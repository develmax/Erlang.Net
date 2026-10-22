namespace Erlang;

public sealed partial class ProcessRuntime
{
    private int systemRequestWindow432;

    internal Term ReadSystemRequestWindow432(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();

            return Term.I(InitializeSystemRequestWindow432());
        }
    }

    internal Term ExchangeSystemRequestWindow432(ProcessContext context, Term value)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (value is not Integer number || number.Value < 1 || number.Value > ProcessTableParameters.MaximumCapacity)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            int previous = InitializeSystemRequestWindow432();
            systemRequestWindow432 = (int)number.Value;

            return Term.I(previous);
        }
    }

    private int InitializeSystemRequestWindow432()
    {
        if (systemRequestWindow432 == 0)
            systemRequestWindow432 = checked(2 * ActorDispatchCapacity);

        return systemRequestWindow432;
    }
}
