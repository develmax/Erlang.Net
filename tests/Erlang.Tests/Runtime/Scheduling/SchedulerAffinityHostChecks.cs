namespace Erlang.Tests;

internal static class SchedulerAffinityHostChecks
{
    internal static async Task FirstInstructionAndMigration()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        var process = runtime.Spawn(async context =>
        {
            Term zero = await Flag(context, SchedulerAffinityHostNames.First);
            Require(zero.Equals(Term.I(SchedulerAffinityHostNames.Unbound)));
            Require((await Identity(context)).Equals(Term.I(SchedulerAffinityHostNames.First)));
            Term previous = await Flag(context, SchedulerAffinityHostNames.Second);
            Require(previous.Equals(Term.I(SchedulerAffinityHostNames.First)));
            Require((await Identity(context)).Equals(Term.I(SchedulerAffinityHostNames.Second)));
            var child = new FunctionTerm(
                0,
                async (executionContext, _) =>
            {
                var childContext = (ProcessContext)executionContext;
                Require((await Identity(childContext)).Equals(Term.I(SchedulerAffinityHostNames.First)));
                await childContext.YieldExecution();
                Require((await Identity(childContext)).Equals(Term.I(SchedulerAffinityHostNames.First)));

                return Nil.Value;
            }
            );
            Term spawned = await runtime.Modules.Call(
                context,
                SchedulerAffinityHostNames.Erlang,
                SchedulerAffinityHostNames.Spawn,
                child,
                Options(SchedulerAffinityHostNames.First)
            );
            await Down(context, spawned);
            Require((await Flag(context, SchedulerAffinityHostNames.Unbound)).Equals(Term.I(SchedulerAffinityHostNames.Second)));

            return Nil.Value;
        });
        Require((await process.Completion.WaitAsync(TimeSpan.FromSeconds(SchedulerAffinityHostNames.TimeoutSeconds)))
            .Equals(Term.A(SchedulerAffinityHostNames.Normal)));
    }

    internal static async Task OfflineChildParksAndResumes()
    {
        await using var runtime = new ProcessRuntime(SchedulerAffinityHostNames.Capacity);
        int executions = 0;
        var process = runtime.Spawn(async context =>
        {
            await Flag(context, SchedulerAffinityHostNames.First);
            await runtime.Modules.Call(
                context,
                SchedulerAffinityHostNames.Erlang,
                SchedulerAffinityHostNames.SystemFlag,
                Term.A(SchedulerAffinityHostNames.Online),
                Term.I(SchedulerAffinityHostNames.First)
            );
            var child = new FunctionTerm(
                0,
                async (executionContext, _) =>
            {
                var childContext = (ProcessContext)executionContext;
                Require((await Identity(childContext)).Equals(Term.I(SchedulerAffinityHostNames.Second)));
                Interlocked.Increment(ref executions);

                return Nil.Value;
            }
            );
            Term spawned = await runtime.Modules.Call(
                context,
                SchedulerAffinityHostNames.Erlang,
                SchedulerAffinityHostNames.Spawn,
                child,
                Options(SchedulerAffinityHostNames.Second)
            );
            await context.YieldExecution();
            Require(Volatile.Read(ref executions) == SchedulerAffinityHostNames.Unbound);
            await runtime.Modules.Call(
                context,
                SchedulerAffinityHostNames.Erlang,
                SchedulerAffinityHostNames.SystemFlag,
                Term.A(SchedulerAffinityHostNames.Online),
                Term.I(SchedulerAffinityHostNames.Second)
            );
            await Down(context, spawned);
            Require(Volatile.Read(ref executions) == SchedulerAffinityHostNames.First);

            return Nil.Value;
        });
        Require((await process.Completion.WaitAsync(TimeSpan.FromSeconds(SchedulerAffinityHostNames.TimeoutSeconds)))
            .Equals(Term.A(SchedulerAffinityHostNames.Normal)));
    }

    private static ValueTask<Term> Identity(ProcessContext context) => context.Runtime.Modules.Call(
        context,
        SchedulerAffinityHostNames.Erlang,
        SchedulerAffinityHostNames.Info,
        Term.A(SchedulerAffinityHostNames.Identity)
    );

    private static ValueTask<Term> Flag(ProcessContext context, int lane) => context.Runtime.Modules.Call(
        context,
        SchedulerAffinityHostNames.Erlang,
        SchedulerAffinityHostNames.Flag,
        Term.A(SchedulerAffinityHostNames.Scheduler),
        Term.I(lane)
    );

    private static Term Options(int lane) => Term.List(Term.A(SchedulerAffinityHostNames.Monitor),
        Term.Tuple(Term.A(SchedulerAffinityHostNames.Scheduler), Term.I(lane)));

    private static async Task Down(ProcessContext context, Term spawned)
    {
        var pair = (TupleTerm)spawned;
        Term? result = await context.ReceiveAsync(
            value => value is TupleTerm tuple && tuple.Items.Count == 5 &&
            tuple.Items[0].Equals(Term.A(SchedulerAffinityHostNames.Down)) && tuple.Items[1].Equals(pair.Items[1])
            ? value : null,
            TimeSpan.FromSeconds(SchedulerAffinityHostNames.TimeoutSeconds)
        );
        Require(result is TupleTerm down && down.Items[4].Equals(Term.A(SchedulerAffinityHostNames.Normal)));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SchedulerAffinityHostNames.Failure);
    }
}
