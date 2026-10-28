namespace Erlang.Compiler;

internal static class SourceContinuationPayload506
{
    internal static Term[] Arguments(IReadOnlyList<Term> arguments, Func<Term, Term> publish)
        => ((TupleTerm)publish(Term.Tuple(arguments.ToArray()))).Items.ToArray();

    internal static (SourceInvocationTarget Target, Term[] Arguments) Target(
        SourceInvocationTarget target,
        IReadOnlyList<Term> arguments,
        Func<Term, Term> publish
    )
    {
        KeyValuePair<string, Term>[] environment = target.Environment?.ToArray() ?? [];
        KeyValuePair<string, Term>[] constraints = target.Environment is CompiledBindingScope scope ? scope.Constraints.ToArray() : [];
        var packed = (TupleTerm)publish(Term.Tuple(
            Term.Tuple(arguments.ToArray()),
            Term.Tuple(environment.Select(pair => pair.Value).ToArray()),
            Term.Tuple(constraints.Select(pair => pair.Value).ToArray())
        ));
        var copied = environment.Select((pair, index) => new KeyValuePair<string, Term>(
            pair.Key, ((TupleTerm)packed.Items[1]).Items[index]
        )).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        Dictionary<string, Term>? bindings = target.Environment is null ? null
            : target.Environment is CompiledBindingScope
                ? new CompiledBindingScope(
                    copied,
                    constraints.Select((pair, index) => new KeyValuePair<string, Term>(
                    pair.Key, ((TupleTerm)packed.Items[2]).Items[index]
                )).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                )
                : copied;

        return (target with { Environment = bindings }, ((TupleTerm)packed.Items[0]).Items.ToArray());
    }
}
