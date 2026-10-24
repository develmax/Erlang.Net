namespace Erlang.Tests;

internal static class ProcessBumpCapHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        foreach (int warm in ProcessBumpCapHostNames.WarmCharges)
        {
            var worker = runtime.Spawn(async context =>
            {
                for (int index = 0; index < warm; index++)
                    await context.ReduceAsync();
                long before = await Count(context);
                Require(before == warm);
                Require((await Bump(context, Term.I(ProcessBumpCapHostNames.NativeRequestCap))).Equals(Term.A(ErlangBooleanAtoms.True)));
                long after = await Count(context);
                int remaining = ProcessBumpCapHostNames.LogicalSlice - warm % ProcessBumpCapHostNames.LogicalSlice;
                Require(after - before == remaining);
                Require((await Bump(context, Term.I(ProcessBumpCapHostNames.Zero))).Equals(Term.A(ErlangBooleanAtoms.True)));
                Require(await Count(context) == after);
                Require((await Bump(context, Term.I(ProcessBumpCapHostNames.Small))).Equals(Term.A(ErlangBooleanAtoms.True)));
                Require(await Count(context) == after + ProcessBumpCapHostNames.Small);
                long oversizedBefore = await Count(context);
                Require((await Bump(context, Term.I(ProcessBumpCapHostNames.MaximumNativeSmall))).Equals(Term.A(ErlangBooleanAtoms.True)));
                Require(await Count(context) - oversizedBefore == ProcessBumpCapHostNames.LogicalSlice - ProcessBumpCapHostNames.Small);
                foreach (Term invalid in new Term[] { Term.I(-ProcessBumpCapHostNames.Small), Nil.Value, Term.I(ProcessBumpCapHostNames.NativeTooLarge) })
                {
                    long rejectedBefore = await Count(context);
                    try
                    {
                        await Bump(context, invalid);
                        throw new InvalidOperationException(ProcessBumpCapHostNames.Failure);
                    }
                    catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                    {
                    }
                    Require(await Count(context) == rejectedBefore);
                }

                return Nil.Value;
            });
            Require((await worker.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
        }
    }

    private static ValueTask<Term> Bump(ProcessContext context, Term count) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        ErlangProcessQueryNames.Bump,
        count
    );

    private static async ValueTask<long> Count(ProcessContext context)
    {
        var result = (TupleTerm)await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ProcessInfoNames.Function,
            context.Self,
            Term.A(ProcessInfoLogicalNames.Reductions)
        );

        return checked((long)((Integer)result.Items[1]).Value);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessBumpCapHostNames.Failure);
    }
}
