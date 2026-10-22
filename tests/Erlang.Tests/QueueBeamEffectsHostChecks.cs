using Erlang;
using System.Collections;
using System.Reflection;

namespace Erlang.Tests;

internal static class QueueBeamEffectsHostChecks
{
    internal static readonly List<QueueBeamErrorObservation> ErrorObservations = new();

    internal static async Task PredicateInvalidList()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term list = Term.A(QueueBeamFullSmokeNames.InvalidList);
            var callback = new FunctionTerm(1, (_, _) => ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.True)));
            foreach (string operation in new[] { QueueModuleNames.All, QueueModuleNames.Any })
            {
                foreach (Term function in new Term[] { callback, Nil.Value })
                {
                    try
                    {
                        await runtime.Modules.Call(
                            context,
                            QueueBeamAdmissionHostNames.Lists,
                            operation,
                            function,
                            list
                        );
                        throw new InvalidOperationException(QueueBeamFullSmokeNames.Failure);
                    }
                    catch (ErlangException error)
                    {
                        Term expected = ReferenceEquals(function, callback)
                            ? Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), list)
                            : Term.A(ErlangErrorReasons.FunctionClause);
                        QueueBeamFullSmokeBackend.Require(error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(expected));
                        var frame = (TupleTerm)((Cons)error.StackTraceTerm).Head;
                        QueueBeamFullSmokeBackend.Require(frame.Items[0].Equals(Term.A(QueueBeamAdmissionHostNames.Lists))
                            && frame.Items[1].Equals(Term.A(operation)) && frame.Items[3] is Cons);
                    }
                }
            }

            return Nil.Value;
        });
    }

    internal static async Task Long(bool beamBackend, bool originalLists = false)
    {
        await using var runtime = new ProcessRuntime();
        QueueBeamFullSmokeBackend.Select(runtime, beamBackend, originalLists);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Cons.From(Enumerable.Range(0, QueueBeamFullSmokeNames.LongCells).Select(value => Term.I(value)));

            ValueTask<Term> Call(string function, params Term[] arguments)
                => runtime.Modules.Call(
                    context,
                    QueueModuleNames.Module,
                    function,
                    arguments
                );
            Term queue = await Call(QueueModuleNames.FromList, input);
            QueueBeamFullSmokeBackend.Require((await Call(QueueModuleNames.ToList, queue)).Equals(input));
            Term reverse = await Call(QueueModuleNames.Reverse, queue);
            Term reversed = Cons.From(Enumerable.Range(0, QueueBeamFullSmokeNames.LongCells).Reverse().Select(value => Term.I(value)));
            QueueBeamFullSmokeBackend.Require((await Call(QueueModuleNames.ToList, reverse)).Equals(reversed));
            foreach (int point in new[] { 0, QueueBeamFullSmokeNames.SplitIndex, QueueBeamFullSmokeNames.LongCells })
            {
                var parts = (TupleTerm)await Call(QueueModuleNames.Split, Term.I(point), queue);
                Term left = await Call(QueueModuleNames.ToList, parts.Items[0]);
                Term right = await Call(QueueModuleNames.ToList, parts.Items[1]);
                QueueBeamFullSmokeBackend.Require(left.Equals(Cons.From(Enumerable.Range(0, point).Select(value => Term.I(value)))));
                QueueBeamFullSmokeBackend.Require(right.Equals(Cons.From(Enumerable.Range(point, QueueBeamFullSmokeNames.LongCells - point).Select(value => Term.I(value)))));
                Term joined = await Call(QueueModuleNames.Join, parts.Items[0], parts.Items[1]);
                QueueBeamFullSmokeBackend.Require((await Call(QueueModuleNames.ToList, joined)).Equals(input));
            }
            QueueBeamFullSmokeBackend.Require((await Call(QueueModuleNames.ToList, queue)).Equals(input));

            return Nil.Value;
        });
    }

    internal static async Task CallbackFailures(bool beamBackend, bool originalLists = false)
    {
        await using var runtime = new ProcessRuntime();
        QueueBeamFullSmokeBackend.Select(runtime, beamBackend, originalLists);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (string exceptionClass in new[] { ErlangExceptionClasses.Error, ErlangExceptionClasses.Throw, ErlangExceptionClasses.Exit })
            {
                int calls = 0;
                var function = new FunctionTerm(
                    1,
                    (_, arguments) =>
                {
                    calls++;
                    QueueBeamFullSmokeBackend.Require(arguments[0].Equals(Term.I(1)));
                    throw new ErlangException(Term.A(QueueBeamFullSmokeNames.CallbackError), exceptionClass);
                }
                );
                try
                {
                    await runtime.Modules.Call(
                        context,
                        QueueModuleNames.Module,
                        QueueModuleNames.Filter,
                        function,
                        Term.Tuple(Term.List(Term.I(3)), Term.List(Term.I(1), Term.I(2)))
                    );
                    throw new InvalidOperationException(QueueBeamFullSmokeNames.Failure);
                }
                catch (ErlangException error)
                {
                    lock (ErrorObservations)
                        ErrorObservations.Add(new(
                            beamBackend,
                            exceptionClass,
                            error.ExceptionClass,
                            error.Reason,
                            error.StackTraceTerm
                        ));
                    QueueBeamFullSmokeBackend.Require(calls == 1 && error.ExceptionClass == exceptionClass && error.Reason.Equals(Term.A(QueueBeamFullSmokeNames.CallbackError)));
                }
            }

            return Nil.Value;
        });
    }

    internal static async Task Cancellation(bool beamBackend, bool originalLists = false)
    {
        await using var runtime = new ProcessRuntime();
        QueueBeamFullSmokeBackend.Select(runtime, beamBackend, originalLists);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(QueueModuleNames.Module)!;
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackUnwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationUnwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new FunctionTerm(
            1,
            async (context, _) =>
        {
            var processContext = (ProcessContext)context;
            entered.TrySetResult(processContext);
            try
            {
                await release.Task.WaitAsync(processContext.Cancellation);

                return Term.A(ErlangBooleanAtoms.True);
            }
            finally
            {
                callbackUnwound.TrySetResult();
            }
        }
        );
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            try
            {
                return await runtime.Modules.Call(
                    context,
                    QueueModuleNames.Module,
                    QueueModuleNames.Filter,
                    callback,
                    Term.Tuple(Nil.Value, Term.List(Term.I(1), Term.I(2)))
                );
            }
            finally
            {
                operationUnwound.TrySetResult();
            }
        });
        try
        {
            ProcessContext active = await entered.Task.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
            QueueBeamFullSmokeBackend.Require(ActiveReferences(active, image) > 0);
            await observer.Invoke(context =>
            {
                runtime.Exit(context, worker.Pid, Term.A(QueueHigherCooperativeHostNames.Kill));

                return true;
            });
            Term reason = await worker.Completion.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
            await callbackUnwound.Task.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
            await operationUnwound.Task.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
            QueueBeamFullSmokeBackend.Require(reason.Equals(Term.A(ProcessExitReasons.Killed)));
            QueueBeamFullSmokeBackend.Require(ActiveReferences(active, image) == 0);
            if (originalLists)
                QueueBeamFullSmokeBackend.Require(ActiveReferences(active, runtime.Modules.CurrentCodeImage(ListBeamHostNames.Module)!) == 0);
            Term alive = await observer.InvokeAsync(async context => await runtime.Modules.Call(context, QueueModuleNames.Module, QueueModuleNames.New));
            QueueBeamFullSmokeBackend.Require(alive.Equals(Term.Tuple(Nil.Value, Nil.Value)));
        }
        finally
        {
            release.TrySetResult();
            if (!worker.Completion.IsCompleted)
                await observer.Invoke(context =>
                {
                    runtime.Exit(context, worker.Pid, Term.A(QueueHigherCooperativeHostNames.Kill));

                    return true;
                });
            await worker.Completion.WaitAsync(QueueHigherCooperativeHostNames.Deadline);
        }
    }

    private static int ActiveReferences(ProcessContext context, ModuleCodeImage image)
    {
        object gate = typeof(ProcessContext).GetField(ActualMinimalBeamGuestSendHostNames.CodeGate, ActualMinimalBeamGuestSendHostNames.Members)!.GetValue(context)!;
        lock (gate)
        {
            var active = (IDictionary)typeof(ProcessContext).GetField(ActualMinimalBeamGuestSendHostNames.ActiveCode, ActualMinimalBeamGuestSendHostNames.Members)!.GetValue(context)!;

            return active.Contains(image) ? (int)active[image]! : 0;
        }
    }
}
