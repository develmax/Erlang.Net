namespace Erlang;

internal sealed partial class NativeDriverPortRegistry422
{
    private async Task<Term> ReplyInternalPort428(
        Pid requester,
        PortTerm source,
        ReferenceTerm reference,
        Task<Term> operation,
        bool call
    )
    {
        Term response;
        try
        {
            Term value = await operation.ConfigureAwait(false);
            response = call ? Term.Tuple(Term.A(InternalPortLoaderNames428.Ok), value) : value;
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && !error.ExplicitStackTrace && error.Reason is Atom { Name: ErlangErrorReasons.BadArgument })
        {
            response = error.Reason;
        }
        // This continuation itself is row.Commands and Track(operation), so
        // shutdown cannot declare native/port settlement before reply delivery.
        // Only immutable guest identifiers are retained; no source context is stored.
        runtime.SendSystem(requester, Term.Tuple(reference, response), source);

        return response;
    }
}
