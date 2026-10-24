using System.Collections;
using Erlang;

namespace Erlang.Tests;

internal static class BeamTryExtensionHostChecks
{
    internal static async Task AsyncErlangFailure()
    {
        await using var runtime = new ProcessRuntime();
        ModuleCodeImage image = Publish(runtime, BeamTryLifecycleBytes.Image());
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new FunctionTerm(
            BeamTryLifecycleHostNames.FunctionArity,
            async (context, _) =>
        {
            var owner = (ProcessContext)context;
            try
            {
                await Task.Yield();
                entered.TrySetResult(owner);
                await release.Task.WaitAsync(owner.Cancellation);
                throw new ErlangException(Term.A(BeamTryLifecycleHostNames.CallbackError));
            }
            finally
            {
                unwound.TrySetResult();
            }
        }
        );
        Task<Term> call = actor.InvokeAsync(async context => await Call(runtime, context, callback));
        Exception? primary = null;
        try
        {
            ProcessContext active = await entered.Task.WaitAsync(BeamTryLifecycleHostNames.Deadline);
            Require(!call.IsCompleted && ActiveReferences(active, image) > 0);
            release.TrySetResult();
            Term result = await call.WaitAsync(BeamTryLifecycleHostNames.Deadline);
            await unwound.Task.WaitAsync(BeamTryLifecycleHostNames.Deadline);
            Require(result.Equals(Term.A(BeamOpcodeEdgeNames.Marker)) && ActiveReferences(active, image) == 0);
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
                await call.WaitAsync(BeamTryLifecycleHostNames.Deadline);
            }
            catch (Exception cleanupFailure) when (primary is not null)
            {
                throw new AggregateException(primary, cleanupFailure);
            }
        }
    }

    internal static async Task TailErrors()
    {
        await using var runtime = new ProcessRuntime();
        ModuleCodeImage image = Publish(runtime, BeamTryExtensionBytes.Tail(false));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        int callbacks = 0;
        var wrongArity = new FunctionTerm(
            BeamTryExtensionHostNames.WrongCallbackArity,
            (_, _) =>
        {
            callbacks++;

            return ValueTask.FromResult<Term>(Term.A(BeamOpcodeEdgeNames.Other));
        }
        );
        foreach (Term callee in new Term[] { Term.I(0), wrongArity })
        {
            await actor.InvokeAsync(async context =>
            {
                bool observed = false;
                try
                {
                    _ = await Call(runtime, context, callee);
                }
                catch (ErlangException error)
                {
                    observed = true;
                    Term reason = callee is FunctionTerm
                        ? Term.Tuple(Term.A(ErlangErrorReasons.BadArity), Term.Tuple(callee, Nil.Value))
                        : Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), callee);
                    Require(error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(reason));
                    // Whole trace equality: the no-Line synthetic caller has precisely run/1 with Nil metadata.
                    Require(error.StackTraceTerm.Equals(CallerStack()));
                    Require(error.AutomaticStackTraceCaptured && !error.ExplicitStackTrace);
                }
                Require(observed && callbacks == 0 && ActiveReferences(context, image) == 0 && context.CurrentFunction is null);

                return Nil.Value;
            });
        }
    }

    internal static async Task TailReturn()
    {
        await using var runtime = new ProcessRuntime();
        ModuleCodeImage image = Publish(runtime, BeamTryExtensionBytes.Tail(true));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var callback = new FunctionTerm(
            BeamTryLifecycleHostNames.FunctionArity,
            async (context, _) =>
        {
            var owner = (ProcessContext)context;
            Require(owner.CurrentFunction == BeamOpcodeEdgeNames.Run && owner.CurrentFunctionArity == BeamTryLifecycleHostNames.RunArity);
            await Task.Yield();
            Require(owner.CurrentFunction == BeamOpcodeEdgeNames.Run && owner.CurrentFunctionArity == BeamTryLifecycleHostNames.RunArity);
            var snapshot = (ErlangException)NifAdmissionHostBridge.Call(
                owner,
                BeamTryExtensionHostNames.CaptureSourceFailure,
                new ErlangException(Term.A(BeamTryLifecycleHostNames.CallbackError)),
                null
            )!;
            Require(snapshot.StackTraceTerm.Equals(CallerStack()) && snapshot.AutomaticStackTraceCaptured);

            return Term.A(BeamOpcodeEdgeNames.Other);
        }
        );
        await actor.InvokeAsync(async context =>
        {
            Term result = await Call(runtime, context, callback);
            Term expected = Term.Tuple(Term.A(BeamOpcodeEdgeNames.Marker), Term.A(BeamOpcodeEdgeNames.Other));
            Require(result.Equals(expected) && ActiveReferences(context, image) == 0 && context.CurrentFunction is null);

            return result;
        });
    }

    private static Term CallerStack()
        => Term.List(Term.Tuple(
            Term.A(BeamOpcodeEdgeNames.Module),
            Term.A(BeamOpcodeEdgeNames.Run),
            Term.I(BeamTryLifecycleHostNames.RunArity),
            Nil.Value
        ));

    private static ModuleCodeImage Publish(ProcessRuntime runtime, byte[] bytes)
    {
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(bytes);
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);

        return runtime.Modules.CurrentCodeImage(prepared.Name)!;
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        Term callback
    )
        => runtime.Modules.Call(
            context,
            BeamOpcodeEdgeNames.Module,
            BeamOpcodeEdgeNames.Run,
            callback
        );

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
