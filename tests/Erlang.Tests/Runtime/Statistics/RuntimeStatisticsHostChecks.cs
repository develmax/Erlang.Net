using System.Diagnostics;
using System.Numerics;

namespace Erlang.Tests;

internal static class RuntimeStatisticsHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var samples = new System.Collections.Concurrent.ConcurrentBag<(Pid Owner, TupleTerm Wall, TupleTerm Cpu)>();
        TupleTerm? firstWall = null;
        TupleTerm? firstCpu = null;
        var workers = Enumerable.Range(0, RuntimeStatisticsHostParameters.Owners).Select(_ => runtime.Spawn(async context =>
        {
            await start.Task;
            for (int index = 0; index < RuntimeStatisticsHostParameters.SamplesPerOwner; index++)
            {
                long busy = Stopwatch.GetTimestamp();
                while (Stopwatch.GetElapsedTime(busy) < RuntimeStatisticsHostParameters.BusyInterval)
                    Thread.SpinWait(RuntimeStatisticsHostParameters.BusySpinIterations);
                TupleTerm wall = await Sample(context, RuntimeStatisticsNames.WallClock);
                TupleTerm cpu = await Sample(context, RuntimeStatisticsNames.Runtime);
                samples.Add((context.Self, wall, cpu));
            }

            return Nil.Value;
        })).ToArray();
        var controller = runtime.Spawn(async context =>
        {
            Term microstates = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeStatisticsNames.Function,
                Term.A(RuntimeStatisticsNames.MicrostateAccounting)
            );
            Term[] rows = Cons.Items(microstates).ToArray();
            Require(rows.Length > 0 && rows.All(row => row is MapTerm map
                && map.Entries.Count == RuntimeStatisticsHostParameters.MicrostateFields));
            var io = await Sample(context, RuntimeStatisticsNames.Io);
            Require(io.Items is [
                TupleTerm { Items: [Atom { Name: RuntimeStatisticsHostParameters.Input }, Integer { Value.IsZero: true }] },
                TupleTerm { Items: [Atom { Name: RuntimeStatisticsHostParameters.Output }, Integer { Value.IsZero: true }] }
            ]);
            var switches = await Sample(context, RuntimeStatisticsNames.ContextSwitches);
            Require(switches.Items is [Integer { Value.Sign: > 0 }, Integer { Value.IsZero: true }]);
            foreach (string wallKey in new[] { RuntimeStatisticsNames.SchedulerWallTime, RuntimeStatisticsNames.SchedulerWallTimeAll })
                Require((await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    RuntimeStatisticsNames.Function,
                    Term.A(wallKey)
                )).Equals(Term.A(ProcessRegistryAtoms.Undefined)));
            var queues = await Sample(context, RuntimeStatisticsNames.RunQueues);
            Require(queues.Items.Count == runtime.ActorDispatchCapacity + RuntimeStatisticsHostParameters.DirtyQueueKinds);
            Require(queues.Items.All(value => value is Integer { Value.Sign: >= 0 }));
            var beforeGc = await Sample(context, RuntimeStatisticsNames.GarbageCollection);
            Require(beforeGc.Items is [Integer { Value.Sign: >= 0 }, Integer { Value.Sign: >= 0 }, Integer { Value.IsZero: true }]);
            // Public collection uses the actual VM heap. Observe its own event,
            // independently of the still-legacy CLR statistics selector.
            var beforeVmGc = await VmStorageBirthAccess.Sample(context);
            await runtime.Modules.Call(context, ErlangModuleNames.Module, ProcessGarbageCollectionNames.Function);
            var afterVmGc = await VmStorageBirthAccess.Sample(context);
            Require(afterVmGc.Collections == beforeVmGc.Collections + 1);
            var afterGc = await Sample(context, RuntimeStatisticsNames.GarbageCollection);
            Require(((Integer)afterGc.Items[0]).Value >= ((Integer)beforeGc.Items[0]).Value);
            Require(((Integer)afterGc.Items[1]).Value >= ((Integer)beforeGc.Items[1]).Value);
            Require(afterGc.Items[2].Equals(Term.I(0)));
            firstWall = await Sample(context, RuntimeStatisticsNames.WallClock);
            firstCpu = await Sample(context, RuntimeStatisticsNames.Runtime);
            Require(firstWall.Items[0].Equals(firstWall.Items[1]));
            Require(firstCpu.Items[0].Equals(firstCpu.Items[1]));
            start.SetResult();

            return Nil.Value;
        });
        Require((await controller.Completion.WaitAsync(RuntimeStatisticsHostParameters.Timeout)).Equals(Term.A(ProcessExitReasons.Normal)));
        await Task.WhenAll(workers.Select(worker => worker.Completion)).WaitAsync(RuntimeStatisticsHostParameters.Timeout);
        Require(workers.All(worker => worker.Completion.Result.Equals(Term.A(ProcessExitReasons.Normal))));
        var captured = samples.ToArray();
        Require(captured.Length == RuntimeStatisticsHostParameters.Owners * RuntimeStatisticsHostParameters.SamplesPerOwner);
        Require(captured.Select(sample => sample.Owner).Distinct().Count() == RuntimeStatisticsHostParameters.Owners);
        Verify(firstWall!, captured.Select(sample => sample.Wall));
        Verify(firstCpu!, captured.Select(sample => sample.Cpu));
    }

    private static void Verify(TupleTerm initial, IEnumerable<TupleTerm> values)
    {
        BigInteger previous = ((Integer)initial.Items[0]).Value;
        BigInteger last = previous;
        BigInteger deltas = 0;
        foreach (TupleTerm value in values)
        {
            Require(value.Items is [Integer total, Integer delta] && total.Value >= 0 && delta.Value >= 0 && delta.Value <= total.Value);
            last = BigInteger.Max(last, ((Integer)value.Items[0]).Value);
            deltas += ((Integer)value.Items[1]).Value;
        }
        Require(last > previous);
        Require(deltas == last - previous);
    }

    private static async ValueTask<TupleTerm> Sample(ProcessContext context, string key) => (TupleTerm)await context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        RuntimeStatisticsNames.Function,
        Term.A(key)
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(RuntimeStatisticsHostParameters.Failure);
    }
}
