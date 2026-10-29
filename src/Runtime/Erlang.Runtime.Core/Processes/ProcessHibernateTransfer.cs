namespace Erlang;

public sealed class ProcessHibernateTransfer : Exception, IOwnedControlTransfer506
{
    public string Module { get; }
    public string Function { get; }
    public IReadOnlyList<Term> Arguments { get; private set; }
    internal bool GuestArguments641 { get; }
    internal RuntimeProcessPayload641? Origin641 { get; private set; }
    internal Exception? Teardown641 { get; set; }

    internal ProcessHibernateTransfer(
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        bool guestArguments641 = false
    )
    {
        Module = module;
        Function = function;
        Arguments = Array.AsReadOnly(arguments.ToArray());
        GuestArguments641 = guestArguments641;
    }

    Exception IOwnedControlTransfer506.TransferTerms506(Func<Term, Term> publish)
    {
        // Existing opaque/.NET Host transport stays unchanged.
        if (!GuestArguments641) return this;
        var complete = (TupleTerm)publish(Term.Tuple(Arguments.ToArray()));

        return new ProcessHibernateTransfer(
            Module,
            Function,
            complete.Items,
            true
        );
    }

    internal void SetVmArguments641(IReadOnlyList<Term> stable, RuntimeProcessPayload641 origin)
    {
        Arguments = stable; // actual complete detached tuple, no post-capture allocation
        Origin641 = origin;
    }
}
