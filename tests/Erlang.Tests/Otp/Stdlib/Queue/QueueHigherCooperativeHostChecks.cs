namespace Erlang.Tests;

internal static class QueueHigherCooperativeHostChecks
{
    internal static async Task Run()
    {
        Term input = Cons.From(Enumerable.Repeat<Term>(Nil.Value, QueueHigherCooperativeHostNames.Cells));
        var failures = new List<Exception>();
        foreach (QueueHigherCooperativeScenario scenario in Enum.GetValues<QueueHigherCooperativeScenario>())
        {
            foreach (bool cancel in new[] { false, true })
            {
                try
                {
                    await Boundary(scenario, input, cancel);
                }
                catch (Exception failure)
                {
                    failures.Add(new InvalidOperationException(scenario.ToString(), failure));
                }
            }
        }
        Require(Cons.Items(input).Count() == QueueHigherCooperativeHostNames.Cells);
        if (failures.Count != 0)
            throw new AggregateException(QueueHigherCooperativeHostNames.Failure, failures);
    }

    private static async Task Boundary(QueueHigherCooperativeScenario scenario, Term input, bool cancel)
    {
        await using var runtime = new ProcessRuntime();
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var cleanup = new RuntimeNodeFoundationHostActor(runtime);
        var continuations = new QueueHigherYieldContext();
        var entered = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int callbacks = 0;
        int callbacksAfterCancellation = 0;
        bool rear = scenario is QueueHigherCooperativeScenario.FilterRearUnwind
            or QueueHigherCooperativeScenario.DeleteRearRebuild or QueueHigherCooperativeScenario.DeleteRearNoMatch;
        bool filter = scenario is QueueHigherCooperativeScenario.FilterFrontRebuild or QueueHigherCooperativeScenario.FilterRearUnwind;
        Term queue = rear ? Term.Tuple(input, Nil.Value) : Term.Tuple(Nil.Value, input);
        var callback = new FunctionTerm(
            QueueHigherCooperativeHostNames.CallbackArity,
            (execution, _) =>
        {
            if (((ProcessContext)execution).Cancellation.IsCancellationRequested)
                Interlocked.Increment(ref callbacksAfterCancellation);
            int observed = Interlocked.Increment(ref callbacks);
            bool matches = filter
                || scenario == QueueHigherCooperativeScenario.DeleteFrontRebuild && observed == QueueHigherCooperativeHostNames.Cells
                || scenario == QueueHigherCooperativeScenario.DeleteRearRebuild;

            return ValueTask.FromResult<Term>(Term.A(matches ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
        }
        );
        string function = scenario switch
        {
            QueueHigherCooperativeScenario.Split => QueueModuleNames.Split,
            QueueHigherCooperativeScenario.Join => QueueModuleNames.Join,
            _ => filter ? QueueModuleNames.Filter : QueueModuleNames.DeleteWith
        };
        Term[] arguments = scenario switch
        {
            QueueHigherCooperativeScenario.Split => [Term.I(QueueHigherCooperativeHostNames.SplitPoint), queue],
            QueueHigherCooperativeScenario.Join => [Term.Tuple(input, Nil.Value), Term.Tuple(Nil.Value, input)],
            _ => [callback, queue]
        };

        bool PhaseReached()
        {
            int observed = Volatile.Read(ref callbacks);

            return scenario switch
            {
                QueueHigherCooperativeScenario.FilterFrontRebuild or QueueHigherCooperativeScenario.DeleteFrontRebuild
                    => observed == QueueHigherCooperativeHostNames.Cells,
                QueueHigherCooperativeScenario.DeleteRearRebuild => observed == QueueHigherCooperativeHostNames.FirstRearMatchCallbacks,
                QueueHigherCooperativeScenario.FilterRearUnwind or QueueHigherCooperativeScenario.DeleteFrontNoMatch
                    or QueueHigherCooperativeScenario.DeleteRearNoMatch => observed > 0 && observed < QueueHigherCooperativeHostNames.Cells,
                _ => true
            };
        }
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            try
            {
                Task<Term> operation;
                SynchronizationContext? previous = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(continuations);
                try
                {
                    operation = runtime.Modules.Call(
                        context,
                        QueueModuleNames.Module,
                        function,
                        arguments
                    ).AsTask();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(previous);
                }
                entered.TrySetResult(operation);

                return await operation;
            }
            finally
            {
                unwound.TrySetResult();
            }
        });
        Task<Term>? pending = null;
        try
        {
            pending = await entered.Task.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
            Require(!pending.IsCompleted && !unwound.Task.IsCompleted);
            Require(await continuations.ResumeUntilPhase(pending, PhaseReached));
            Require(PhaseReached() && !pending.IsCompleted && !unwound.Task.IsCompleted);
            int pausedCallbacks = Volatile.Read(ref callbacks);
            bool progressed = await observer.InvokeAsync(async context =>
            {
                Require(!pending.IsCompleted && Volatile.Read(ref callbacks) == pausedCallbacks);
                Term sampled = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ProcessInfoNames.Function,
                    worker.Pid,
                    Term.A(ProcessInfoLogicalNames.Reductions)
                );
                Require(sampled is TupleTerm { Items.Count: 2 } pair && pair.Items[1] is Integer reductions
                    && reductions.Value >= QueueHigherCooperativeHostNames.MinimumObservedReductions);
                if (cancel)
                    runtime.Exit(context, worker.Pid, Term.A(QueueHigherCooperativeHostNames.Kill));

                return true;
            });
            Require(progressed && !pending.IsCompleted);
            await continuations.ResumeUntil(pending);
            Term reason = await worker.Completion.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
            await unwound.Task.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
            Require(!runtime.IsAlive(worker.Pid));
            if (cancel)
            {
                Require(pending.IsCanceled && reason.Equals(Term.A(QueueHigherCooperativeHostNames.Killed))
                    && Volatile.Read(ref callbacksAfterCancellation) == 0);
            }
            else
            {
                Require(reason.Equals(Term.A(QueueHigherCooperativeHostNames.Normal)));
                Term result = await pending;
                await observer.InvokeAsync(async context =>
                {
                    await CheckResult(
                        runtime,
                        context,
                        scenario,
                        queue,
                        result,
                        Volatile.Read(ref callbacks)
                    );

                    return true;
                });
            }
            Term alive = await observer.InvokeAsync(async context =>
                await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.New));
            Require(alive.Equals(Term.Tuple(Nil.Value, Nil.Value)));
        }
        finally
        {
            if (pending is null || !pending.IsCompleted)
            {
                await cleanup.Invoke(context =>
                {
                    runtime.Exit(context, worker.Pid, Term.A(QueueHigherCooperativeHostNames.Kill));

                    return true;
                });
                if (pending is not null)
                    await continuations.ResumeUntil(pending);
            }
            await worker.Completion.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
            await unwound.Task.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
        }
    }

    private static async Task CheckResult(
        ProcessRuntime runtime,
        ProcessContext context,
        QueueHigherCooperativeScenario scenario,
        Term original,
        Term result,
        int callbacks
    )
    {
        async Task Count(Term queue, int expected)
        {
            Term list = await runtime.Modules.Call(
                context,
                QueueModuleNames.Module,
                QueueModuleNames.ToList,
                queue
            );
            Require(Cons.Items(list).Count() == expected && Cons.Items(list).All(item => item is Nil));
        }
        if (scenario == QueueHigherCooperativeScenario.Split)
        {
            Require(callbacks == 0 && result is TupleTerm { Items.Count: 2 });
            var pair = (TupleTerm)result;
            await Count(pair.Items[0], QueueHigherCooperativeHostNames.SplitPoint);
            await Count(pair.Items[1], QueueHigherCooperativeHostNames.Cells - QueueHigherCooperativeHostNames.SplitPoint);
        }
        else if (scenario == QueueHigherCooperativeScenario.Join)
        {
            Require(callbacks == 0);
            await Count(result, QueueHigherCooperativeHostNames.Cells * 2);
        }
        else
        {
            int expectedCallbacks = scenario == QueueHigherCooperativeScenario.DeleteRearRebuild
                ? QueueHigherCooperativeHostNames.FirstRearMatchCallbacks : QueueHigherCooperativeHostNames.Cells;
            Require(callbacks == expectedCallbacks);
            bool deleted = scenario is QueueHigherCooperativeScenario.DeleteFrontRebuild or QueueHigherCooperativeScenario.DeleteRearRebuild;
            await Count(result, QueueHigherCooperativeHostNames.Cells - (deleted ? 1 : 0));
            if (scenario is QueueHigherCooperativeScenario.DeleteFrontNoMatch or QueueHigherCooperativeScenario.DeleteRearNoMatch)
                Require(ReferenceEquals(result, original));
        }
        await Count(original, QueueHigherCooperativeHostNames.Cells);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(QueueHigherCooperativeHostNames.Failure);
    }
}
