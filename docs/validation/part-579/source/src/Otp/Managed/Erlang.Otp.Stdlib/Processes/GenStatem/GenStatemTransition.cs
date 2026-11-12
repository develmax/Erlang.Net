namespace Erlang;

internal sealed class GenStatemTransition(Term state, Term data)
{
    internal Term State { get; set; } = state;
    internal Term Data { get; set; } = data;
    internal bool Hibernate { get; set; }
    internal bool Postpone { get; set; }
    internal List<Term> NextEvents { get; } = [];
    internal List<TupleTerm> Timeouts { get; } = [];
}
