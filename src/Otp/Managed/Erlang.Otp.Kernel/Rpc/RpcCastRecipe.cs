namespace Erlang.Otp;

internal sealed record RpcCastRecipe(RuntimeFunctionEntries Owner) : IRuntimeStoredFunctionRecipe
{
    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
    {
        var identity = new LocalFunctionIdentity(
            Term.A(RpcLibrary.Name),
            Term.A(RpcWorkerIdentity.BroadcastFunction),
            RpcWorkerIdentity.BroadcastIndex,
            RpcWorkerIdentity.OldUnique,
            new BitString(new byte[RpcWorkerIdentity.IdentityBytes])
        );
        var function = new FunctionTerm(
            identity,
            1,
            environment,
            (context, arguments) => RpcLibrary.Invoke((ProcessContext)context, RpcFunctionNames.Cast,
                [arguments[0], environment[0], environment[1], environment[2]])
        );
        Owner.RegisterStoredSource(function, this);

        return function;
    }
}
