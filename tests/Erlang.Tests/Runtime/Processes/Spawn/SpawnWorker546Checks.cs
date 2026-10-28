using static Erlang.Tests.SpawnWorker546Names;

namespace Erlang.Tests;

internal static class SpawnWorker546Checks
{
    internal static Task Run()
    {
        var originalCaller = new Pid("nonode@nohost", 10);
        var originalWorker = new Pid("nonode@nohost", 87);
        var actualCaller = new Pid("nonode@nohost", 2);
        var actualWorker = new Pid("nonode@nohost", 9);
        Term protocol = Term.Tuple(Term.Tuple(Term.A(Returned), Nil.Value), Term.A(Normal));
        MapTerm original = Witness(originalCaller, originalWorker, 100);
        MapTerm actual = Witness(actualCaller, actualWorker, 200);
        var law = SpawnWorker546WitnessLaw.Bind(
            original,
            actual,
            protocol,
            protocol,
            actualCaller
        );
        var comparison = new SpawnResource541Comparison(law);
        comparison.Compare(originalCaller, actualCaller);
        comparison.Compare(originalWorker, actualWorker);
        comparison.Compare(Term.Tuple(originalWorker, originalWorker), Term.Tuple(actualWorker, actualWorker));
        Reject(() => comparison.Compare(originalWorker, actualCaller));
        Reject(() => comparison.Compare(new Pid("nonode@nohost", 55), actualWorker));
        Reject(() => new SpawnResource541Comparison().Compare(originalWorker, actualWorker));
        foreach (MapTerm invalid in new[]
        {
            Change(actual, Subject, Term.A("wrong_subject")),
            Change(actual, Alive, Term.A(ErlangBooleanAtoms.True)),
            Change(actual, Down, Term.A("error")),
            Change(actual, Worker, actualCaller),
            Change(actual, SpawnWorker546Names.Monitor, Term.I(1)),
            Change(actual, Correlation, actual.Get(Term.A(SpawnWorker546Names.Monitor))),
            Change(actual, Correlation, new ReferenceTerm("foreign@host", 2)),
            Change(actual, Correlation, new ReferenceTerm("nonode@nohost", 2, 1)),
            new MapTerm(actual.Entries.Where(entry => !entry.Key.Equals(Term.A(Caller)))),
            new MapTerm(actual.Entries.Select(entry => entry.Key.Equals(Term.A(Worker))
                ? new KeyValuePair<Term, Term>(Term.A("wrong_worker_label"), entry.Value) : entry))
        })
            Reject(() => SpawnWorker546WitnessLaw.Bind(
                original,
                invalid,
                protocol,
                protocol,
                actualCaller
            ));
        MapTerm swapped = Change(Change(actual, Caller, actualWorker), Worker, actualCaller);
        Reject(() => SpawnWorker546WitnessLaw.Bind(
            original,
            swapped,
            protocol,
            protocol,
            actualCaller
        ));
        Reject(() => SpawnWorker546WitnessLaw.Bind(
            original,
            actual,
            protocol,
            protocol,
            actualWorker
        ));
        Reject(() => SpawnWorker546WitnessLaw.Bind(
            original,
            actual,
            protocol,
            Term.Tuple(Term.Tuple(Term.A(Returned), Nil.Value), Term.A("error")),
            actualCaller
        ));

        // Correct external code identity is retained. Name/module/arity changes
        // cannot be accepted by the authenticated PID law.
        FunctionTerm Export(string name) => new(new ExternalFunctionIdentity(Term.A("fixture"), Term.A(name), 0), Refuse);
        comparison.Compare(Export("target"), Export("target"));
        Reject(() => comparison.Compare(Export("target"), Export("other")));
        var local = new FunctionTerm(
            new LocalFunctionIdentity(
                Term.A("fixture"),
                null,
                0,
                0,
                new BitString(new byte[16])
            ),
            0,
            [originalWorker],
            Refuse
        );
        Reject(() => SpawnWorker546FunctionScope.RequireExportFunctionsOnly(local));
        Reject(() => SpawnWorker546FunctionScope.RequireExportFunctionsOnly(Term.Tuple(local)));
        Reject(() => comparison.Compare(local, local));

        return Task.CompletedTask;
    }

    private static MapTerm Witness(Pid caller, Pid worker, ulong referenceBase)
        => new(new Dictionary<Term, Term>
        {
            [Term.A(Subject)] = Term.A(SpawnResource541Names.Subject),
            [Term.A(Caller)] = caller,
            [Term.A(Worker)] = worker,
            [Term.A(SpawnWorker546Names.Monitor)] = new ReferenceTerm(caller.Node, referenceBase),
            [Term.A(Correlation)] = new ReferenceTerm(caller.Node, referenceBase + 1),
            [Term.A(Alive)] = Term.A(ErlangBooleanAtoms.False),
            [Term.A(Down)] = Term.A(Normal)
        });

    private static MapTerm Change(MapTerm source, string field, Term value)
        => new(source.Entries.Select(entry => entry.Key.Equals(Term.A(field))
            ? new KeyValuePair<Term, Term>(entry.Key, value) : entry));

    private static ValueTask<Term> Refuse(ITermExecutionContext context, IReadOnlyList<Term> arguments)
        => throw new InvalidOperationException(Failure);

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException(Failure);
    }
}
