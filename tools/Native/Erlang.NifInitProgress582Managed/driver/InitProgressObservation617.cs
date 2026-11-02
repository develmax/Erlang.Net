using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Erlang;
using static InitProgressNames617;
using static InitProgressAdmission617;

internal static class InitProgressObservation617
{
    internal static Term Expected()
    {
        Term Val(Term term) => Term.Tuple(Term.A(Value), term);

        return Term.Tuple(
            Term.A(Returned),
            Term.Tuple(
                Val(Term.A(Ok)),
                Val(Term.A(Ok)),
                Val(Term.Tuple(Term.I(1), Term.I(0))),
                Val(Term.A(Ok)),
                Val(Term.Tuple(Term.I(2), Term.I(0))),
                Val(Term.Tuple(Term.I(1), Term.I(0))),
                Val(Term.A(Ok)),
                Term.A(Normal),
                Term.I(2)
            )
        );
    }

    internal static Term ExpectedProfile() => Term.Tuple(
        Term.A(ProfileAtom),
        Term.I(2),
        Term.I(2),
        Term.I(1),
        Term.I(2),
        Term.A(NotReady)
    );

    internal static async Task<Term> Outcome(Func<Task<Term>> body)
    {
        try
        {
            return Term.Tuple(Term.A(Value), await body());
        }
        catch (ErlangException error)
        {
            return Term.Tuple(
                Term.A(Raised),
                Term.A(error.ExceptionClass),
                error.Reason,
                error.StackTraceTerm
            );
        }
    }

    internal static async Task<bool> Purge(ProcessRuntime runtime, ProcessContext context, string module)
    {
        MethodInfo method = typeof(ProcessRuntime).GetMethod(
            PurgeMethod,
            Members,
            null,
            [typeof(ProcessContext), typeof(string)],
            null
        )!;

        try
        {
            return await (ValueTask<bool>)method.Invoke(runtime, [context, module])!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    internal static string[] Events(string path, bool complete = false)
    {
        if (!System.IO.File.Exists(path)) return [];
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete
        );
        Require(stream.Length <= MaxEventBytes);
        using var reader = new StreamReader(stream, Encoding.ASCII);
        string text = reader.ReadToEnd();
        Require(!complete || text.Length == 0 || text.EndsWith('\n'));

        // Incomplete native records cannot satisfy a physical retirement witness.
        return text.Split('\n').SkipLast(1).ToArray();
    }

    private static async Task WaitOwners(
        string path,
        int first,
        int second,
        Stopwatch budget
    )
    {
        while (budget.Elapsed < InvocationDeadline)
        {
            string[] events = Events(path);
            int a = events.Count(e => e == Unload1), b = events.Count(e => e == Unload2);
            Require(a <= first && b <= second);
            if (a == first && b == second) return;
            await Task.Delay(Poll);
        }
        throw new TimeoutException(Failure);
    }

    internal static async Task<Term> Cleanup(
        ProcessRuntime runtime,
        ProcessContext context,
        string log,
        Stopwatch budget,
        List<string> errors
    )
    {
        int accepted = -1;
        try
        {
            accepted = Events(log).Count(e => e == Load);
            Require(accepted is >= 0 and <= SubjectOwnerCount);
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
            accepted = -1;
        }
        int first = accepted > 0 ? 1 : 0, second = accepted > 1 ? 1 : 0;
        var results = new List<Term>();
        int initialErrors = errors.Count;

        async Task Attempt(Func<Task<Term>> action)
        {
            try
            {
                results.Add(await Outcome(action));
            }
            catch (Exception error)
            {
                errors.Add(error.ToString());
                results.Add(Term.A(HostFailure));
            }
        }
        await Attempt(async () =>
        {
            bool purged = await Purge(runtime, context, Subject);
            if (accepted > 0) await WaitOwners(
                log,
                first,
                0,
                budget
            );

            return Term.A(purged ? True : False);
        });
        await Attempt(async () =>
        {
            Term deleted = runtime.Modules.DeleteModule(Subject);
            bool purged = await Purge(runtime, context, Subject);
            if (accepted >= 0) await WaitOwners(
                log,
                first,
                second,
                budget
            );

            return Term.Tuple(deleted, Term.A(purged ? True : False));
        });
        foreach (string module in new[] { AckModule, Observer })
            await Attempt(async () => Term.Tuple(runtime.Modules.DeleteModule(module), Term.A(await Purge(runtime, context, module) ? True : False)));
        string[] lines = [];
        try
        {
            lines = Events(log, complete: true);
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
        }
        Term events = Cons.From(lines.Select(line => (Term)new BitString(Encoding.ASCII.GetBytes(line))));
        Term report = Term.Tuple(
            Term.I(accepted),
            Term.Tuple(Term.I(first), Term.I(second)),
            results[0],
            results[1],
            results[2],
            results[3],
            events
        );
        bool passed = accepted >= 0 && errors.Count == initialErrors && results.All(value => value is TupleTerm { Items: [Atom { Name: Value }, _] });

        return Term.Tuple(Term.A(passed ? Returned : CleanupFailed), report);
    }

    internal static bool CleanupPrefixEquals(Term actual, Term original)
    {
        if (actual is not TupleTerm { Items: [Atom { Name: Returned }, TupleTerm a] }
            || original is not TupleTerm { Items: [Atom { Name: Returned }, TupleTerm b] }
            || a.Items.Count != CleanupFields || b.Items.Count != CleanupFields) return false;

        return Enumerable.Range(0, CleanupFields - 1).All(index => a.Items[index].Equals(b.Items[index]));
    }

    internal static bool EventContract(string[] events)
    {
        var counts = new Dictionary<string, int>
        {
            [Load] = 2,
            [Constructor] = 1,
            [AckAttempt] = 1,
            [AckWritten] = 1,
            [AckSeen] = 1,
            [Upgrade] = 1,
            [Unload1] = 1,
            [Unload2] = 1
        };
        if (events.Any(e => !counts.ContainsKey(e)) || counts.Any(pair => events.Count(e => e == pair.Key) != pair.Value)) return false;

        int At(string name) => Array.IndexOf(events, name);
        int load = Array.FindLastIndex(events, e => e == Load);

        return At(Constructor) < At(AckAttempt) && At(AckAttempt) < At(AckSeen) && At(AckSeen) < At(Upgrade)
            && At(Upgrade) < load && load < At(Unload1) && At(Unload1) < At(Unload2) && At(AckAttempt) < At(AckWritten);
    }
}
