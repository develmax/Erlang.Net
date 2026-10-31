using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

internal static class CpuStatisticsAccess
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(CpuStatisticsParameters.Failure);
    }

    internal static CpuStatisticsSample Parse(Term value)
    {
        Require(value is TupleTerm { Items.Count: 2 });
        var tuple = (TupleTerm)value;
        Require(tuple.Items[0] is Integer { Value.Sign: >= 0 } && tuple.Items[1] is Integer { Value.Sign: >= 0 });
        var sample = new CpuStatisticsSample(((Integer)tuple.Items[0]).Value, ((Integer)tuple.Items[1]).Value);
        Require(sample.Delta <= sample.Total);

        return sample;
    }

    internal static CpuStatisticsSample Direct(ProcessRuntime runtime)
        => Parse(runtime.Statistics.Sample(RuntimeStatisticsNames.Runtime));

    internal static Task<CpuStatisticsSample> Bif(ProcessRuntime runtime)
        => Actor<CpuStatisticsSample>(
            runtime,
            async context => Parse(await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                RuntimeStatisticsNames.Function,
                Term.A(RuntimeStatisticsNames.Runtime)
            ))
        );

    internal static async Task<T> Actor<T>(ProcessRuntime runtime, Func<ProcessContext, ValueTask<T>> body)
    {
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                result.TrySetResult(await body(context));
            }
            catch (Exception error)
            {
                result.TrySetException(error);
            }

            return Nil.Value;
        });
        T? value = default;
        Exception? failure = null;
        try
        {
            value = await result.Task.WaitAsync(CpuStatisticsParameters.Deadline);
        }
        catch (Exception error)
        {
            failure = error;
        }
        try
        {
            Term reason = await actor.Completion.WaitAsync(CpuStatisticsParameters.Deadline);
            Require(reason.Equals(Term.A(ProcessExitReasons.Normal)));
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();

        return value!;
    }

    internal static RuntimeActorDispatcher Dispatcher(ProcessRuntime runtime)
        => (RuntimeActorDispatcher)typeof(ProcessRuntime).GetField(
            CpuStatisticsParameters.DispatcherField,
            BindingFlags.Instance | BindingFlags.NonPublic
        )!.GetValue(runtime)!;
}
