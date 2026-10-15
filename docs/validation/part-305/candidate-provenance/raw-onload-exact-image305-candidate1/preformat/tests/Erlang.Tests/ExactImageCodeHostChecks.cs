using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Compiler;

namespace Erlang.Tests;

public static class ExactImageCodeHostChecks
{
    public static async Task Run()
    {
        await QueuedAcrossOldReplacement();
        await ActiveAndIdle(false);
        await ActiveAndIdle(true);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(ExactImageCodeHostNames.Broken);
    }

    private static object Call(object target, string method, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(method, ExactImageCodeHostNames.Members)!.Invoke(target, arguments)!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static ValueTask<Term> ImageCheck(ProcessRuntime runtime, ProcessContext requester, Pid target, ModuleCodeImage image, bool async, Term id)
        => (ValueTask<Term>)Call(runtime, ExactImageCodeHostNames.ImageCheck, requester, target, image, async, id);

    private static Task Unwound(ProcessContext context)
        => ((TaskCompletionSource)typeof(ProcessContext).GetProperty(ExactImageCodeHostNames.Unwound, ExactImageCodeHostNames.Members)!.GetValue(context)!).Task;

    private static async Task RunActor(ProcessRuntime runtime, Func<ProcessContext, Task> body)
    {
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                await body(context);
                result.TrySetResult();
            }
            catch (Exception failure)
            {
                result.TrySetException(failure);
                throw;
            }

            return Nil.Value;
        });
        await result.Task.WaitAsync(ExactImageCodeHostParameters.Deadline);
        await actor.Completion.WaitAsync(ExactImageCodeHostParameters.Deadline);
    }

    private static int Pending(ProcessRuntime runtime, ProcessContext target)
    {
        object gate = typeof(ProcessRuntime).GetField(ExactImageCodeHostNames.Gate, ExactImageCodeHostNames.Members)!.GetValue(runtime)!;
        lock (gate)
        {
            object requests = typeof(ProcessContext).GetProperty(ExactImageCodeHostNames.Pending, ExactImageCodeHostNames.Members)!.GetValue(target)!;

            return (int)requests.GetType().GetProperty(nameof(ICollection.Count))!.GetValue(requests)!;
        }
    }

    private static async Task QueuedAcrossOldReplacement()
    {
        await using var runtime = new ProcessRuntime();
        new Parser(ExactImageCodeHostNames.Source).ParseModule().Register(runtime.Modules);
        var image = runtime.Modules.CurrentCodeImage(ExactImageCodeHostNames.Module)!;
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle? target = null;
        try
        {
            await RunActor(runtime, async context =>
            {
                target = runtime.Spawn(async child =>
                {
                    using var lease = runtime.Modules.ReserveSourceCode(child, image);
                    entered.TrySetResult(child);
                    await release.Task;
                    await child.ReceiveAsync<Term>(message => message);

                    return Nil.Value;
                });
                ProcessContext child = await entered.Task.WaitAsync(ExactImageCodeHostParameters.Deadline);
                runtime.Modules.DeleteModule(ExactImageCodeHostNames.Module);
                new Parser(ExactImageCodeHostNames.ReplacementSource).ParseModule().Register(runtime.Modules);
                var exactId = runtime.MakeReference();
                var publicId = runtime.MakeReference();
                Require((await ImageCheck(runtime, context, target.Pid, image, true, exactId)).Equals(Term.A(ProcessCodeCheckNames.AsyncResult)));
                await (ValueTask<Term>)Call(runtime, ExactImageCodeHostNames.ModuleCheck, context, target.Pid, ExactImageCodeHostNames.Module, true, publicId);
                Require(Pending(runtime, child) == 2);
                // This fixture models prepare_on_load's private old-slot displacement.
                // It does not activate a public loader or purger operation.
                runtime.Modules.Apply(registry =>
                {
                    var old = (IDictionary)typeof(ModuleRegistry).GetField(ExactImageCodeHostNames.OldCode, ExactImageCodeHostNames.Members)!.GetValue(registry)!;
                    old.Remove(ExactImageCodeHostNames.Module);
                    registry.DeleteModule(ExactImageCodeHostNames.Module);
                });
                release.TrySetResult();
                runtime.Send(target.Pid, Term.A(ExactImageCodeHostNames.Continue));
                var exact = await context.ReceiveAsync(message => message is TupleTerm { Items.Count: 3 } tuple && tuple.Items[1].Equals(exactId) ? tuple : null);
                var module = await context.ReceiveAsync(message => message is TupleTerm { Items.Count: 3 } tuple && tuple.Items[1].Equals(publicId) ? tuple : null);
                Require(exact!.Items[2].Equals(Term.A(ErlangBooleanAtoms.True)));
                Require(module!.Items[2].Equals(Term.A(ErlangBooleanAtoms.False)));
                await target.Completion.WaitAsync(ExactImageCodeHostParameters.Deadline);
            });
        }
        finally
        {
            release.TrySetResult();
            if (target is not null)
                runtime.Send(target.Pid, Term.A(ExactImageCodeHostNames.Continue));
        }
    }

    private static async Task ActiveAndIdle(bool terminateBeforeScan)
    {
        await using var runtime = new ProcessRuntime();
        new Parser(ExactImageCodeHostNames.Source).ParseModule().Register(runtime.Modules);
        var image = runtime.Modules.CurrentCodeImage(ExactImageCodeHostNames.Module)!;
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await RunActor(runtime, async context =>
            {
                var held = (FunctionTerm)await runtime.Modules.Call(context, ExactImageCodeHostNames.Module, ExactImageCodeHostNames.Make);
                context.Dictionary[Term.A(ExactImageCodeHostNames.Held)] = held;
                var idleReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var idle = runtime.Spawn(async holder =>
                {
                    holder.Dictionary[Term.A(ExactImageCodeHostNames.Held)] = held;
                    idleReady.TrySetResult();
                    await holder.ReceiveAsync<Term>(message => message);

                    return Nil.Value;
                });
                await idleReady.Task.WaitAsync(ExactImageCodeHostParameters.Deadline);
                Require((await ImageCheck(runtime, context, context.Self, image, false, Nil.Value)).Equals(Term.A(ErlangBooleanAtoms.False)));
                var target = runtime.Spawn(async child =>
                {
                    using var lease = runtime.Modules.ReserveSourceCode(child, image);
                    entered.TrySetResult(child);
                    try
                    {
                        if (terminateBeforeScan)
                            await release.Task;
                        else
                            await child.ReceiveAsync<Term>(message => message);
                    }
                    finally
                    {
                        // A retained global writer would deadlock this real cleanup.
                        await Task.Run(() => runtime.Modules.Apply(_ => cleanup.TrySetResult()));
                        await cleanupRelease.Task;
                    }

                    return Nil.Value;
                });
                ProcessContext child = await entered.Task.WaitAsync(ExactImageCodeHostParameters.Deadline);
                if (terminateBeforeScan)
                {
                    runtime.Exit(context, target.Pid, Term.A(ProcessExitSignals.Kill));
                    await target.Completion.WaitAsync(ExactImageCodeHostParameters.Deadline);
                    Require(!Unwound(child).IsCompleted);
                }
                Task draining;
                if (terminateBeforeScan)
                {
                    // This async helper runs inline through the terminated branch
                    // to its first Unwound wait before returning to this caller.
                    draining = ((ValueTask)Call(runtime, ExactImageCodeHostNames.DrainOne, context, child, image)).AsTask();
                    Require(!draining.IsCompleted);
                    release.TrySetResult();
                }
                else
                {
                    draining = ((ValueTask)Call(runtime, ExactImageCodeHostNames.Drain, context, image)).AsTask();
                }
                await cleanup.Task.WaitAsync(ExactImageCodeHostParameters.Deadline);
                Require(!draining.IsCompleted && !Unwound(child).IsCompleted);
                cleanupRelease.TrySetResult();
                await draining.WaitAsync(ExactImageCodeHostParameters.Deadline);
                Require(Unwound(child).IsCompleted);
                if (terminateBeforeScan)
                    await (ValueTask)Call(runtime, ExactImageCodeHostNames.Drain, context, image);
                Require(!idle.Completion.IsCompleted);
                runtime.Send(idle.Pid, Term.A(ExactImageCodeHostNames.Continue));
                await idle.Completion.WaitAsync(ExactImageCodeHostParameters.Deadline);
                Require((await held.Invoke(context, [])).Equals(Term.A(ExactImageCodeHostNames.Kept)));
                Require(context.Dictionary.ContainsKey(Term.A(ExactImageCodeHostNames.Held)));
            });
        }
        finally
        {
            release.TrySetResult();
            cleanupRelease.TrySetResult();
        }
    }
}
