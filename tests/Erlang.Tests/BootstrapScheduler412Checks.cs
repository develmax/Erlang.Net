using System.Diagnostics;
using Erlang;

namespace Erlang.Tests;

internal static class BootstrapScheduler412Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(BootstrapScheduler412Names.Failure);
    }

    private static Task<Term> Set(RuntimeNodeFoundationHostActor actor, ProcessRuntime runtime, Term value)
        => actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            BootstrapScheduler412Names.Module,
            BootstrapScheduler412Names.Function,
            value
        ));

    private static void Install(ProcessRuntime runtime) => RuntimeBootstrapScheduler.Register(runtime, runtime.Modules);

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime(BootstrapScheduler412Names.Capacity);
        Require(!runtime.Modules.HasExport(BootstrapScheduler412Names.Module, BootstrapScheduler412Names.Function, 1));
        Install(runtime);
        Install(runtime);
        Require(runtime.Modules.HasExport(BootstrapScheduler412Names.Module, BootstrapScheduler412Names.Function, 1)
            && runtime.Modules.SourceOwner(BootstrapScheduler412Names.Module, BootstrapScheduler412Names.Function, 1) is null);
        await using var foreign = new ProcessRuntime(BootstrapScheduler412Names.Capacity);
        bool refused = false;
        try
        {
            RuntimeBootstrapScheduler.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused && !foreign.Modules.HasExport(BootstrapScheduler412Names.Module, BootstrapScheduler412Names.Function, 1));
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        await foreignActor.InvokeAsync(async context =>
        {
            bool rejected = false;
            try
            {
                _ = await runtime.Modules.Call(
                    context,
                    BootstrapScheduler412Names.Module,
                    BootstrapScheduler412Names.Function,
                    Term.A(BootstrapScheduler412Names.True)
                );
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Require(rejected && !runtime.CaptureManagedSchedulerWallTime().Enabled
                && !foreign.CaptureManagedSchedulerWallTime().Enabled);

            return true;
        });
        await using var conflict = new ProcessRuntime(BootstrapScheduler412Names.Capacity);
        conflict.Modules.Register(
            BootstrapScheduler412Names.Module,
            BootstrapScheduler412Names.Function,
            1,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        refused = false;
        try
        {
            Install(conflict);
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        Require(refused);
        await using var rollback = new ProcessRuntime(BootstrapScheduler412Names.Capacity);
        var sentinel = new InvalidOperationException(BootstrapScheduler412Names.Failure);
        try
        {
            await rollback.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapScheduler.Register(rollback, registry);
                throw sentinel;
            });
        }
        catch (InvalidOperationException error) when (ReferenceEquals(error, sentinel)) { }
        Require(!rollback.Modules.HasExport(BootstrapScheduler412Names.Module, BootstrapScheduler412Names.Function, 1));
        Install(rollback);
        Require(rollback.Modules.HasExport(BootstrapScheduler412Names.Module, BootstrapScheduler412Names.Function, 1));
    }

    internal static async Task Measurement()
    {
        await using var runtime = new ProcessRuntime(BootstrapScheduler412Names.Capacity);
        Install(runtime);
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        foreach (var item in new[] { (first, 1), (second, 2) })
            await item.Item1.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                BootstrapScheduler412Names.Erlang,
                BootstrapScheduler412Names.ProcessFlag,
                Term.A(BootstrapScheduler412Names.Scheduler),
                Term.I(item.Item2)
            ));
        var disabled = runtime.CaptureManagedSchedulerWallTime();
        Require(!disabled.Enabled && disabled.Lanes.Count == 0);
        Require((await Set(first, runtime, Term.A(BootstrapScheduler412Names.True))).Equals(Term.A(BootstrapScheduler412Names.False)));
        var before = runtime.CaptureManagedSchedulerWallTime();
        foreach (var actor in new[] { first, second })
            await actor.InvokeAsync(_ =>
            {
                var watch = Stopwatch.StartNew();
                while (watch.ElapsedMilliseconds < BootstrapScheduler412Names.WorkMilliseconds)
                    Thread.SpinWait(BootstrapScheduler412Names.SpinQuantum);

                return Task.FromResult(true);
            });
        var worked = runtime.CaptureManagedSchedulerWallTime();
        Require(worked.Enabled && worked.TimestampFrequency == Stopwatch.Frequency
            && worked.Lanes.Count == BootstrapScheduler412Names.Capacity
            && worked.Lanes.Select(lane => lane.Lane).Distinct().Count() == BootstrapScheduler412Names.Capacity);
        foreach (var lane in worked.Lanes)
        {
            var old = before.Lanes.Single(value => value.Lane == lane.Lane);
            Require(lane.ActiveTicks > old.ActiveTicks && lane.TotalTicks >= lane.ActiveTicks
                && lane.TotalTicks >= old.TotalTicks && lane.OriginTimestamp == old.OriginTimestamp);
        }
        Require((await Set(first, runtime, Term.A(BootstrapScheduler412Names.True))).Equals(Term.A(BootstrapScheduler412Names.True)));
        var repeated = runtime.CaptureManagedSchedulerWallTime();
        Require(repeated.Generation == worked.Generation && repeated.Lanes.All(lane =>
            lane.OriginTimestamp == worked.Lanes.Single(value => value.Lane == lane.Lane).OriginTimestamp));
        Require((await Set(first, runtime, Term.A(BootstrapScheduler412Names.False))).Equals(Term.A(BootstrapScheduler412Names.True)));
        Require(!runtime.CaptureManagedSchedulerWallTime().Enabled && runtime.CaptureManagedSchedulerWallTime().Lanes.Count == 0);
        _ = await Set(first, runtime, Term.A(BootstrapScheduler412Names.True));
        var reset = runtime.CaptureManagedSchedulerWallTime();
        Require(reset.Generation == worked.Generation + 1 && reset.Lanes.All(lane => lane.ActiveTicks <= lane.TotalTicks
            && lane.OriginTimestamp > worked.Lanes.Single(value => value.Lane == lane.Lane).OriginTimestamp));
        _ = await Set(first, runtime, Term.A(BootstrapScheduler412Names.False));
    }

    internal static async Task Values()
    {
        await using var runtime = new ProcessRuntime(BootstrapScheduler412Names.Capacity);
        Install(runtime);
        var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        try
        {
            foreach (Term value in new Term[] { Term.A(BootstrapScheduler412Names.False), Term.A(BootstrapScheduler412Names.Other),
                Nil.Value, Term.I(1), Term.Tuple(Term.I(2)), new BitString([]), context.Self })
            {
                _ = await Set(actor, runtime, Term.A(BootstrapScheduler412Names.True));
                Require((await Set(actor, runtime, value)).Equals(Term.A(BootstrapScheduler412Names.True)));
                Require((await Set(actor, runtime, value)).Equals(Term.A(BootstrapScheduler412Names.False))
                    && !runtime.CaptureManagedSchedulerWallTime().Enabled);
            }
        }
        finally
        {
            await actor.DisposeAsync();
        }
        bool canceled = false;
        try
        {
            _ = await runtime.Modules.Call(
                context,
                BootstrapScheduler412Names.Module,
                BootstrapScheduler412Names.Function,
                Term.A(BootstrapScheduler412Names.True)
            );
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))
            || error.Reason.Equals(Term.A(ErlangErrorReasons.NoProcess)))
        {
            canceled = true;
        }
        Require(canceled && !runtime.CaptureManagedSchedulerWallTime().Enabled);
    }
}
