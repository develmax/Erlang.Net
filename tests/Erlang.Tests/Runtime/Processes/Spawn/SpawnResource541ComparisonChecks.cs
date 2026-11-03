namespace Erlang.Tests;

internal static class SpawnResource541ComparisonChecks
{
    internal const string TestName = "runtime/spawn-resource541/whole-protocol-reference-law-negative-controls";

    internal static Task Run()
    {
        Term Reply(ReferenceTerm reference) => Term.Tuple(
            Term.A(SpawnResource541Names.Reply),
            reference,
            Term.A("error"),
            Term.A("badopt")
        );
        var first = new ReferenceTerm("nonode@nohost", 1);
        var second = new ReferenceTerm("nonode@nohost", 2);
        var other = new ReferenceTerm("nonode@nohost", 9);
        var comparer = new SpawnResource541Comparison();
        comparer.Compare(Reply(first), Reply(other));
        comparer.Compare(Reply(first), Reply(other));
        ExpectFailure(() => comparer.Compare(Reply(second), Reply(other)));
        ExpectFailure(() => new SpawnResource541Comparison().Compare(first, other));
        ExpectFailure(() => new SpawnResource541Comparison().Compare(
            Term.Tuple(Term.A("error"), Term.I(1)), Term.Tuple(Term.A("error"), Term.I(2))));

        return Task.CompletedTask;
    }

    private static void ExpectFailure(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException(SpawnResource541Names.Failure);
    }
}
