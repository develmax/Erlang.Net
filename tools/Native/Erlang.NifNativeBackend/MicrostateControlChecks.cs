using System.Reflection;

namespace Erlang.NifNativeBackend;

public static class MicrostateControlChecks
{
    public static async Task SameActorEnableWorkIdle()
    {
        var failures = new List<Exception>();
        foreach (bool owned in new[] { false, true })
        {
            try
            {
                await SameActor(owned);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    private static async Task SameActor(bool owned)
    {
        TimeSpan work = CpuUserTimeProbe.CalibrateWork();
        var options = new ProcessRuntimeOptions { OwnedTermsPilot506 = owned };
        await using var runtime = new ProcessRuntime(
            ProcessNodeDefaults.Local,
            null,
            32768,
            null,
            1,
            options
        );
        var ready = new TaskCompletionSource<(RuntimeMicrostateRecord Record, MicrostateSample After)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                async Task<Term> Sample() => await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    RuntimeStatisticsNames.Function,
                    Term.A(RuntimeStatisticsNames.MicrostateAccounting)
                );
                RuntimeMicrostateRecord record = RuntimeMicrostateExecution.Current!.Record!;
                CpuStatisticsAccess.Require(record.Type == RuntimeMicrostateNames.Scheduler);
                Term previous = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SequenceTraceNames.SystemFlag,
                    Term.A(RuntimeStatisticsNames.MicrostateAccounting),
                    CoreModules.Boolean(true)
                );
                MicrostateTestAccess.Boolean(previous, false);
                Term beforeTerm = await Sample();
                MicrostateTestAccess.Capture(MicrostateControlCaseNames.SameActorBefore, beforeTerm);
                MicrostateSample before = MicrostateTestAccess.Row(MicrostateTestAccess.Parse(beforeTerm), record);
                _ = CpuUserTimeProbe.Burn(work);
                // Actual actor yield ends the measured Emulator interval. A gather
                // alone deliberately does not flush normal managed active state.
                await context.YieldExecution();
                CpuStatisticsAccess.Require(ReferenceEquals(RuntimeMicrostateExecution.Current!.Record, record));
                Term afterTerm = await Sample();
                MicrostateTestAccess.Capture(MicrostateControlCaseNames.SameActorAfter, afterTerm);
                MicrostateSample after = MicrostateTestAccess.Row(MicrostateTestAccess.Parse(afterTerm), record);
                CpuStatisticsAccess.Require(after.Counters[MicrostateCaseNames.Emulator] > before.Counters[MicrostateCaseNames.Emulator]);
                ready.TrySetResult((record, after));
                await release.Task;
                complete.TrySetResult();
            }
            catch (ErlangException error)
            {
                MicrostateTestAccess.Capture(
                    MicrostateControlCaseNames.FullFailure,
                    Term.Tuple(
                        Term.A(MicrostateControlCaseNames.Raised),
                        Term.A(error.ExceptionClass),
                        error.Reason,
                        error.StackTraceTerm
                    )
                );
                ready.TrySetException(error);
                complete.TrySetException(error);
            }
            catch (Exception error)
            {
                ready.TrySetException(error);
                complete.TrySetException(error);
            }

            return Nil.Value;
        });
        Exception? failure = null;
        try
        {
            var actual = await ready.Task.WaitAsync(CpuStatisticsParameters.Deadline);
            Thread thread = NormalThread(runtime);
            using var timeout = new CancellationTokenSource(CpuStatisticsParameters.Deadline);
            while ((thread.ThreadState & ThreadState.WaitSleepJoin) == 0)
                await Task.Delay(1, timeout.Token);
            CpuStatisticsAccess.Require(!actor.Completion.IsCompleted);
            // HOST CPU while the actual sole VM worker is physically waiting.
            // It does not fake a VM Sleep row or increment any counters.
            _ = await CpuHostUserWork.Burn(work);
            Term idleTerm = await MicrostateTestAccess.Sample(runtime);
            MicrostateTestAccess.Capture(MicrostateControlCaseNames.SameActorIdle, idleTerm);
            MicrostateSample idle = MicrostateTestAccess.Row(MicrostateTestAccess.Parse(idleTerm), actual.Record);
            CpuStatisticsAccess.Require(idle.Counters[MicrostateCaseNames.Sleep] > actual.After.Counters[MicrostateCaseNames.Sleep]);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            release.TrySetResult();
            try
            {
                await complete.Task.WaitAsync(CpuStatisticsParameters.Deadline);
                CpuStatisticsAccess.Require((await actor.Completion.WaitAsync(CpuStatisticsParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
            }
            catch (Exception cleanup)
            {
                failure = failure is null ? cleanup : new AggregateException(failure, cleanup);
            }
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static Thread NormalThread(ProcessRuntime runtime)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var dispatcher = CpuStatisticsAccess.Dispatcher(runtime);
        var workers = (RuntimeNormalWorker[])typeof(RuntimeActorDispatcher).GetField(MicrostateControlCaseNames.NormalWorkers, flags)!.GetValue(dispatcher)!;
        CpuStatisticsAccess.Require(workers.Length == 1);

        return (Thread)typeof(RuntimeNormalWorker).GetField(MicrostateControlCaseNames.Thread, flags)!.GetValue(workers[0])!;
    }
}
