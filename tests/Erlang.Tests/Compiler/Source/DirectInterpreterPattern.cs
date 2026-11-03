namespace Erlang.Tests;

internal sealed record DirectInterpreterPattern(Action<ProcessContext, Term> Observe) : Pattern
{
    protected override bool MatchCore(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
    {
        Observe(context!, value);
        bindings[DirectInterpreterEntryNames.Bound] = value;

        return true;
    }
}
