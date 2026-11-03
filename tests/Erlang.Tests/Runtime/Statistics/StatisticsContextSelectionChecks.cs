using System.Numerics;
using System.Reflection;

namespace Erlang.Tests;

// Reflection invokes real admission APIs/reads witnesses;
// it never edits queue/epoch/counter fields or calls Record* directly.
internal static class StatisticsContextSelectionChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static Task Interleaved() => SameEpoch(true);

    internal static Task Adjacent() => SameEpoch(false);

    private static async Task SameEpoch(bool interleave)
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = new TaskCompletionSource<(long Epoch, BigInteger Count)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var middle = new TaskCompletionSource<(long Epoch, BigInteger Count)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var last = new TaskCompletionSource<(long Epoch, BigInteger Count)>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle? peer = null;
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                first.TrySetResult((Epoch(context), Sample(context)));
                Continue(
                    context,
                    () =>
                {
                    try
                    {
                        last.TrySetResult((Epoch(context), Sample(context)));
                    }
                    catch (Exception error)
                    {
                        last.TrySetException(error);
                    }
                    finally
                    {
                        completed.TrySetResult();
                    }
                }
                );
                if (interleave)
                    peer = SpawnHigh(runtime, child =>
                    {
                        middle.TrySetResult((Epoch(child), Sample(child)));

                        return ValueTask.FromResult<Term>(Nil.Value);
                    });

                await completed.Task;

                return Nil.Value;
            }
            catch (Exception error)
            {
                first.TrySetException(error);
                middle.TrySetException(error);
                last.TrySetException(error);
                throw;
            }
        });
        try
        {
            var a = await first.Task.WaitAsync(StatisticsContextSelectionNames.Timeout);
            var c = await last.Task.WaitAsync(StatisticsContextSelectionNames.Timeout);
            Require(a.Epoch > 0 && c.Epoch == a.Epoch);
            if (interleave)
            {
                var b = await middle.Task.WaitAsync(StatisticsContextSelectionNames.Timeout);
                Require(b.Epoch > 0 && b.Count == a.Count + 1 && c.Count == b.Count + 1);
            }
            else
            {
                Require(c.Count == a.Count + 1);
            }
        }
        finally
        {
            completed.TrySetResult();
            await actor.Completion.WaitAsync(StatisticsContextSelectionNames.Timeout);
            if (peer is not null)
                await peer.Completion.WaitAsync(StatisticsContextSelectionNames.Timeout);
        }
    }

    internal static async Task InitializerAndInline()
    {
        await using var runtime = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(_ => { });
        await runtime.InitializeModulesAsync(_ => { });
        var observed = new TaskCompletionSource<(BigInteger Before, BigInteger After)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(context =>
        {
            try
            {
                BigInteger before = Sample(context);
                var task = new Task(() => { });
                task.RunSynchronously(TaskScheduler.Current);
                Require(task.IsCompletedSuccessfully);
                observed.TrySetResult((before, Sample(context)));
            }
            catch (Exception error)
            {
                observed.TrySetException(error);
                throw;
            }

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        var pair = await observed.Task.WaitAsync(StatisticsContextSelectionNames.Timeout);
        Require(pair.Before == 1 && pair.After == pair.Before);
        await actor.Completion.WaitAsync(StatisticsContextSelectionNames.Timeout);
    }

    private static BigInteger Sample(ProcessContext context)
    {
        ValueTask<Term> read = context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            RuntimeStatisticsNames.Function,
            Term.A(RuntimeStatisticsNames.ContextSwitches)
        );
        Require(read.IsCompletedSuccessfully);
        var tuple = (TupleTerm)read.Result;
        Require(tuple.Items.Count == 2 && tuple.Items[1].Equals(Term.I(0)));

        return ((Integer)tuple.Items[0]).Value;
    }

    private static long Epoch(ProcessContext context)
    {
        Type turn = typeof(ProcessRuntime).Assembly.GetType(StatisticsContextSelectionNames.TurnType, true)!;

        return (long)turn.GetMethod(StatisticsContextSelectionNames.Epoch, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [context])!;
    }

    private static void Continue(ProcessContext context, Action body)
    {
        object scheduler = typeof(ProcessContext).GetProperty(StatisticsContextSelectionNames.Scheduler, Members)!.GetValue(context)!;
        MethodInfo method = scheduler.GetType().GetMethod(StatisticsContextSelectionNames.Continue, Members)!;
        object admission = Enum.Parse(method.GetParameters()[1].ParameterType, StatisticsContextSelectionNames.Continuation);
        method.Invoke(scheduler, [body, admission]);
    }

    private static ProcessHandle SpawnHigh(ProcessRuntime runtime, Func<ProcessContext, ValueTask<Term>> body)
    {
        MethodInfo method = typeof(ProcessRuntime).GetMethod(StatisticsContextSelectionNames.Spawn, Members)!;
        var parameters = method.GetParameters();
        object?[] values = parameters.Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : null).ToArray();
        values[0] = body;
        values[2] = false;
        int priority = Array.FindIndex(parameters, parameter => parameter.Name == StatisticsContextSelectionNames.InitialPriority);
        values[priority] = Enum.Parse(parameters[priority].ParameterType, StatisticsContextSelectionNames.High);

        return (ProcessHandle)method.Invoke(runtime, values)!;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(StatisticsContextSelectionNames.Failure);
    }
}
