using static Erlang.Tests.SpawnResource541Names;

namespace Erlang.Tests;

// Compare the complete decoded protocol without changing either raw ETF.
// Default behavior retains only the spawn_reply request-reference law.
// The optional law prebinds caller/worker PIDs authenticated by the exact
// compiled-worker source and inventory witness. No first-PID matching occurs.
internal sealed class SpawnResource541Comparison
{
    private readonly SpawnWorker546WitnessLaw? workerRoles;
    private readonly Dictionary<Term, Term> forward = new();
    private readonly Dictionary<Term, Term> reverse = new();

    internal SpawnResource541Comparison(SpawnWorker546WitnessLaw? workerRoles = null)
    {
        this.workerRoles = workerRoles;
    }

    internal void Compare(
        Term expected,
        Term actual,
        string path = "$",
        bool requestReference = false
    )
    {
        if (workerRoles is not null && expected is Pid)
        {
            if (workerRoles.MatchKnownPid(expected, actual))
                return;
        }
        if (workerRoles is not null && expected is FunctionTerm expectedFunction && actual is FunctionTerm actualFunction)
        {
            CompareFunction(expectedFunction, actualFunction, path);

            return;
        }
        if (requestReference)
        {
            if (expected is not ReferenceTerm left || actual is not ReferenceTerm right
                || left.Node != right.Node)
                Fail(path);
            if (forward.TryGetValue(expected, out Term? mapped))
            {
                if (!mapped.Equals(actual))
                    Fail(path);
            }
            else
            {
                if (reverse.ContainsKey(actual))
                    Fail(path);
                forward.Add(expected, actual);
                reverse.Add(actual, expected);
            }

            return;
        }
        if (expected is TupleTerm first && actual is TupleTerm second)
        {
            if (first.Items.Count != second.Items.Count)
                Fail(path);
            bool reply = first.Items.Count == 4 && first.Items[0] is Atom { Name: Reply };
            for (int index = 0; index < first.Items.Count; index++)
                Compare(
                    first.Items[index],
                    second.Items[index],
                    path + "." + index,
                    reply && index == 1
                );

            return;
        }
        if (expected is Cons one && actual is Cons two)
        {
            Compare(one.Head, two.Head, path + ".head");
            Compare(one.Tail, two.Tail, path + ".tail");

            return;
        }
        if (expected is MapTerm map && actual is MapTerm other)
        {
            if (map.Entries.Count != other.Entries.Count)
                Fail(path);
            foreach (var entry in map.Entries)
            {
                if (!other.TryGet(entry.Key, out Term? value))
                    Fail(path);
                Compare(entry.Value, value!, path + ".map[" + entry.Key + "]");
            }

            return;
        }
        if (!expected.Equals(actual))
            Fail(path);
    }

    private void CompareFunction(FunctionTerm expected, FunctionTerm actual, string path)
    {
        if (expected.Arity != actual.Arity || expected.Environment.Count != actual.Environment.Count)
            Fail(path);
        if (expected.ExternalIdentity is { } external)
        {
            if (!external.Equals(actual.ExternalIdentity) || actual.LocalIdentity is not null)
                Fail(path);
        }
        else
        {
            // Scoped315 proof admits EXPORT_FUN only. NEW_FUN's creator PID
            // is not retained by the existing decoder, so never accept that
            // partial representation under this witness-aware proof.
            Fail(path);
        }
        for (int index = 0; index < expected.Environment.Count; index++)
            Compare(expected.Environment[index], actual.Environment[index], path + ".env[" + index + "]");
    }

    private static void Fail(string path) => throw new InvalidDataException(Failure + " Path=" + path);
}
