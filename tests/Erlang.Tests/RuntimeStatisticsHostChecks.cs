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
            foreach (string key in RuntimeStatisticsHostParameters.PhysicalKeys)
            {
                try
                {
                    await Sample(context, key);
                    throw new InvalidOperationException(RuntimeStatisticsHostParameters.Failure);
                }
                catch (ErlangException exception)
                {
                    Require(exception.Reason.Equals(Term.A(RuntimeStatisticsNames.NotSupported)));
                }
            }
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
