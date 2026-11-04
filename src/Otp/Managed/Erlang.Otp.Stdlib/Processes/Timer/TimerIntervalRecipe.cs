namespace Erlang;

// Rebinding owns the destination environment. Never retain source terms in a
// transferred delegate: interval actions can outlive the creating server turn.
internal sealed record TimerIntervalRecipe(RuntimeFunctionEntries Owner) : IRuntimeStoredFunctionRecipe
{
    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
    {
        var identity = new LocalFunctionIdentity(
            Term.A(TimerLibrary.Name),
            Term.A(TimerClosureNames.Interval),
            TimerClosureLayout.IntervalIndex,
            TimerClosureLayout.OldUnique,
            new BitString(new byte[TimerClosureLayout.IdentityBytes])
        );
        var function = new FunctionTerm(
            identity,
            TimerClosureLayout.IntervalArity,
            environment,
            (context, _) => TimerLibrary.RunInterval((ProcessContext)context, environment)
        );
        Owner.RegisterStoredSource(function, this);

        return function;
    }
}
