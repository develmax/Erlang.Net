using static Erlang.Tests.SpawnWorker546Names;

namespace Erlang.Tests;

internal sealed class SpawnWorker546WitnessLaw
{
    private readonly Dictionary<Term, Term> roles;

    private SpawnWorker546WitnessLaw(
        Pid expectedCaller,
        Pid actualCaller,
        Pid expectedWorker,
        Pid actualWorker
    )
    {
        roles = new()
        {
            [expectedCaller] = actualCaller,
            [expectedWorker] = actualWorker
        };
    }

    internal static SpawnWorker546WitnessLaw Bind(
        Term expectedWitness,
        Term actualWitness,
        Term expectedProtocol,
        Term actualProtocol,
        Pid actualController
    )
    {
        var expected = Validate(expectedWitness, expectedProtocol);
        var actual = Validate(actualWitness, actualProtocol);
        Require(actual.Caller.Equals(actualController));

        return new(
            expected.Caller,
            actual.Caller,
            expected.Worker,
            actual.Worker
        );
    }

    // False means there is no authenticated role; caller retains strict equality.
    internal bool MatchKnownPid(Term expected, Term actual)
    {
        if (!roles.TryGetValue(expected, out Term? selected))
        {
            Require(!roles.Values.Any(role => role.Equals(actual)));

            return false;
        }
        Require(actual is Pid && selected.Equals(actual));

        return true;
    }

    private static (Pid Caller, Pid Worker) Validate(Term witness, Term protocol)
    {
        Require(witness is MapTerm);
        var map = (MapTerm)witness;
        Require(map.Entries.Count == Fields.Length
            && map.Entries.All(entry => entry.Key is Atom atom && Fields.Contains(atom.Name, StringComparer.Ordinal)));

        Term Read(string key) => map.Get(Term.A(key));
        Require(Read(Subject).Equals(Term.A(SpawnResource541Names.Subject)));
        Require(Read(Caller) is Pid && Read(Worker) is Pid);
        var caller = (Pid)Read(Caller);
        var worker = (Pid)Read(Worker);
        Require(!caller.Equals(worker) && caller.Node == worker.Node && caller.Creation == worker.Creation);
        Require(Read(SpawnWorker546Names.Monitor) is ReferenceTerm && Read(Correlation) is ReferenceTerm);
        var monitor = (ReferenceTerm)Read(SpawnWorker546Names.Monitor);
        var correlation = (ReferenceTerm)Read(Correlation);
        Require(!monitor.Equals(correlation)
            && monitor.Node == caller.Node && correlation.Node == caller.Node
            && monitor.Creation == caller.Creation && correlation.Creation == caller.Creation);
        Require(Read(Alive).Equals(Term.A(ErlangBooleanAtoms.False))
            && Read(Down).Equals(Term.A(Normal)));
        Require(protocol is TupleTerm { Items: [TupleTerm { Items: [Atom { Name: Returned }, _] }, Atom { Name: Normal }] });

        return (caller, worker);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidDataException(Failure);
    }
}
