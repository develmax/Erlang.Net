namespace Erlang.Otp;

internal static class Rpc
{
    internal static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 4, 5 })
            registry.Register(
                ErpcNames.Rpc,
                ErpcNames.Call,
                arity,
                Call
            );
    }

    private static async ValueTask<Term> Call(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        try
        {
            Term value = await Erpc.Call(context, arguments);

            return IsExit(value) ? Term.Tuple(ErpcAtoms.BadRpc, value) : value;
        }
        catch (ErlangException error)
        {
            Term reason = error.Reason;
            if (error.ExceptionClass == ErlangExceptionClasses.Throw)
                return IsExit(reason) ? Term.Tuple(ErpcAtoms.BadRpc, reason) : reason;
            if (error.ExceptionClass == ErlangExceptionClasses.Exit)
            {
                if (reason is TupleTerm { Items: [Atom { Name: ErpcNames.Exception or ErpcNames.Signal }, var inner] })
                    return Term.Tuple(ErpcAtoms.BadRpc, Term.Tuple(ErpcAtoms.ExitTag, inner));
                throw;
            }
            if (reason is TupleTerm { Items: [Atom { Name: ErpcNames.Exception }, var failure, var stack] })
                return Term.Tuple(ErpcAtoms.BadRpc, Term.Tuple(ErpcAtoms.ExitTag, Term.Tuple(failure, stack)));
            if (reason is TupleTerm { Items: [Atom { Name: ErpcNames.Module }, var remote] })
            {
                if (remote.Equals(ErpcAtoms.BadArgument))
                    throw new ErlangException(ErpcAtoms.BadArgument);
                if (remote.Equals(ErpcAtoms.NoConnection))
                    return Term.Tuple(ErpcAtoms.BadRpc, ErpcAtoms.NodeDown);
                if (remote.Equals(ErpcAtoms.Timeout) || remote.Equals(ErpcAtoms.NotSupported))
                    return Term.Tuple(ErpcAtoms.BadRpc, remote);

                return Term.Tuple(ErpcAtoms.BadRpc, Term.Tuple(ErpcAtoms.ExitTag, remote));
            }
            throw;
        }
    }

    private static bool IsExit(Term value) => value is TupleTerm { Items: [Atom { Name: ErpcNames.ExitTag }, _] };
}
