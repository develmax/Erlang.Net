using System.Reflection;

namespace Erlang.Tests;

internal static class TraceHookRollbackHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(
            ProcessNodeDefaults.Local,
            null,
            TraceHookRollbackHostParameters.ProcessLimit,
            null,
            TraceHookRollbackHostParameters.Capacity
        );
        int bodies = 0, hookCalls = 0;
        await runtime.InitializeModulesAsync(registry => registry.Register(
            TraceHookRollbackHostNames.WorkerModule,
            TraceHookRollbackHostNames.WorkerFunction,
            0,
            (_, _) =>
            {
                Interlocked.Increment(ref bodies);

                return ValueTask.FromResult<Term>(Nil.Value);
            }
        ));
        var tracerReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishTracer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tracer = runtime.Spawn(async context =>
        {
            tracerReady.TrySetResult(context);
            await finishTracer.Task;

            return Nil.Value;
        });
        ProcessContext tracerContext = await tracerReady.Task.WaitAsync(TraceHookRollbackHostParameters.Deadline);
        PropertyInfo property = typeof(Mailbox).GetProperty(TraceHookRollbackHostNames.IncomingTermProperty,
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var original = (Action<Term>?)property.GetValue(tracerContext.Mailbox);
        property.SetValue(
            tracerContext.Mailbox,
            (Action<Term>)(term =>
        {
            original?.Invoke(term);
            if (term is TupleTerm { Items: [Atom { Name: SequenceTraceNames.Message }, _, _] })
            {
                Interlocked.Increment(ref hookCalls);
                throw new InvalidOperationException(TraceHookRollbackHostNames.InjectedFailure);
            }
        })
        );
        try
        {
            var parent = runtime.Spawn(async context =>
            {
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SequenceTraceNames.SystemFlag,
                    Term.A(SequenceTraceNames.SystemTracer),
                    tracer.Pid
                );
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Label),
                    Term.I(TraceHookRollbackHostParameters.Label)
                );
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Send),
                    Term.A(ErlangBooleanAtoms.True)
                );
                int visible = runtime.ProcessSnapshot().Count;
                for (int attempt = 0; attempt < TraceHookRollbackHostParameters.FailureAttempts; attempt++)
                {
                    try
                    {
                        _ = await runtime.Modules.Call(
                            context,
                            ErlangModuleNames.Module,
                            ErlangModuleNames.SpawnLink,
                            Term.A(TraceHookRollbackHostNames.WorkerModule),
                            Term.A(TraceHookRollbackHostNames.WorkerFunction),
                            Nil.Value
                        );
                        Require(false);
                    }
                    catch (InvalidOperationException exception) when (exception.Message == TraceHookRollbackHostNames.InjectedFailure)
                    {
                    }
                    Require(Volatile.Read(ref hookCalls) == attempt + 1);
                    Require(runtime.ProcessSnapshot().Count == visible);
                    Require((await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ProcessInfoNames.Function,
                        context.Self,
                        Term.A(ProcessInfoNames.Links)
                    )).Equals(Term.Tuple(Term.A(ProcessInfoNames.Links), Nil.Value)));
                    await Task.Yield();
                    Require(Volatile.Read(ref bodies) == 0);
                }
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Token),
                    Nil.Value
                );
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SequenceTraceNames.SystemFlag,
                    Term.A(SequenceTraceNames.SystemTracer),
                    Term.A(ErlangBooleanAtoms.False)
                );
                var actual = runtime.Spawn(_ =>
                {
                    Interlocked.Increment(ref bodies);

                    return ValueTask.FromResult<Term>(Nil.Value);
                });
                Require((await actual.Completion.WaitAsync(TraceHookRollbackHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
                Require(Volatile.Read(ref bodies) == 1);
                Require(runtime.ProcessSnapshot().Count == visible);

                return Nil.Value;
            });
            Require((await parent.Completion.WaitAsync(TraceHookRollbackHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
            Require(!tracer.Completion.IsCompleted);
        }
        finally
        {
            property.SetValue(tracerContext.Mailbox, original);
            finishTracer.TrySetResult();
        }
        Require((await tracer.Completion.WaitAsync(TraceHookRollbackHostParameters.Deadline)).Equals(Term.A(ProcessExitReasons.Normal)));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(TraceHookRollbackHostNames.Failure);
    }
}
