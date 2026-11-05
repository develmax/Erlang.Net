namespace Erlang;

internal sealed record TimerTerminationRecipe(RuntimeFunctionEntries Owner) : IRuntimeStoredFunctionRecipe
{
    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
    {
        var identity = new LocalFunctionIdentity(
            Term.A(TimerLibrary.Name),
            Term.A(TimerClosureNames.Termination),
            TimerClosureLayout.TerminationIndex,
            TimerClosureLayout.OldUnique,
            new BitString(new byte[TimerClosureLayout.IdentityBytes])
        );
        var function = new FunctionTerm(
            identity,
            TimerClosureLayout.TerminationArity,
            environment,
            (context, arguments) => TimerLibrary.TerminateEntry((ProcessContext)context, arguments)
        );
        Owner.RegisterStoredSource(function, this);

        return function;
    }
}
