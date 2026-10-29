namespace Erlang;

internal static class CountersNative421
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || IntPtr.Size != sizeof(ulong) || module != CountersNames421.Module
            || !CountersNames421.Signatures.Contains((function, arity))) return false;
        body = (context, arguments) =>
        {
            if (arguments.Count != arity) throw new ArgumentException(CountersNames421.Foreign);

            return ValueTask.FromResult(Invoke(
                owner,
                context,
                function,
                arguments
            ));
        };

        return true;
    }

    private static Term Invoke(
        ProcessRuntime owner,
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        int lane = owner.RequireCountersActor421(context);
        RuntimeCountersOwner421 state = owner.CountersOwner421;
        using var invocation = state.Enter();
        if (function == CountersNames421.New)
        {
            ulong length = CountersValues421.Length(arguments[0]);

            return CountersProducer596.Create(
                owner,
                state,
                context,
                length
            );
        }
        if (arguments[0] is not ReferenceTerm reference) throw new ErlangException(ErlangErrorReasons.BadArgument);
        using var resource = owner.ReferenceResources.AcquireCounters421(reference);
        if (resource is null || !ReferenceEquals(resource.Value.Owner, state))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        context.RequireCountersReference596(reference, resource.Value);
        using var lease = resource.Value.Array.Acquire();
        if (lease is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
        CountersArray421 selected = lease.Value;
        if (function == CountersNames421.Info)
            return new MapTerm([
                new(Term.A(CountersNames421.Memory), new Integer(selected.OriginalSize591)),
                new(Term.A(CountersNames421.Size), new Integer(selected.Length))
            ]);
        ulong index = CountersValues421.Index(arguments[1], selected.Length);
        if (function == CountersNames421.Add)
        {
            long delta = CountersValues421.Value(arguments[2], true);
            context.Cancellation.ThrowIfCancellationRequested();
            ref long cell = ref selected.Cell(index, lane);
            Volatile.Write(ref cell, unchecked(Volatile.Read(ref cell) + delta));

            return Term.A(CommonAtomNames.Ok);
        }
        // Pinned counters_put validates the signed replacement before sampling
        // scheduler lanes. Reads for get still include the base cell.
        long replacement = function == CountersNames421.Put ? CountersValues421.Value(arguments[2], false) : 0;
        long sum = 0;
        for (int current = function == CountersNames421.Put ? 1 : 0; current <= selected.Lanes; current++)
            sum = unchecked(sum + Volatile.Read(ref selected.Cell(index, current)));
        if (function == CountersNames421.Get) return Term.I(sum);
        context.Cancellation.ThrowIfCancellationRequested();
        Volatile.Write(ref selected.Cell(index, 0), unchecked(replacement - sum));

        return Term.A(CommonAtomNames.Ok);
    }
}
