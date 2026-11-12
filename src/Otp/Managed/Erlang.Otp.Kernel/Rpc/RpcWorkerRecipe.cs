namespace Erlang.Otp;

internal sealed record RpcWorkerRecipe(RuntimeFunctionEntries Owner, RpcWorkerKind Kind) : IRuntimeStoredFunctionRecipe
{
    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
    {
        var identity = new LocalFunctionIdentity(
            Term.A(RpcLibrary.Name),
            Term.A(RpcWorkerIdentity.Functions[(int)Kind]),
            (int)Kind,
            RpcWorkerIdentity.OldUnique,
            new BitString(new byte[RpcWorkerIdentity.IdentityBytes])
        );
        var function = new FunctionTerm(
            identity,
            0,
            environment,
            (context, _) => RpcLibrary.RunWorker((ProcessContext)context, Kind, environment)
        );
        Owner.RegisterStoredSource(function, this);

        return function;
    }
}
