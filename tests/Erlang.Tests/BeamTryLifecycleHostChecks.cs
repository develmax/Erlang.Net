using System.Collections;
using Erlang;

namespace Erlang.Tests;

internal static class BeamTryLifecycleHostChecks
{
    internal static Task Normal()
        => Positive(false);

    internal static Task ErlangException()
        => Positive(true);

    private static async Task Positive(bool fail)
    {
        await using var runtime = new ProcessRuntime();
        ModuleCodeImage image = Publish(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        int calls = 0;
        bool callbackUnwound = false;
        var callback = new FunctionTerm(
            BeamTryLifecycleHostNames.FunctionArity,
            (_, _) =>
        {
            calls++;
            try
            {
                if (fail)
                    throw new ErlangException(Term.A(BeamTryLifecycleHostNames.CallbackError));

                return ValueTask.FromResult<Term>(Term.A(BeamOpcodeEdgeNames.Other));
            }
            finally
            {
                callbackUnwound = true;
            }
        }
        );
        await actor.InvokeAsync(async context =>
        {
            Term result = await Call(runtime, context, callback);
            Require(result.Equals(Term.A(fail ? BeamOpcodeEdgeNames.Marker : BeamOpcodeEdgeNames.Other)));
            Require(calls == 1 && callbackUnwound && ActiveReferences(context, image) == 0);

            return result;
        });
    }

    internal static async Task ClrFailure()
    {
        await using var runtime = new ProcessRuntime();
        ModuleCodeImage image = Publish(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var expected = new InvalidDataException(BeamTryLifecycleHostNames.ClrFailure);
        bool callbackUnwound = false;
        ProcessContext? executing = null;
        var callback = new FunctionTerm(
            BeamTryLifecycleHostNames.FunctionArity,
            (context, _) =>
        {
            executing = (ProcessContext)context;
            try
            {
                throw expected;
            }
            finally
            {
                callbackUnwound = true;
            }
        }
        );
        bool escaped = false;
        try
        {
            _ = await actor.InvokeAsync(async context => await Call(runtime, context, callback));
        }
        catch (InvalidDataException observed)
        {
            escaped = true;
            Require(ReferenceEquals(expected, observed));
        }
        Require(escaped && callbackUnwound && executing is not null && ActiveReferences(executing, image) == 0);
        // The host command catches CLR transport outside BEAM. A second invocation must not inherit a stale tag.
        await FreshCall(runtime, actor, image);
    }

    internal static async Task Cancellation()
    {
        await using var runtime = new ProcessRuntime();
        ModuleCodeImage image = Publish(runtime);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackUnwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationUnwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outcome = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new FunctionTerm(
            BeamTryLifecycleHostNames.FunctionArity,
            async (context, _) =>
        {
            var owner = (ProcessContext)context;
            entered.TrySetResult(owner);
            try
            {
                await release.Task.WaitAsync(owner.Cancellation);

                return Term.A(BeamOpcodeEdgeNames.Other);
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
                Term result = await Call(runtime, context, callback);
                outcome.TrySetResult(result);

                return result;
            }
            catch (Exception failure)
            {
                outcome.TrySetException(failure);
                throw;
            }
            finally
            {
                operationUnwound.TrySetResult();
            }
        });
        Exception? primary = null;
        try
        {
            ProcessContext active = await entered.Task.WaitAsync(BeamTryLifecycleHostNames.Deadline);
            Require(ActiveReferences(active, image) > 0);
            await Kill(runtime, observer, worker);
            Term reason = await worker.Completion.WaitAsync(BeamTryLifecycleHostNames.Deadline);
            await callbackUnwound.Task.WaitAsync(BeamTryLifecycleHostNames.Deadline);
            await operationUnwound.Task.WaitAsync(BeamTryLifecycleHostNames.Deadline);
            Require(reason.Equals(Term.A(ProcessExitReasons.Killed)));
            bool cancelled = false;
            try
            {
                _ = await outcome.Task.WaitAsync(BeamTryLifecycleHostNames.Deadline);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                Require(active.Cancellation.IsCancellationRequested);
            }
            Require(cancelled && ActiveReferences(active, image) == 0);
            await using var fresh = new RuntimeNodeFoundationHostActor(runtime);
            await FreshCall(runtime, fresh, image);
        }
        catch (Exception failure)
        {
            primary = failure;
            throw;
        }
        finally
        {
            release.TrySetResult();
            try
            {
                if (!worker.Completion.IsCompleted)
                    await Kill(runtime, observer, worker);
                await worker.Completion.WaitAsync(BeamTryLifecycleHostNames.Deadline);
                if (entered.Task.IsCompletedSuccessfully)
                {
                    await callbackUnwound.Task.WaitAsync(BeamTryLifecycleHostNames.Deadline);
                    await operationUnwound.Task.WaitAsync(BeamTryLifecycleHostNames.Deadline);
                }
                if (outcome.Task.IsFaulted)
                    _ = outcome.Task.Exception;
            }
            catch (Exception cleanupFailure) when (primary is not null)
            {
                throw new AggregateException(primary, cleanupFailure);
            }
        }
    }

    private static ModuleCodeImage Publish(ProcessRuntime runtime)
    {
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(BeamTryLifecycleBytes.Image());
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);

        return runtime.Modules.CurrentCodeImage(prepared.Name)!;
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        FunctionTerm callback
    )
        => runtime.Modules.Call(
            context,
            BeamOpcodeEdgeNames.Module,
            BeamOpcodeEdgeNames.Run,
            callback
        );

    private static async Task FreshCall(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        ModuleCodeImage image
    )
    {
        var callback = new FunctionTerm(
            BeamTryLifecycleHostNames.FunctionArity,
            (_, _) => ValueTask.FromResult<Term>(Term.A(BeamOpcodeEdgeNames.Other))
        );
        await actor.InvokeAsync(async context =>
        {
            Term result = await Call(runtime, context, callback);
            Require(result.Equals(Term.A(BeamOpcodeEdgeNames.Other)) && ActiveReferences(context, image) == 0);

            return result;
        });
    }

    private static Task<bool> Kill(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor observer,
        ProcessHandle worker
    )
        => observer.Invoke(context =>
        {
            runtime.Exit(context, worker.Pid, Term.A(BeamTryLifecycleHostNames.Kill));

            return true;
        });

    private static int ActiveReferences(ProcessContext context, ModuleCodeImage image)
    {
        object gate = typeof(ProcessContext).GetField(ActualMinimalBeamGuestSendHostNames.CodeGate, ActualMinimalBeamGuestSendHostNames.Members)!.GetValue(context)!;
        lock (gate)
        {
            var active = (IDictionary)typeof(ProcessContext).GetField(ActualMinimalBeamGuestSendHostNames.ActiveCode, ActualMinimalBeamGuestSendHostNames.Members)!.GetValue(context)!;

            return active.Contains(image) ? (int)active[image]! : 0;
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BeamTryLifecycleHostNames.Failure);
    }
}
