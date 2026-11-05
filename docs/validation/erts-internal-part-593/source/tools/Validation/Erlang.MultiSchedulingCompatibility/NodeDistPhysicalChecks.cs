using Erlang;
using static MultiSchedulingProbe;
using static MultiSchedulingProbeNames;
using static NodeDistPhysicalNames;

internal static class NodeDistPhysicalChecks
{
    private static void EnableState(ProcessContext context)
        => typeof(ProcessRuntime).GetMethod(Write, Members)!.Invoke(context.Runtime,
            [context, Term.A(Enable), Term.A(True)]);

    private static ValueTask<Term> Capture(ProcessContext context)
        => (ValueTask<Term>)typeof(ProcessRuntime).GetMethod(Read, Members)!.Invoke(context.Runtime,
            [context, Term.Tuple(Term.A(Query), Term.A(Blocked))])!;

    private static long Count(
        Term snapshot,
        int table,
        Term key,
        Term owner
    )
    {
        TupleTerm group = (TupleTerm)((TupleTerm)snapshot).Items[table];
        TupleTerm? row = Cons.Items(group.Items[1]).Cast<TupleTerm>().FirstOrDefault(row => row.Items[0].Equals(key));
        if (row is null) return 0;
        TupleTerm? referrer = Cons.Items(row.Items[2]).Cast<TupleTerm>().FirstOrDefault(row => row.Items[0].Equals(owner));

        return referrer is null ? 0 : Cons.Items(referrer.Items[1]).Cast<TupleTerm>()
            .Where(pair => pair.Items[0].Equals(Term.A(Heap))).Sum(pair => (long)((Integer)pair.Items[1]).Value);
    }

    internal static async Task StableRoots(bool owned)
    {
        var options = new ProcessRuntimeOptions();
        typeof(ProcessRuntimeOptions).GetProperty(Profile, Members)!.SetValue(options, owned);
        await using var runtime = new ProcessRuntime(options);
        await Finish(Start(
            runtime,
            async context =>
        {
            EnableState(context);
            context.Dictionary[Term.A(Key)] = Term.Tuple(new Pid(Remote, 1, FirstCreation), new PortTerm(Remote, 2, SecondCreation));
            await FlagCall(context, Block);
            try
            {
                Term first = await Capture(context);
                Term owner = Term.Tuple(Term.A(Process), context.Self);
                Require(Count(
                    first,
                    0,
                    Term.Tuple(Term.A(Remote), Term.I(FirstCreation)),
                    owner
                ) == 1);
                Require(Count(
                    first,
                    0,
                    Term.Tuple(Term.A(Remote), Term.I(SecondCreation)),
                    owner
                ) == 1);
                for (int attempt = 0; attempt < Repeats; attempt++) Require(first.Equals(await Capture(context)));
            }
            finally
            {
                await FlagCall(context, Unblock);
            }

            return Nil.Value;
        }
        ));
    }

    internal static async Task PendingHandle()
    {
        var options = new ProcessRuntimeOptions();
        typeof(ProcessRuntimeOptions).GetProperty(Profile, Members)!.SetValue(options, true);
        await using var runtime = new ProcessRuntime(options);
        Pid? previous = null;
        await Finish(Start(
            runtime,
            async context =>
        {
            previous = context.Self;
            EnableState(context);
            context.Dictionary[Term.A(Key)] = runtime.DistributionPending.BeginPending(Term.A(Remote));
            await FlagCall(context, Block);
            Term owner = Term.Tuple(Term.A(Process), context.Self);
            Term first = await Capture(context);
            Require(Count(
                first,
                1,
                Term.A(Remote),
                owner
            ) == 1);
            Require(first.Equals(await Capture(context)));

            // Exact process death, rather than an explicit unblock, retires this
            // real header and releases the scheduler reservation.
            return Nil.Value;
        }
        ));
        await Finish(Start(
            runtime,
            async context =>
        {
            await FlagCall(context, Block);
            try
            {
                Term snapshot = await Capture(context);
                Require(Count(
                    snapshot,
                    1,
                    Term.A(Remote),
                    Term.Tuple(Term.A(Process), previous!)
                ) == 0);
            }
            finally
            {
                await FlagCall(context, Unblock);
            }

            return Nil.Value;
        }
        ));
    }
}
