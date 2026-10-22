namespace Erlang;

internal sealed class EtsCompiledRunFailure419(Term list, Term program, Term accumulator) : Exception
{
    internal IReadOnlyList<Term> ActualArguments { get; } = new Term[] { list, program, accumulator };
}
